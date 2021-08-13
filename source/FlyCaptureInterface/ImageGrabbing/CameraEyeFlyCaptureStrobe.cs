using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIris.ImageGrabbing
{

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0414 // Remove unused private members
    public partial class CameraEyeFlyCapture
    {
        /// <summary>
        /// 32-bit offset of the Strobe output signal CSRs 
        /// from the base address of initial register space.
        /// 
        /// To calculate the base address for an offset CSR: 
        /// 1. Query the offset inquiry register. 
        /// 2. Multiple the value by 4. (The value is a 32-bit offset.) 
        /// 3. Remove the 0xF prefix from the result. (i.e., F70000h becomes 70000
        /// </summary>
        uint STROBE_OUTPUT_CSR_INQ = 0x48C;

        /// <summary>
        /// Base address for the strobe registers
        /// </summary>
        uint STROBEBASE;

        /// <summary>
        /// Presence of strobe 0 signal
        /// </summary>
        uint STROBE_0_INQ { get { return STROBEBASE + 0x100; } }

        /// <summary>
        /// Presence of strobe 1 signal
        /// </summary>
        uint STROBE_1_INQ { get { return STROBEBASE + 0x104; } }

        /// <summary>
        /// Presence of strobe 2 signal
        /// </summary>
        uint STROBE_2_INQ { get { return STROBEBASE + 0x108; } }

        /// <summary>
        /// Presence of strobe 3 signal
        /// </summary>
        uint STROBE_3_INQ { get { return STROBEBASE + 0x10C; } }

        /// <summary>
        /// Presence_Inq [0] Presence of this feature
        /// [1-3] Reserved
        /// ReadOut_Inq [4] Ability to read the value of this feature
        /// On_Off_Inq [5] Ability to switch feature ON and OFF
        /// Polarity_Inq [6] Ability to change signal polarity
        /// [7] Reserved
        /// Min_Value [8-19] Minimum value for this feature control
        /// Max_Value [20-31] Maximum value for this feature control
        /// </summary>
        uint STROBE_0_CNT { get { return STROBEBASE + 0x200; } }

        /// <summary>
        /// Same definition as STROBE_0_CNT.
        /// </summary>
        uint STROBE_1_CNT { get { return STROBEBASE + 0x204; } }

        /// <summary>
        /// Same definition as STROBE_0_CNT.
        /// </summary>
        uint STROBE_2_CNT { get { return STROBEBASE + 0x208; } }

        /// <summary>
        /// Same definition as STROBE_0_CNT.
        /// </summary>
        uint STROBE_3_CNT { get { return STROBEBASE + 0x20C; } }

        /// <summary>
        /// This register provides control over a shared 4-bit counter with 
        /// programmable period. When the Current_Count equals N a GPIO pin 
        /// will only output a strobe pulse if bit[N] of the GPIO_STRPAT_MASK_PIN_x 
        /// register’s Enable_Pin field is setto ‘1’
        /// </summary>
        uint GPIO_STRPAT_CTRL = 0x110C;

        /// <summary>
        /// These registers define the actual strobe pattern to be implemented by 
        /// GPIO pins in conjunction with the Count_Period defined in GPIO_STRPAT_CTRL 
        /// register 110Ch. For example, if Count_Period is set to ‘3’, bits 16-18 of the 
        /// Enable_Mask can be used to define a strobe pattern. An example strobe pattern 
        /// might be bit 16=0, bit 17=0, and bit 18=1, which will cause a strobe to occur 
        /// every three frames (when the Current_Count is equal to 2).
        /// </summary>
        uint GPIO_STRPAT_MASK_PIN_0 = 0x1118;
        uint GPIO_STRPAT_MASK_PIN_1 = 0x1128;
        uint GPIO_STRPAT_MASK_PIN_2 = 0x1138;
        uint GPIO_STRPAT_MASK_PIN_3 = 0x1148;

        // GPIO registers
        uint GPIO_CTRL_PIN_0 = 0x1110;
        uint GPIO_CTRL_PIN_1 = 0x1120;
        uint GPIO_CTRL_PIN_2 = 0x1130;
        uint GPIO_CTRL_PIN_3 = 0x1140;

        public void InitStrobe()
        {
            // Same for the strobe interface registers.
            var STROBEBASE_absolute = ReadRegister(0x48c) * 4;
            STROBEBASE = STROBEBASE_absolute & 0x000fffff;

            // The Point Grey register manual seems to indicate that the mode should
            // be set to 3, but when I read back the registers, I see a mode value of 8,
            // so that is what I am setting it to here, but not sure why.
            WriteRegister(GPIO_STRPAT_CTRL, 0x00080002);
            WriteRegister(GPIO_CTRL_PIN_1, 0x00080002);

            // Set strobe on and set polarity to high
            WriteRegister(STROBE_0_CNT, 0x03000000);
            WriteRegister(STROBE_1_CNT, 0x03000000);

            // Set Pin 0 to output strobe every 16 frames.
            WriteRegister(GPIO_STRPAT_CTRL, 16);   // Set period to 16 frames.
            WriteRegister(GPIO_STRPAT_MASK_PIN_0, 0x8000);   // Set 1 of the 16 bits in the strobe mask.
        }
    }
}
