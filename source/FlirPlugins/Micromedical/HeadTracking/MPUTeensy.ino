// Dale Roberts
// December 2015
// Johns Hopkins University
//
// Code to read sample from an Invensense MPU-9250 at 1000 samples/sec,
// and write samples out to the UART to a Point Grey camera (or any UART
// receiver) at whatever trigger rate is used on the FRAME_STROBE_PIN (pin 2).
//

#include <SPI.h>
#include "mpu9250.h"

#define SYSCLK_Mhz  84

#define SPI_CSPIN 10
//#define SPI_CSPIN 9

#define SPI_MV2_CSPIN 14

#define SYNC_PIN 15
#define IRQ_SCOPE_PIN 20

#define MPU_IRQ_PIN 9
//#define MPU_IRQ_PIN 8

#define PTGREY_FRAME_STROBE_PIN 2
#define PTGREY_16FRAME_STROBE_PIN 3


int accelRange = 0x02;   //choose 0x00=2g, 0x01=4g, 0x02=8g, 0x03=16g
int gyroRange = 0x01;    //choose 0x00=250dps, 0x01=500dps, 0x02=1000dps, 0x03=2000dps

// Used by interrupt routine to determine if it should give up on time consuming activities.
int MPUSampRate_KHz;

// Selects which register to put the FSYCN bit in. When enabled, the low bit of the specified
// sensor register is overwritten with the FSYNC bits. Values from 0 to 7 are:
//   Disabled, TEMPerature register, Gyro X, Y, Z, Accel X, Y, Z.
#define FSYNC_BIT 5


char spbuf[300];
void dprintf(char const *Format, ...)
{
  va_list ap;
  va_start(ap, Format);
  vsnprintf(spbuf, sizeof(spbuf), Format, ap);
  va_end(ap);
  Serial.print(spbuf);
}

void MPU_write(int addr, int val)
{
  digitalWrite(SPI_CSPIN, LOW);
  SPI.transfer(addr);
  SPI.transfer(val);
  digitalWrite(SPI_CSPIN, HIGH);
}

// Read a single 8-bit register.
byte MPU_read8(int addr)
{
  digitalWrite(SPI_CSPIN, LOW);
  SPI.transfer(addr | (1 << 7));
  int val = SPI.transfer(0);
  digitalWrite(SPI_CSPIN, HIGH);
  
  return val;
}

// Read a 16-bit register.
short MPU_read16(int addr)
{
  short rval;

  digitalWrite(SPI_CSPIN, LOW);
  SPI.transfer(addr | (1 << 7));
  rval = ((short)SPI.transfer(0)) << 8;
  rval |= SPI.transfer(0);
  digitalWrite(SPI_CSPIN, HIGH);
  return rval;
}

// Read N registers sequentially, starting at addr, with a single transition of Chip Select.
// No data is saved or returned to the caller. The sole purpose of this
// function is to get the data to display on the oscilloscope.
void MPU_readN(int addr, int N)
{
  digitalWrite(SPI_CSPIN, LOW);
  SPI.transfer(addr | (1 << 7));

  while (N-- > 1)
    SPI.transfer(0);

  SPI.transfer(0);
  digitalWrite(SPI_CSPIN, HIGH);
}

// Read N 16-bit registers sequentially, starting at addr,
// with a single transition of Chip Select.
void MPU_readN16(short *buf, int addr, int N)
{
  digitalWrite(SPI_CSPIN, LOW);
  SPI.transfer(addr | (1 << 7));

  while (N-- > 0) {
    *buf = ((short)SPI.transfer(0)) << 8;
    *buf |= SPI.transfer(0);
    ++buf;
  }

  digitalWrite(SPI_CSPIN, HIGH);
}

