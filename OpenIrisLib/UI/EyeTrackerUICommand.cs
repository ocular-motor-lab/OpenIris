//-----------------------------------------------------------------------
// <copyright file="EyeTrackerUICommand.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

#pragma warning disable CA1031 
    // Do not catch general exception types. This is the top layer below the UI. We catch errors
    // here because otherwise the application will crash.

    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Threading.Tasks;

    /// <summary>
    /// Parent abstract class for all eye tracker commands. The main objective of the command pattern
    /// here is to combine in one place the execution of the command and the control of when the
    /// command can be executed or not. The user interface elements will bind to the command to start
    /// its execution but will also be enabled or disabled automatically.
    /// </summary>
    public class EyeTrackerUICommand : ICommand
    {
        private static readonly Dictionary<object, EyeTrackerUICommand> bindings = new Dictionary<object, EyeTrackerUICommand>();

        static EyeTrackerUICommand()
        {
            Application.Idle += (o, e) =>
            {
                foreach (var command in EyeTrackerUICommand.bindings.Values)
                {
                    command.CanExecute();
                }
            };
        }

        private readonly Func<object?,Task> executeMethod;
        private readonly Func<bool> canExecuteMethod;

        private bool enabled;

        /// <summary>
        /// Binds the command to a control.
        /// </summary>
        /// <param name="control"></param>
        public void Bind(Control control)
        {
            if (control is null) throw new ArgumentNullException(nameof(control));

            bindings.Add(control, this);

            control.Enabled = enabled;
            control.Click += (o,e) => Execute(o);

            CanExecuteChanged += (ob, e) => control.Enabled = enabled;
        }

        /// <summary>
        /// Binds the command to a menu item.
        /// </summary>
        /// <param name="menuItem"></param>
        public void Bind(ToolStripMenuItem menuItem)
        {
            if (menuItem is null) throw new ArgumentNullException(nameof(menuItem));

            bindings.Add(menuItem, this);

            menuItem.Enabled = enabled;
            menuItem.Click += (o, e) => Execute(o);

            CanExecuteChanged += (ob, e) => menuItem.Enabled = enabled;
        }

        /// <summary>
        /// Initializes an instance of the class EyeTrackerCommand for commands that do not need parameters
        /// </summary>
        /// <param name="execute">Execute method.</param>
        /// <param name="canExecute">Can execute method.</param>
        public EyeTrackerUICommand(Func<object?, Task> execute, Func<bool> canExecute)
        {
            this.executeMethod = execute;
            this.canExecuteMethod = canExecute;
        }

        /// <summary>
        /// Event from when the can execute changed.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Checks if the command can be executed.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object? parameter = null)
        {
            var value = canExecuteMethod();

            if (enabled != value)
            {
                enabled = value;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }

            return value;
        }

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="parameter"></param>
        public async void Execute(object? parameter = null)
        {
            try
            {
                await executeMethod(parameter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ERROR: " + ex.Message,
                    "ERROR",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                System.Diagnostics.Trace.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}
