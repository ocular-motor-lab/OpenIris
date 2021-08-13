/-----------------------------------------------------------------------
// <copyright file="PositionTrackerNone.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2017 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace VORLab.VOG.ImageProcessing
{
    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Position tracker that does not additional calculations. It just outputs the same pupil position as it gets.
    /// </summary>
    public class PositionTrackerNone
    {
    }
}
