using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPU
{
    [Obsolete]
    class MPU9250ConstantClass
    {

        public const int AFS_SEL = 0x00; //choose 0x00=2g, 0x01=4g, 0x02=8g, 0x03=16g 
        public const int FS_SEL = 0x01;  //choose 0x00=250dps, 0x01=500dps, 0x02=1000dps, 0x03=2000dps

        public const int MPU9250_SELF_TEST_X_GYRO = 0x00;
        public const int MPU9250_SELF_TEST_Y_GYRO = 0x01;
        public const int MPU9250_SELF_TEST_Z_GYRO = 0x02;

        public const int MPU9250_SELF_TEST_X_ACCEL = 0x0D;
        public const int MPU9250_SELF_TEST_Y_ACCEL = 0x0E;
        public const int MPU9250_SELF_TEST_Z_ACCEL = 0x0F;

        public const int MPU9250_XG_OFFSET_H = 0x13;
        public const int MPU9250_XG_OFFSET_L = 0x14;
        public const int MPU9250_YG_OFFSET_H = 0x15;
        public const int MPU9250_YG_OFFSET_L = 0x16;
        public const int MPU9250_ZG_OFFSET_H = 0x17;
        public const int MPU9250_ZG_OFFSET_L = 0x18;
        public const int MPU9250_SMPLRT_DIV = 0x19;
        public const int MPU9250_CONFIG = 0x1A;
        public const int MPU9250_GYRO_CONFIG = 0x1B;
        public const int MPU9250_ACCEL_CONFIG = 0x1C;
        public const int MPU9250_ACCEL_CONFIG2 = 0x1D;
        public const int MPU9250_LP_ACCEL_ODR = 0x1E;
        public const int MPU9250_WOM_THR = 0x1F;

        public const int MPU9250_FIFO_EN = 0x23;
        public const int MPU9250_I2C_MST_CTRL = 0x24;
        public const int MPU9250_I2C_SLV0_ADDR = 0x25;
        public const int MPU9250_I2C_SLV0_REG = 0x26;
        public const int MPU9250_I2C_SLV0_CTRL = 0x27;
        public const int MPU9250_I2C_SLV1_ADDR = 0x28;
        public const int MPU9250_I2C_SLV1_REG = 0x29;
        public const int MPU9250_I2C_SLV1_CTRL = 0x2A;
        public const int MPU9250_I2C_SLV2_ADDR = 0x2B;
        public const int MPU9250_I2C_SLV2_REG = 0x2C;
        public const int MPU9250_I2C_SLV2_CTRL = 0x2D;
        public const int MPU9250_I2C_SLV3_ADDR = 0x2E;
        public const int MPU9250_I2C_SLV3_REG = 0x2F;
        public const int MPU9250_I2C_SLV3_CTRL = 0x30;
        public const int MPU9250_I2C_SLV4_ADDR = 0x31;
        public const int MPU9250_I2C_SLV4_REG = 0x32;
        public const int MPU9250_I2C_SLV4_DO = 0x33;
        public const int MPU9250_I2C_SLV4_CTRL = 0x34;
        public const int MPU9250_I2C_SLV4_DI = 0x35;
        public const int MPU9250_I2C_MST_STATUS = 0x36;
        public const int MPU9250_INT_PIN_CFG = 0x37;
        public const int MPU9250_INT_ENABLE = 0x38;

        public const int MPU9250_INT_STATUS = 0x3A;
        public const int MPU9250_ACCEL_XOUT_H = 0x3B;
        public const int MPU9250_ACCEL_XOUT_L = 0x3C;
        public const int MPU9250_ACCEL_YOUT_H = 0x3D;
        public const int MPU9250_ACCEL_YOUT_L = 0x3E;
        public const int MPU9250_ACCEL_ZOUT_H = 0x3F;
        public const int MPU9250_ACCEL_ZOUT_L = 0x40;
        public const int MPU9250_TEMP_OUT_H = 0x41;
        public const int MPU9250_TEMP_OUT_L = 0x42;
        public const int MPU9250_GYRO_XOUT_H = 0x43;
        public const int MPU9250_GYRO_XOUT_L = 0x44;
        public const int MPU9250_GYRO_YOUT_H = 0x45;
        public const int MPU9250_GYRO_YOUT_L = 0x46;
        public const int MPU9250_GYRO_ZOUT_H = 0x47;
        public const int MPU9250_GYRO_ZOUT_L = 0x48;
        public const int MPU9250_EXT_SENS_DATA_00 = 0x49;
        public const int MPU9250_EXT_SENS_DATA_01 = 0x4A;
        public const int MPU9250_EXT_SENS_DATA_02 = 0x4B;
        public const int MPU9250_EXT_SENS_DATA_03 = 0x4C;
        public const int MPU9250_EXT_SENS_DATA_04 = 0x4D;
        public const int MPU9250_EXT_SENS_DATA_05 = 0x4E;
        public const int MPU9250_EXT_SENS_DATA_06 = 0x4F;
        public const int MPU9250_EXT_SENS_DATA_07 = 0x50;
        public const int MPU9250_EXT_SENS_DATA_08 = 0x51;
        public const int MPU9250_EXT_SENS_DATA_09 = 0x52;
        public const int MPU9250_EXT_SENS_DATA_10 = 0x53;
        public const int MPU9250_EXT_SENS_DATA_11 = 0x54;
        public const int MPU9250_EXT_SENS_DATA_12 = 0x55;
        public const int MPU9250_EXT_SENS_DATA_13 = 0x56;
        public const int MPU9250_EXT_SENS_DATA_14 = 0x57;
        public const int MPU9250_EXT_SENS_DATA_15 = 0x58;
        public const int MPU9250_EXT_SENS_DATA_16 = 0x59;
        public const int MPU9250_EXT_SENS_DATA_17 = 0x5A;
        public const int MPU9250_EXT_SENS_DATA_18 = 0x5B;
        public const int MPU9250_EXT_SENS_DATA_19 = 0x5C;
        public const int MPU9250_EXT_SENS_DATA_20 = 0x5D;
        public const int MPU9250_EXT_SENS_DATA_21 = 0x5E;
        public const int MPU9250_EXT_SENS_DATA_22 = 0x5F;
        public const int MPU9250_EXT_SENS_DATA_23 = 0x60;

        public const int MPU9250_I2C_SLV0_DO = 0x63;
        public const int MPU9250_I2C_SLV1_DO = 0x64;
        public const int MPU9250_I2C_SLV2_DO = 0x65;
        public const int MPU9250_I2C_SLV3_DO = 0x66;
        public const int MPU9250_I2C_MST_DELAY_CTRL = 0x67;
        public const int MPU9250_SIGNAL_PATH_RESET = 0x68;
        public const int MPU9250_MOT_DETECT_CTRL = 0x69;
        public const int MPU9250_USER_CTRL = 0x6A;
        public const int MPU9250_PWR_MGMT_1 = 0x6B;
        public const int MPU9250_PWR_MGMT_2 = 0x6C;

        public const int MPU9250_FIFO_COUNTH = 0x72;
        public const int MPU9250_FIFO_COUNTL = 0x73;
        public const int MPU9250_FIFO_R_W = 0x74;
        public const int MPU9250_WHO_AM_I = 0x75;
        public const int MPU9250_XA_OFFSET_H = 0x77;
        public const int MPU9250_XA_OFFSET_L = 0x78;

        public const int MPU9250_YA_OFFSET_H = 0x7A;
        public const int MPU9250_YA_OFFSET_L = 0x7B;

        public const int MPU9250_ZA_OFFSET_H = 0x7D;
        public const int MPU9250_ZA_OFFSET_L = 0x7E;

        //reset values
        public const int WHOAMI_RESET_VAL = 0x71;
        public const int POWER_MANAGMENT_1_RESET_VAL = 0x01;
        public const int DEFAULT_RESET_VALUE = 0x00;

        public const int WHOAMI_DEFAULT_VAL = 0x68;

        //CONFIG register masks
        public const int MPU9250_FIFO_MODE_MASK = 0x40;
        public const int MPU9250_EXT_SYNC_SET_MASK = 0x38;
        public const int MPU9250_DLPF_CFG_MASK = 0x07;

        //GYRO_CO;NFIG register masks
        public const int MPU9250_XGYRO_CTEN_MASK = 0x80;
        public const int MPU9250_YGYRO_CTEN_MASK = 0x40;
        public const int MPU9250_ZGYRO_CTEN_MASK = 0x20;
        public const int MPU9250_GYRO_FS_SEL_MASK = 0x18;
        public const int MPU9250_FCHOICE_B_MASK = 0x03;

        //ACCEL_CONFIG register masks
        public const int MPU9250_AX_ST_EN_MASK = 0x80;
        public const int MPU9250_AY_ST_EN_MASK = 0x40;
        public const int MPU9250_AZ_ST_EN_MASK = 0x20;
        public const int MPU9250_ACCEL_FS_SEL_MASK = 0x18;

        //ACCEL_CONFIG_2 register masks
        public const int MPU9250_ACCEL_FCHOICE_B_MASK = 0xC0;
        public const int MPU9250_A_DLPF_CFG_MASK = 0x03;

        //LP_ACCEL_ODR register masks
        public const int MPU9250_LPOSC_CLKSEL_MASK = 0x0F;

        //FIFO_EN register masks
        public const int MPU9250_TEMP_FIFO_EN_MASK = 0x80;
        public const int MPU9250_GYRO_XOUT_MASK = 0x40;
        public const int MPU9250_GYRO_YOUT_MASK = 0x20;
        public const int MPU9250_GYRO_ZOUT_MASK = 0x10;
        public const int MPU9250_ACCEL_MASK = 0x08;
        public const int MPU9250_SLV2_MASK = 0x04;
        public const int MPU9250_SLV1_MASK = 0x02;
        public const int MPU9250_SLV0_MASK = 0x01;

        //I2C_MST_CTRL register masks
        public const int MPU9250_MULT_MST_EN_MASK = 0x80;
        public const int MPU9250_WAIT_FOR_ES_MASK = 0x40;
        public const int MPU9250_SLV_3_FIFO_EN_MASK = 0x20;
        public const int MPU9250_I2C_MST_P_NSR_MASK = 0x10;
        public const int MPU9250_I2C_MST_CLK_MASK = 0x0F;

        //I2C_SLV0_ADDR register masks
        public const int MPU9250_I2C_SLV0_RNW_MASK = 0x80;
        public const int MPU9250_I2C_ID_0_MASK = 0x7F;

        //I2C_SLV0_CTRL register masks
        public const int MPU9250_I2C_SLV0_EN_MASK = 0x80;
        public const int MPU9250_I2C_SLV0_BYTE_SW_MASK = 0x40;
        public const int MPU9250_I2C_SLV0_REG_DIS_MASK = 0x20;
        public const int MPU9250_I2C_SLV0_GRP_MASK = 0x10;
        public const int MPU9250_I2C_SLV0_LENG_MASK = 0x0F;

        //I2C_SLV1_ADDR register masks
        public const int MPU9250_I2C_SLV1_RNW_MASK = 0x80;
        public const int MPU9250_I2C_ID_1_MASK = 0x7F;

        //I2C_SLV1_CTRL register masks
        public const int MPU9250_I2C_SLV1_EN_MASK = 0x80;
        public const int MPU9250_I2C_SLV1_BYTE_SW_MASK = 0x40;
        public const int MPU9250_I2C_SLV1_REG_DIS_MASK = 0x20;
        public const int MPU9250_I2C_SLV1_GRP_MASK = 0x10;
        public const int MPU9250_I2C_SLV1_LENG_MASK = 0x0F;

        //I2C_SLV2_ADDR register masks
        public const int MPU9250_I2C_SLV2_RNW_MASK = 0x80;
        public const int MPU9250_I2C_ID_2_MASK = 0x7F;

        //I2C_SLV2_CTRL register masks
        public const int MPU9250_I2C_SLV2_EN_MASK = 0x80;
        public const int MPU9250_I2C_SLV2_BYTE_SW_MASK = 0x40;
        public const int MPU9250_I2C_SLV2_REG_DIS_MASK = 0x20;
        public const int MPU9250_I2C_SLV2_GRP_MASK = 0x10;
        public const int MPU9250_I2C_SLV2_LENG_MASK = 0x0F;

        //I2C_SLV3_ADDR register masks
        public const int MPU9250_I2C_SLV3_RNW_MASK = 0x80;
        public const int MPU9250_I2C_ID_3_MASK = 0x7F;

        //I2C_SLV3_CTRL register masks
        public const int MPU9250_I2C_SLV3_EN_MASK = 0x80;
        public const int MPU9250_I2C_SLV3_BYTE_SW_MASK = 0x40;
        public const int MPU9250_I2C_SLV3_REG_DIS_MASK = 0x20;
        public const int MPU9250_I2C_SLV3_GRP_MASK = 0x10;
        public const int MPU9250_I2C_SLV3_LENG_MASK = 0x0F;

        //I2C_SLV4_ADDR register masks
        public const int MPU9250_I2C_SLV4_RNW_MASK = 0x80;
        public const int MPU9250_I2C_ID_4_MASK = 0x7F;

        //I2C_SLV4_CTRL register masks
        public const int MPU9250_I2C_SLV4_EN_MASK = 0x80;
        public const int MPU9250_SLV4_DONE_INT_EN_MASK = 0x40;
        public const int MPU9250_I2C_SLV4_REG_DIS_MASK = 0x20;
        public const int MPU9250_I2C_MST_DLY_MASK = 0x1F;

        //I2C_MST_STATUS register masks
        public const int MPU9250_PASS_THROUGH_MASK = 0x80;
        public const int MPU9250_I2C_SLV4_DONE_MASK = 0x40;
        public const int MPU9250_I2C_LOST_ARB_MASK = 0x20;
        public const int MPU9250_I2C_SLV4_NACK_MASK = 0x10;
        public const int MPU9250_I2C_SLV3_NACK_MASK = 0x08;
        public const int MPU9250_I2C_SLV2_NACK_MASK = 0x04;
        public const int MPU9250_I2C_SLV1_NACK_MASK = 0x02;
        public const int MPU9250_I2C_SLV0_NACK_MASK = 0x01;

        //INT_PIN_CFG register masks
        public const int MPU9250_ACTL_MASK = 0x80;
        public const int MPU9250_OPEN_MASK = 0x40;
        public const int MPU9250_LATCH_INT_EN_MASK = 0x20;
        public const int MPU9250_INT_ANYRD_2CLEAR_MASK = 0x10;
        public const int MPU9250_ACTL_FSYNC_MASK = 0x08;
        public const int MPU9250_FSYNC_INT_MODE_EN_MASK = 0x04;
        public const int MPU9250_BYPASS_EN_MASK = 0x02;

        //INT_ENABLE register masks
        public const int MPU9250_WOM_EN_MASK = 0x40;
        public const int MPU9250_FIFO_OFLOW_EN_MASK = 0x10;
        public const int MPU9250_FSYNC_INT_EN_MASK = 0x08;
        public const int MPU9250_RAW_RDY_EN_MASK = 0x01;

        //INT_STATUS register masks
        public const int MPU9250_WOM_INT_MASK = 0x40;
        public const int MPU9250_FIFO_OFLOW_INT_MASK = 0x10;
        public const int MPU9250_FSYNC_INT_MASK = 0x08;
        public const int MPU9250_RAW_DATA_RDY_INT_MASK = 0x01;

        //I2C_MST_DELAY_CTRL register masks
        public const int MPU9250_DELAY_ES_SHADOW_MASK = 0x80;
        public const int MPU9250_I2C_SLV4_DLY_EN_MASK = 0x10;
        public const int MPU9250_I2C_SLV3_DLY_EN_MASK = 0x08;
        public const int MPU9250_I2C_SLV2_DLY_EN_MASK = 0x04;
        public const int MPU9250_I2C_SLV1_DLY_EN_MASK = 0x02;
        public const int MPU9250_I2C_SLV0_DLY_EN_MASK = 0x01;

        //SIGNAL_PATH_RESET register masks
        public const int MPU9250_GYRO_RST_MASK = 0x04;
        public const int MPU9250_ACCEL_RST_MASK = 0x02;
        public const int MPU9250_TEMP_RST_MASK = 0x01;

        //MOT_DETECT_CTRL register masks
        public const int MPU9250_ACCEL_INTEL_EN_MASK = 0x80;
        public const int MPU9250_ACCEL_INTEL_MODE_MASK = 0x40;

        //USER_CTRL register masks
        public const int MPU9250_FIFO_EN_MASK = 0x40;
        public const int MPU9250_I2C_MST_EN_MASK = 0x20;
        public const int MPU9250_I2C_IF_DIS_MASK = 0x10;
        public const int MPU9250_FIFO_RST_MASK = 0x04;
        public const int MPU9250_I2C_MST_RST_MASK = 0x02;
        public const int MPU9250_SIG_COND_RST_MASK = 0x01;

        //PWR_MGMT_1 register masks
        public const int MPU9250_H_RESET_MASK = 0x80;
        public const int MPU9250_SLEEP_MASK = 0x40;
        public const int MPU9250_CYCLE_MASK = 0x20;
        public const int MPU9250_GYRO_STANDBY_CYCLE_MASK = 0x10;
        public const int MPU9250_PD_PTAT_MASK = 0x08;
        public const int MPU9250_CLKSEL_MASK = 0x07;

        //PWR_MGMT_2 register masks
        public const int MPU9250_DISABLE_XA_MASK = 0x20;
        public const int MPU9250_DISABLE_YA_MASK = 0x10;
        public const int MPU9250_DISABLE_ZA_MASK = 0x08;
        public const int MPU9250_DISABLE_XG_MASK = 0x04;
        public const int MPU9250_DISABLE_YG_MASK = 0x02;
        public const int MPU9250_DISABLE_ZG_MASK = 0x01;

        public const int MPU9250_DISABLE_XYZA_MASK = 0x38;
        public const int MPU9250_DISABLE_XYZG_MASK = 0x07;

        //Magnetometer register maps
        public const int MPU9250_MAG_WIA = 0x00;
        public const int MPU9250_MAG_INFO = 0x01;
        public const int MPU9250_MAG_ST1 = 0x02;
        public const int MPU9250_MAG_XOUT_L = 0x03;
        public const int MPU9250_MAG_XOUT_H = 0x04;
        public const int MPU9250_MAG_YOUT_L = 0x05;
        public const int MPU9250_MAG_YOUT_H = 0x06;
        public const int MPU9250_MAG_ZOUT_L = 0x07;
        public const int MPU9250_MAG_ZOUT_H = 0x08;
        public const int MPU9250_MAG_ST2 = 0x09;
        public const int MPU9250_MAG_CNTL = 0x0A;
        public const int MPU9250_MAG_RSV = 0x0B; //reserved mystery meat
        public const int MPU9250_MAG_ASTC = 0x0C;
        public const int MPU9250_MAG_TS1 = 0x0D;
        public const int MPU9250_MAG_TS2 = 0x0E;
        public const int MPU9250_MAG_I2CDIS = 0x0F;
        public const int MPU9250_MAG_ASAX = 0x10;
        public const int MPU9250_MAG_ASAY = 0x11;
        public const int MPU9250_MAG_ASAZ = 0x12;



    }
}
