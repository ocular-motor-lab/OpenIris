using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenIris.UI
{
    /// <summary>
    /// 
    /// </summary>
    public partial class GeneralForm : Form
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        public GeneralForm(object o)
        {
            var n = 0;
            foreach (FieldInfo FI in o.GetType().GetFields())
            {
                n++;

                FI.GetValue(o);
                FI.GetType();
                
                var l = new Label();
                l.Location = new Point(10, 10);
                l.Size = new Size(50, 500);
                l.Text = FI.Name;

                this.Controls.Add(l);

                switch(FI.GetType().Name)
                {
                    case "bool":
                        var c = new CheckBox();
                        c.Size = new Size(50, 50);
                        this.Controls.Add(c);
                        break;
                    case "int":
                        break;
                    case "string":
                        break;
                    default:
                        break;
                }
            }

            InitializeComponent();

        }
    }
}
