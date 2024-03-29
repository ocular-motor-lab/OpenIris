﻿//-----------------------------------------------------------------------
// <copyright file="SliderTextControl.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Runtime;
    using System.Windows.Forms;

    /// <summary>
    /// User control that contains a text label, a slider and a text box. The slider and the text box are.
    /// Coordinated to as they represent the same value all the time.
    /// </summary>
    public partial class SliderTextControl : UserControl, IDisposable
    {
        /// <summary>
        /// Value represented in the slider and the text box.
        /// </summary>
        private double sliderValue;
        private RangeDouble range;
        private INotifyPropertyChanged? settingsForBinding;
        private string? settingNameForBinding;

        /// <summary>
        /// Initializes a new instance of the SliderTextControl class.
        /// </summary>
        public SliderTextControl()
        {
            InitializeComponent();

            sliderValue = 0;

            EnabledChanged += SliderTextControl_EnabledChanged;
        }

        /// <summary>
        /// Binds this slider with a particular setting.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="settingName"></param>
        public void Bind(INotifyPropertyChanged settings, string settingName )
        {
            Value = Convert.ToDouble(settings.GetType().GetProperty(settingName)?.GetValue(settings));
            ValueChanged += (o, e) =>
            {
                var propInfo = settings.GetType().GetProperty(settingName);
                propInfo?.SetValue(settings, Convert.ChangeType(Value, propInfo.PropertyType));
            };

            settingsForBinding = settings;
            settingNameForBinding = settingName;
            settings.PropertyChanged += settingsChangedHandler;
        }

        /// <summary>
        /// Handles the change of the bind setting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingsChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == settingNameForBinding)
            {
                Value = Convert.ToDouble(settingsForBinding?.GetType().GetProperty(settingNameForBinding)?.GetValue(settingsForBinding));
            }
        }


        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // This is VERY important so old sliders do not keep messing around with the setting
            if (settingsForBinding is not null)
            {
                settingsForBinding.PropertyChanged -= settingsChangedHandler;
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        void SliderTextControl_EnabledChanged(object sender, EventArgs e)
        {
            label.Enabled = Enabled;
            trackBar.Enabled = Enabled;
            numericUpDown1.Enabled = Enabled;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool EnableValueChangedEvent { get; set; } = true;

        /// <summary>
        /// Event raised when the value changes.
        /// </summary>
        public event EventHandler? ValueChanged;
        
        /// <summary>
        /// Gets or sets the name to be shown in the text label.
        /// </summary>
        [Browsable(true)]
        [Description("Name of the value represented."), Category("Data")]  
        public override string Text
        { 
            get{ return label.Text; }
            set { label.Text = value; }
        }

        /// <summary>
        /// Gets or sets the range of values allowed. 
        /// </summary>
        [Browsable(true)]
        [Description("Range of values allowed."), Category("Data")]
        public RangeDouble Range
        {
            get
            {
                return range;
            }
            set
            {
                range = value;

                trackBar.Minimum = 0;
                trackBar.Maximum = 100;

                numericUpDown1.Minimum = (decimal)range.Begin;
                numericUpDown1.Maximum = (decimal)range.End;

                numericUpDown1.DecimalPlaces = Range.End switch
                {
                    < 1 => 2,
                    < 20 => 1,
                    _ => 0,
                };
            }
        }

        /// <summary>
        /// Gets or sets the value of the number.
        /// </summary>
        [Browsable(true)]
        [Description("Current value."), Category("Data")]  
        public double Value
        {
            get { return sliderValue; }
            set
            {
                var oldValue = sliderValue;

                if (oldValue != value && !range.IsEmpty)
                {
                    if (!Range.Contains(value))
                    {
                        if ( value > range.End )
                        {
                            value = range.End;
                        }
                        if ( value < range.Begin)
                        {
                            value = range.Begin;
                        }
                    }

                    sliderValue = value;

                    trackBar.Value = (int)Math.Max(0, Math.Min(100, Math.Round((sliderValue - Range.Begin) *100.0 / (Range.End - Range.Begin))));
                    numericUpDown1.Value = (decimal)sliderValue;

                    if (EnableValueChangedEvent)
                    {
                        OnValueChanged(new EventArgs());
                    }
                }
            }
        }

        /// <summary>
        /// Raises the ValueChanged event.
        /// </summary>
        /// <param name="e">Event parameters.</param>
        protected void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            Value = trackBar.Value / 100.0 * (Range.End - Range.Begin) + Range.Begin;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Value = (double)numericUpDown1.Value;
        }
    }
}
