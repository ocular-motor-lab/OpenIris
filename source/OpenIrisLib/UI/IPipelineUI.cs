using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for pipelines
    /// </summary>
    public interface IPipelineUI
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBox"></param>
        /// <param name="dataAndImages"></param>
        void UpdatePipelineUI(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages);
    }
}