// This sets the gyro filter setting and the FCHOICE value, as specified in the MPU9250 register
// manual. See table with register 26 in manual for associated bandwidths and delays.
//
// We break this out so that we can set it "on the fly" while the chip is running.
//
int gyro_last_filtval, gyro_last_fchoice;
void SetGyro(int filtval, int fchoice)
{
  // Set the Gyro low-pass filter.
  // (FSYNC_BIT<<3) enables saving FSYNC signal into low bit of selected sensor register.
  MPU_write(MPU9250_CONFIG, filtval | (FSYNC_BIT << 3));

  // Set full-scale range of gyro. Also set the FCHOICE bits.
  // Note we INVERT the FCHOICE bits here, so they correspond to the table in the manual.
  // (the FCHOICE_B register bits confusingly contain the inverse of the FCHOICE bits.
  MPU_write(MPU9250_GYRO_CONFIG, (gyroRange << 3) | (0x3 & ~fchoice));

  gyro_last_filtval = filtval;
  gyro_last_fchoice = fchoice;
}

// Do the same thing for the accelerometer. There is a separate table describing these values.
void SetAccel(int filtval, int fchoice)
{
  // Set the A/D full-scale ranges.
  MPU_write(MPU9250_ACCEL_CONFIG, accelRange << 3);//set fullscale range of accel

  // Set the Accel low-pass filter.
  MPU_write(MPU9250_ACCEL_CONFIG2, filtval | ((0x1 & ~fchoice) << 3));
}


int SYNCcount = 0;

// Make sure val is within -limit <= val <= +limit.
inline int maxabs(int val, int limit)
{
  return val>limit ? limit : ((val<-limit) ? -limit : val);
}

// Called when the MPU9250 interrupt pin goes high.
int IRQcount=0;
int accel, angvel;

// We read the temperature register from the MPU9250, but we DO NOT send it
// to the camera.
short CurSamp[7];

void MPU_interrupt()
{
//  digitalWrite(IRQ_SCOPE_PIN, 1);
  ++IRQcount;

  // Gyro X and accel Z are good when motion is perpendicular to the plane of the chip, and
  // rotation is about the short length of the board.
  //int angvel = MPU_read16(MPU9250_GYRO_XOUT_H);
  //int accel = MPU_read16(MPU9250_ACCEL_ZOUT_H);

  // This is when the rotation is about the vector perpendicular to the chip,
  // and the accel is along the short length of the board.
//  angvel = MPU_read16(MPU9250_GYRO_ZOUT_H);
//  accel = MPU_read16(MPU9250_ACCEL_XOUT_H);

  // Read all 6 sensors in one go.
  SPI.setDataMode(SPI_MODE3);  // MPU9250 uses SPI MODE 3!!
  MPU_readN16(CurSamp, MPU9250_ACCEL_XOUT_H, 7);
  angvel = CurSamp[5];
  accel = CurSamp[0];
    
//  analogWrite(DAC0, maxabs(-(angvel>>4),2046) + 2048);
//  analogWrite(DAC1, maxabs((accel>>4), 2046) + 2048);

  // At high sample rates, skip the extra stuff, else the Arduino is overwhelmed
  // by interrupts, and can't keep up.
//  if(MPUSampRate_KHz == 32) {
//    // Just read the Gyro config register, which tells us which Fchoice is in effect.
//    MPU_read8(27);
//    return;
//  }

  // Write out FSYNC bit.
//  digitalWrite(51, accel & 1);

  // Write to the Point Grey camera via UART serial link.
//  Serial1.write((uint8_t *)&accel, 2);

//  Serial1.write((uint8_t *)&accel, 2);

//  if (++SYNCcount >= 20) {
//    SYNCcount = 0;
//    digitalWrite(SYNC_PIN, 1);
//  } else
//    digitalWrite(SYNC_PIN, 0);

  // For documentation purposes, read the 4 gyro and accel config registers, so that
  // the SPI data appears on the oscilloscope. We don't actually save any of the
  // data here.
  //
  // Read the 4 config registers, starting with register 26.
  // These are config, gyro config, accel config, accel config2.
//  MPU_readN(26, 4);

//  digitalWrite(IRQ_SCOPE_PIN, 0);

}

int GyroRangeString(int range)
{
  switch (range) {
    case 0: return 250; break;
    case 1: return 500; break;
    case 2: return 1000; break;
    case 3: return 2000; break;
    default: return 0; break;
  }
}

