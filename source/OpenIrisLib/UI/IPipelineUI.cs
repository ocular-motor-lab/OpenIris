using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for pipelines, one per eye
    /// </summary>
    public interface IPipelineUI
    {
        Eye WhichEye { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBox"></param>
        /// <param name="dataAndImages"></param>
        void UpdatePipelineEyeimage(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages);

        void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages);
    }
}
