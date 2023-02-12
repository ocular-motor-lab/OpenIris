//-----------------------------------------------------------------------
// <copyright file="SubjectiveTracker.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace SubjectiveTracker
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;

    [Serializable]
    public class SubjectiveTrackerData
    {
        public string[] VideoFiles;

        public ulong CurrentFrameNumber;

        public ObservableCollection<string> MarkerNames;

        public string SelectedMarker;

        public SubjectiveTrackerDataOptions Options;

        public List<MarkerData> MarkerData;

        public SubjectiveTrackerData()
        {
            this.CurrentFrameNumber = 0;
            this.MarkerNames = new ObservableCollection<string>();
            this.SelectedMarker = string.Empty;
            this.Options = new SubjectiveTrackerDataOptions();
            this.MarkerData = new List<MarkerData>();
        }

        public static SubjectiveTrackerData Load(string dataFile)
        {
            SubjectiveTrackerData data = null;

            try
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(SubjectiveTrackerData));

                using (System.IO.StreamReader file = new System.IO.StreamReader(dataFile))
                {
                    data = (SubjectiveTrackerData)reader.Deserialize(file);
                }

                return data;
            }
            catch (Exception)
            {
                System.Diagnostics.Trace.WriteLine("Error loading settings.");
            }

            return new SubjectiveTrackerData();
        }

        public void Save(string dataFile)
        {
            // Save the main file
            System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(typeof(SubjectiveTrackerData));

            var dir = System.IO.Directory.GetCurrentDirectory();

            using (var file = new System.IO.StreamWriter(dataFile))
            {
                writer.Serialize(file, this);
            }
        }
    }

    [Serializable]
    public class SubjectiveTrackerDataOptions
    {
        public string LeftClickAction;
        public string RightClickAction;
        public string CtrlLeftClickAction;
        public string CtrlRightClickAction;

        public bool AutoAdvanceLeftClick;
        public bool AutoAdvanceRightClick;
        public bool AutoAdvanceCtrlLeftClick;
        public bool AutoAdvanceCtrlRightClick;

        public int EveryOtherFrame;

        public SubjectiveTrackerDataOptions()
        {
            this.LeftClickAction = null;
            this.RightClickAction = null;
            this.CtrlLeftClickAction = null;
            this.CtrlRightClickAction = null;

            this.AutoAdvanceLeftClick = false;
            this.AutoAdvanceRightClick = false;
            this.AutoAdvanceCtrlLeftClick = false;
            this.AutoAdvanceCtrlRightClick = false;

            this.EveryOtherFrame = 1;
        }
    }

    [Serializable]
    public class MarkerData
    {
        public string MarkerName;
        public ulong FrameNumber;
        public Point Location;
        public LeftRight LeftRight;

        public string ToCSVString()
        {
          return  string.Format("{0},{1},{2},{3},{4}", MarkerName, FrameNumber, LeftRight, Location.X, Location.Y);
        }
    }

    public enum LeftRight
    {
        Left,
        Right
    }
}