// Take into account the current sample rate, and set the SPI clock accordingly.
// The 3 possible sensor sample rates are 1KHz, 8KHz, and 32KHz.
void SetSPIClock()
{
  // Set the SPI rate based on the sample rate, so that the SPI commands
  // will complete within one interrupt period.
    int divisor;

    if(gyro_last_fchoice != 0x3)
      {divisor = SYSCLK_Mhz / 20; MPUSampRate_KHz = 32;}
    else if ((gyro_last_filtval == 0) || (gyro_last_filtval == 7))
      {divisor = SYSCLK_Mhz / 1.5; MPUSampRate_KHz = 8;}
    else
      // NOTE, largest divisor value is 255, so we can't go as low as 200KHz.
      {divisor = SYSCLK_Mhz / 0.35; MPUSampRate_KHz = 1;}

  SPI.setClockDivider(divisor);

  dprintf("Gyro filter %d, range %4ddps, %2d KSamp/sec, SPI clk 84/%-3d=%6.3fMHz\n",
    gyro_last_filtval, GyroRangeString(gyroRange), MPUSampRate_KHz,
    divisor, (double)SYSCLK_Mhz/divisor);
}


// Process commands sent by the user via the serial port (i.e., SerialMonitor in Arduino GUI).
void ProcessCommand()
{
  char ch = Serial.read();

  noInterrupts();
  
  switch (ch) {
    case '0': case '1': case '2': case '3':
    case '4': case '5': case '6': case '7':
      SetGyro(ch - '0', 0x3);
      break;

    case '8': SetGyro(0, 0x00); break;
    case '9': SetGyro(0, 0x01); break;

    case '-':
      if (gyroRange > 0) {
        --gyroRange;
        SetGyro(gyro_last_filtval, gyro_last_fchoice);
      }
      break;

    case '+': case '=':
      if (gyroRange < 3) {
        ++gyroRange;
        SetGyro(gyro_last_filtval, gyro_last_fchoice);
      }
      break;
  }

//  SetSPIClock();
  interrupts();
}



//************************************************************************************
// Init the SPI bus to talk to the MPU9250, then init the MPU9250.
//
int mputestcount=0;
void Init9250()
{
  // For setup and writing to registers, use the slower 1MHz SPI clock, as specified in the data sheet.
  // Can use up to 20MHz later for read-only from sensor registers.
  pinMode(SPI_CSPIN, OUTPUT);

  //SPI.begin(); // CALLED in main setup() now.
  
  //SPI.setClockDivider(SYSCLK_Mhz / 1);
  //SPI.setClockDivider(SPI_CLK_DIV4);
  SPI.setDataMode(SPI_MODE3);
  SPI.setBitOrder(MSBFIRST);

  // Now that the SPI bus is set up, we can start talking to the MPU9250 chip.

  // Reset while MPU.
  MPU_write(MPU9250_PWR_MGMT_1, 0x81);
  delay(50);

  // Disable I2C mode - set I2C_IF_DIS bit, reset FIFO, and reset signal paths..
  MPU_write(MPU9250_USER_CTRL, 0x15);
  delay(50);
  MPU_write(MPU9250_USER_CTRL, 0x00);
  delay(50);

  // See if we can read the chip ID register.
  dprintf("%d MPU9250 who_am_i register: %d  (should be 113)\n",
    mputestcount++, MPU_read8(MPU9250_WHO_AM_I));

  // Set Accel digital filter to setting 0, and enable the filter.
  //SetAccel(0, 1); // Turn on highest bandwidth filter, 1KHz samples.
  SetAccel(0, 0); // Turn off filter, 4KHz samples.

  // Set Gyro digital filter to setting 1, and set FCHOICE to 0b11 to enable digital filtering.
  SetGyro(1, 3);

  // Set sample rate divisor.
  //MPU_write(MPU9250_SMPLRT_DIV, 9);
  MPU_write(MPU9250_SMPLRT_DIV, 0);

  // Set INT pin to put out 50uSec pulse, and to clear INT status on ANY read
  // (so we don't have to explicitely read the interrupt status register).
  // 0x20 - latch until read.
  // 0x10 - ANY read clears INT bit, not just int status reg read.
  //MPU_write(MPU9250_INT_PIN_CFG, 0x30);

  // If we just leave this register 0, and enable the RAW data interrupt, then we get
  // a 50uSec pulse on EVERY data sample, which is EXACTLY what we need!!
  // VERY nice!!!
  MPU_write(MPU9250_INT_PIN_CFG, 0x0);

  // Enable raw data read interrupt, which will put out a 50uSec pulse on the INT line.
  MPU_write(MPU9250_INT_ENABLE, 0x01);

  // Set SPI clock automatically according to current Gyro sample rate.
//  SetSPIClock();

  dprintf("MPU9250 who_am_i register: %d  (should be 113)\n", MPU_read8(MPU9250_WHO_AM_I));
}

