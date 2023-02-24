using OpenIris;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinnakerInterface
{
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Spinnaker Single Camera - RS Test", typeof(EyeTrackingSystemSettings))]

    class EyeTrackingSystemSpinnaker_SingleCam : EyeTrackingSystem
    {

    }
}
