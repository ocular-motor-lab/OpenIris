//-----------------------------------------------------------------------
// <copyright file="EyeData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;

    /// <summary>
    /// Possible results of processing a frame.
    /// </summary>
    public enum ProcessFrameResult
    {
        /// <summary>
        /// Undefined result.
        /// </summary>
        Undefined,

        /// <summary>
        /// No errors data obtained correctly.
        /// </summary>
        Good,

        /// <summary>
        /// Pupil could not be found.
        /// </summary>
        MissingPupil,

        /// <summary>
        /// Pupil could not be found.
        /// </summary>
        Error,
    }

    /// <summary>
    /// Class containing of the eye movement data from one frame.
    /// </summary>
    /// <remarks>
    /// Best Practices on serialization from http://msdn.microsoft.com/en-us/library/ms229752(v=vs.110).aspx
    /// To ensure proper versioning behavior, follow these rules when modifying a type from version to version:
    /// Never remove a serialized field.
    /// Never apply the NonSerializedAttribute attribute to a field if the attribute was not applied to the field in the previous version.
    /// Never change the name or the type of a serialized field.
    /// When adding a new serialized field, apply the OptionalFieldAttribute attribute.
    /// When removing a NonSerializedAttribute attribute from a field (that was not serializable in a previous version), apply the OptionalFieldAttribute attribute.
    /// For all optional fields, set meaningful defaults using the serialization callbacks unless 0 or nullas defaults are acceptable.
    /// To ensure that a type will be compatible with future serialization engines, follow these guidelines:
    /// Always set the VersionAdded property on the OptionalFieldAttribute attribute correctly.
    /// Avoid branched versioning.</remarks>
    [Serializable]
    public class EyeData
    {
        /// <summary>
        /// Initializes a new instance of the EyeData class.
        /// </summary>
        public EyeData() 
        {
            Timestamp = ImageEyeTimestamp.Empty;
        }

        /// <summary>
        /// Gets or sets left or right eye.
        /// </summary>
        public Eye WhichEye { get; set; }

        /// <summary>
        /// Gets or sets the timestamp from the frame.
        /// </summary>
        public ImageEyeTimestamp Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the pupil position and shape information.
        /// </summary>
        public PupilData Pupil { get; set; }

        /// <summary>
        /// Position and size of the corneal reflections (glints) detected.
        /// </summary>
        public CornealReflectionData[]? CornealReflections {get;set;}

        /// <summary>
        /// Gets or sets the iris position and shape information.
        /// </summary>
        public IrisData Iris { get; set; }
        
        /// <summary>
        /// Gets or sets the torsion angle.
        /// </summary>
        public double TorsionAngle { get; set; }

        /// <summary>
        /// Gets or sets the eyelid points of the eyelid contours.
        /// </summary>
        public EyelidData? Eyelids { get; set; }

        /// <summary>
        /// Gets or sets a value of data quality.
        /// </summary>
        public double DataQuality { get; set; }

        /// <summary>
        /// Gets or sets the result of the processing.
        /// </summary>
        public ProcessFrameResult ProcessFrameResult { get; set; }

        /// <summary>
        /// Size of the image.
        /// </summary>
        public Size ImageSize { get; set; }


        /// <summary>
        /// Creates a copy of the Data.
        /// </summary>
        /// <returns></returns>
        public EyeData Copy()
        {
            return new EyeData()
            {
                WhichEye = WhichEye,
                Timestamp = Timestamp.Copy(),
                ImageSize = ImageSize,
                ProcessFrameResult = ProcessFrameResult,

                Pupil = Pupil,
                Iris = Iris,
                CornealReflections = CornealReflections,
                TorsionAngle = TorsionAngle,
                Eyelids = Eyelids?.Copy(),
                DataQuality = DataQuality,
            };

        }
    }
}