//************************************************************************************
// Camera interrupt, once per camera frame.

// Count by 2, and use the low bit to indicate whether we detect the camera's
// 16-frame "sync" bit set.
ulong FrameCount=0;

// Write out a 32-bit integer to the serial UART, to the camrea.
void inline SerialWrite32(long val) {Serial1.write((uint8_t*)&val, 4);}

void ReadAllMV2();
// Note we have an extra value here, because the Camera serial port code likes to
// see an EVEN number of integer values, else it crashes. So we have a "dummy"
// 0 value that we always send.
short AllMV2Scaled[4];

void PTGREY_interrupt()
{
  // Increment count by 2, and "OR" in the 16-frame sync bit.
  FrameCount = (FrameCount+2) & ~1;
  FrameCount |= digitalRead(PTGREY_16FRAME_STROBE_PIN);

  SerialWrite32(0xffffffff);    // Write out the "flag" value of 0xffffffff.
  SerialWrite32(FrameCount);    // Write out frame counter & sync bit.

  // Skip over the temperature sensor value, CurSamp[3].
  // Write out 3 accel sensors (signed short integers).
  Serial1.write((uint8_t *)&CurSamp, 6);
  // Write out 3 gyro sensors.
  Serial1.write((uint8_t *)&CurSamp[4], 6);

  // Write out MV2 samples.
  ReadAllMV2();
  Serial1.write((uint8_t *)AllMV2Scaled, 8);
}


// The 16 bit "control" word tells the chip what to do.
// Bit 13 high - Write to one of the 3 registers.
// Bit 12 high - Read from one of the 3 registers.
// Bits 11, 10 - Must be 1 1
// Bits 9,8  - Address of register to write or read.
// Bits 7-0  - Value to write to register.
//
// If neither 13 nor 12 are high, then, on the next SPI chip select 

void WriteMV2(int ADDR, int VAL)
{
  digitalWrite(SPI_MV2_CSPIN, LOW);
  SPI.transfer(0x2c | (ADDR&0x3)); // Set the READ bit, and bits 11 and 10.
  SPI.transfer(VAL);
  digitalWrite(SPI_MV2_CSPIN, HIGH);
}

int ReadMV2(int ADDR)
{
  digitalWrite(SPI_MV2_CSPIN, LOW);
  SPI.transfer(0x1c | (ADDR&0x3)); // Set the READ bit, and bits 11 and 10.
  int val = SPI.transfer(0);
  digitalWrite(SPI_MV2_CSPIN, HIGH);
  
  return val;
}

unsigned int MAG[3];
unsigned MAG_offset[3] = {32964, 32440, 32805}; // Offsets when in 10T range.
int REG0_val=0;
void ReadSamples()
{
  digitalWrite(SPI_MV2_CSPIN, LOW);
  MAG[0]  = SPI.transfer(0x2c) << 8; // Write register 0, read axis 0 high byte.
  MAG[0] |= SPI.transfer(REG0_val | 1);  // Select axis 1 for next read. Read axis 0 low byte.
  digitalWrite(SPI_MV2_CSPIN, HIGH);
  
  digitalWrite(SPI_MV2_CSPIN, LOW);
  MAG[1]  = SPI.transfer(0x2c) << 8; // Write register 0, read axis 1 high byte.
  MAG[1] |= SPI.transfer(REG0_val | 2);  // Select axis 2 for next read. Read axis 1 low byte.
  digitalWrite(SPI_MV2_CSPIN, HIGH);
  
  digitalWrite(SPI_MV2_CSPIN, LOW);
  MAG[2]  = SPI.transfer(0x2c) << 8; // Write register 0, read axis 2 high byte.
  MAG[2] |= SPI.transfer(REG0_val | 0);  // Select axis 0 for next read. Read axis 2 low byte.
  digitalWrite(SPI_MV2_CSPIN, HIGH);
}

