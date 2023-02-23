//-----------------------------------------------------------------------
// <copyright file="SliderTextControl.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// User control that contains a text label, a slider and a text box. The slider and the text box are.
    /// Coordinated to as they represent the same value all the time.
    /// </summary>
    public partial class SliderTextControl : UserControl
    {
        /// <summary>
        /// Value represented in the slider and the text box.
        /// </summary>
        private int sliderValue;

        private Range range;

        /// <summary>
        /// Initializes a new instance of the SliderTextControl class.
        /// </summary>
        public SliderTextControl()
        {
            InitializeComponent();

            this.sliderValue = 0;

            this.EnabledChanged += SliderTextControl_EnabledChanged;
        }

        void SliderTextControl_EnabledChanged(object sender, EventArgs e)
        {
            this.label.Enabled = this.Enabled;
            this.trackBar.Enabled = this.Enabled;
            this.textBox.Enabled = this.Enabled;
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
        public Range Range
        {
            get
            {
                return range;
            }
            set
            {
                range = value;

                trackBar.Minimum = (int)range.Begin;
                trackBar.Maximum = (int)range.End;
            }
        }

        /// <summary>
        /// Gets or sets the value of the number.
        /// </summary>
        [Browsable(true)]
        [Description("Current value."), Category("Data")]  
        public int Value
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
                            value = (int)range.End;
                        }
                        if ( value < range.Begin)
                        {
                            value = (int)range.Begin;
                        }
                    }

                    sliderValue = value;

                    trackBar.Value = sliderValue;
                    textBox.Text = sliderValue.ToString();

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
            this.ValueChanged?.Invoke(this, e);
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            var result = int.TryParse(this.textBox.Text, out var value);
            if ( result)
            {
                this.Value = value;
            }
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            this.Value = this.trackBar.Value;
        }
    }
}
