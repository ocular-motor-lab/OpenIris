using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIris.UI
{
    internal class QuickSettingsScroll : EyeTrackingPipelineUIControl
    {
        public QuickSettingsScroll(Eye whichEye)
            : base(whichEye)
        {

        }

        public override void UpdatePipelineEyeImage(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages)
        {
            throw new NotImplementedException();
        }

        public override void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages)
        {
            throw new NotImplementedException();
        }
    }
}
