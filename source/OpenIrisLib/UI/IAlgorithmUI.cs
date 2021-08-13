using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for algorithms
    /// </summary>
    public interface IAlgorithmUI
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBox"></param>
        /// <param name="dataAndImages"></param>
        void UpdateAlgorithmUI(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages);
    }
}