void SetupMV2()
{
  dprintf("Setting up MV2...\n");
  pinMode(SPI_MV2_CSPIN, OUTPUT);
  SPI.setDataMode(SPI_MODE0);
  SPI.setBitOrder(MSBFIRST);  

  // Select Resolution 11, 375samples/sec. Range 10, 1 Tesla (then we select x10 mode).
  REG0_val = 0x38;  // 10T full range, when Large range is set.
  WriteMV2(0, REG0_val);
  WriteMV2(1, 0x80); // Select "Large" x10 measurement range.
  //WriteMV2(1, 0x00); // Select normal measurement range.

  dprintf("Regs 0/1/2: 0x%02x 0x%02x 0x%02x\n",
      ReadMV2(0), ReadMV2(1), ReadMV2(2));
}

// Talk to the MagVector MV2 magnetic sensing chip.
void TestMV2()
{
  dprintf("Setting up MV2...\n");
  pinMode(SPI_MV2_CSPIN, OUTPUT);

  SPI.begin();
  //SPI.setClockDivider(SYSCLK_Mhz / 1);
  //SPI.setClockDivider(SPI_CLK_DIV4);
  SPI.setDataMode(SPI_MODE0);
  SPI.setBitOrder(MSBFIRST);  

  // Select Resolution 11, 375samples/sec. Range 10, 1 Tesla (then we select x10 mode).
  REG0_val = 0x38;  // 10T full range, when Large range is set.
  WriteMV2(0, REG0_val);
  WriteMV2(1, 0x80); // Select "Large" x10 measurement range.
  //WriteMV2(1, 0x00); // Select normal measurement range.
  
  dprintf("Regs 0/1/2: 0x%02x 0x%02x 0x%02x\n",
      ReadMV2(0), ReadMV2(1), ReadMV2(2));
}

double MagScale(unsigned int MAG_int, unsigned int OFFSET=32768)
{return ((double)MAG_int-(double)OFFSET)*1.2*10.0/32768;}

void ReadAllMV2()
{
  SPI.setDataMode(SPI_MODE0);
  ReadSamples();
  AllMV2Scaled[0] = MagScale(MAG[0], MAG_offset[0]) * 1000;
  AllMV2Scaled[1] = MagScale(MAG[1], MAG_offset[1]) * 1000;
  AllMV2Scaled[2] = MagScale(MAG[2], MAG_offset[2]) * 1000;
}

void LoopMV2Test()
{
  char ch = Serial.read();
  switch (ch) {
    case '0': ReadMV2(0); break;
    case '1': ReadMV2(1); break;
    case '2': ReadMV2(2); break;
  }
  ReadSamples();
  dprintf("Samples: %5u %5u %5u  (%.3fT, %.3fT, %.3fT)\n",
    MAG[0], MAG[1], MAG[2],
    MagScale(MAG[0], MAG_offset[0]), MagScale(MAG[1], MAG_offset[1]), MagScale(MAG[2], MAG_offset[2]));
  delay(1000);
}

//************************************************************************************
// Setup and Loop.
//

