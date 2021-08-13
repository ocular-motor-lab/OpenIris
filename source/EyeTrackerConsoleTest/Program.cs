using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VORLab.VOG;
using VORLab.VOG.ImageGrabbing;

namespace EyeTrackerConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            EyeTracker eyeTracker = null;
            try
            {
                eyeTracker = new EyeTracker();
                var trackingTask = eyeTracker.StartTrackingAsync();

                System.Threading.Thread.Sleep(30000);
            }
            catch(Exception ex)
            {
                int a = 1;

            }
            finally
            {
                eyeTracker.StopTrackingAsync();
            }
        }
    }
}
