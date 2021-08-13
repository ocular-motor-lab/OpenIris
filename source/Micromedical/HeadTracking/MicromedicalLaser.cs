using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenIris.ImageGrabbing;

namespace OpenIris.HeadTracking
{
    [Obsolete]
    public class MicromedicalLaser
    {
        private static uint ONbyteCommand = 11;
        private static uint OFFbyteCommand = 10;

        private CameraEyeFlyCapture cam;

        public MicromedicalLaser(CameraEyeFlyCapture cam)
        {
            this.cam = cam;
            this.TurnOFF();
        }

        public bool LaserON { get; private set; }

        public void TurnON()
        {
            this.cam.WriteSerialByte(ONbyteCommand);
            this.LaserON = true;
        }

        public void TurnOFF()
        {
            this.cam.WriteSerialByte(OFFbyteCommand);
            this.LaserON = false;
        }
    }
}