// the setup function runs once when you press reset or power the board
void setup()
{
  Serial.begin(115200);

  // Laser. Gnd is 16 and must stay low.
  pinMode(16, OUTPUT);
  pinMode(17, OUTPUT);
  digitalWrite(16, 0);
  digitalWrite(17, 0);
  
  // CAREFUL!! Initing the 9250 seems to mess up some of the digital I/O pins,
  // changing whether they are inputs or outputs. Not sure why.
  // So I'm putting it near the top of setup().
  //
  // AH, even more CAREFUL!! Pin 13 is both the LED and the SPI clock!
  // That was pretty dumb of them to do! No wonder things are confused.
//  Init9250();

  // initialize digital pin 13 as an output. LED.
  pinMode(13, OUTPUT);

  dprintf("Why is it not working??\n");

  SPI.begin(); // Just call this once, not in each setup().
  
  // Test the MV2 magnetic sensor chip.
  SetupMV2();
//  TestMV2();
//  return;
  
  // Use Pin 51 to output the received FSYNC value.
//  pinMode(51, OUTPUT);

  // Use Pin to output a "sync" pulse every several interrupts, so we can correlate things with particular samples (like looking at the FSYNC values).  Serial.begin(9600);
  pinMode(SYNC_PIN, OUTPUT);
  digitalWrite(SYNC_PIN, 0);

  // For looking at interrupt timing on oscilloscope.
//  pinMode(IRQ_SCOPE_PIN, OUTPUT);
//  digitalWrite(IRQ_SCOPE_PIN, 0);

  // For sending via UART to Point Grey camera.
  pinMode(0, INPUT);
  Serial1.begin(230400);

  for(int i=0; i < 50; ++i) {
    if(Serial)
      break;
    delay(100);
  }
    
  dprintf("Test of printf, number %d, %d\n", 123, 543);
  Init9250();
  
//  pinMode(13, OUTPUT);
//  analogWriteResolution(12);
  pinMode(MPU_IRQ_PIN, INPUT);
  attachInterrupt(MPU_IRQ_PIN, MPU_interrupt, RISING);

  pinMode(PTGREY_16FRAME_STROBE_PIN, INPUT);

  pinMode(PTGREY_FRAME_STROBE_PIN, INPUT);
  attachInterrupt(PTGREY_FRAME_STROBE_PIN, PTGREY_interrupt, RISING);
}

// the loop function runs over and over again forever
unsigned char countup=0;

int LASERtoggle = 0;
int count=0;
void loop() {

//LoopMV2Test();
//return;


  /*
    short val = MPU_read16(MPU9250_ACCEL_XOUT_H);
    analogWrite(DAC0, val/2+2048);
    delay(1);
    return;
  */

//  if (Serial.available())
//    ProcessCommand();

//  digitalWrite(13, HIGH);   // turn the LED on (HIGH is the voltage level)
//  digitalWrite(IRQ_SCOPE_PIN, 1);
//  delay(200);              // wait for a second
//  digitalWrite(13, LOW);    // turn the LED off by making the voltage LOW
//  digitalWrite(IRQ_SCOPE_PIN, 0);
//  delay(200);              // wait for a second

  delay(500);

  // FOR TESTING ONLY!! Comment out otherwise.
  //PTGREY_interrupt();
  
  dprintf("%d  Frm#:%d  MPUIRQ:%d accel: (%6d %6d %6d), Mag(Tesla*1000) (%6d, %6d, %6d)\n",
    count++, FrameCount, IRQcount,
    CurSamp[0], CurSamp[1], CurSamp[2],
    AllMV2Scaled[0], AllMV2Scaled[1], AllMV2Scaled[2]);
  
//  Serial1.write(countup++);

//  digitalWrite(17, LASERtoggle);
//  LASERtoggle = !LASERtoggle;


  while(Serial1.available()) {
    char inbyte = Serial1.read();
    dprintf("From camera: %d  \n", inbyte);
    switch(inbyte) {
      case 10: digitalWrite(17, 0); break;
      case 11: digitalWrite(17, 1); break;
    }
  }
  

//  dprintf("Accel: %6d  IRQ %d\n", MPU_read16(MPU9250_ACCEL_XOUT_H), IRQcount);
//if(!(countup%50))

//  dprintf("Frame %d, SYNC %d, Accel: %6d (0x%04x)  IRQ %d\n",
//    FrameCount>>1, FrameCount&1, accel, accel, IRQcount);

//  dprintf("acc[%6d %6d %6d]      gyro[%6d %6d %6d]\n",
//          CurSamp[0], CurSamp[1], CurSamp[2], CurSamp[3], CurSamp[4], CurSamp[5]);
}

