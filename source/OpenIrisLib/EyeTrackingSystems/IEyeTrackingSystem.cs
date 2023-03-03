//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using OpenIris.ImageGrabbing;
using System.Windows.Forms;

namespace OpenIris
{
    public interface IEyeTrackingSystem
    {
        string Name { get; }
        Form OpenEyeTrackingSystemUI { get; }
        EyeTrackingSystemSettings Settings { get; }

        EyeCollection<CameraEye> CreateCameras();
        IHeadDataSource CreateHeadDataSourceWithCameras();
        IHeadDataSource CreateHeadDataSourceWithVideos();
        EyeCollection<VideoEye> CreateVideos(EyeCollection<string> fileNames);
        void Dispose();
        ToolStripMenuItem[] GetToolStripMenuItems();
        EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData procesedImages);
        EyeCollection<ImageEye> PreProcessImages(EyeCollection<ImageEye> images);
        EyeCollection<ImageEye> PreProcessImagesFromCameras(EyeCollection<ImageEye> images);
        EyeCollection<ImageEye> PreProcessImagesFromVideos(EyeCollection<ImageEye> images);
    }
}