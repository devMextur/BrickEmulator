using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Security;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace BrickEmulator.HabboHotel.Processing
{
    /// <summary>
    /// Collecter that will manage the objects in environment.
    /// </summary>
    class ProgressCollector : IDisposable
    {
        #region Fields

        private readonly Timer Timer;

        private List<Object> Finializers = new List<Object>();

        public ProgressCollector()
        {
            Timer = new Timer(new TimerCallback(Interact), Timer, 0, 5000);
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

        #endregion

        #region Methods
        public void Interact(Object Obj)
        {
            BrickEngine.GetScreenWriter().ScretchTitle(" [" + System.Diagnostics.Process.GetCurrentProcess().Id + "] [" + Environment.UserName + "] [" + (int)(DateTime.Now - BrickEngine.Started).TotalHours + "] Hours alive.");

            SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, (UIntPtr)0xFFFFFFFF, (UIntPtr)0xFFFFFFFF);

            GC.Collect();

            foreach (Object e in Finializers)
            {
                GC.SuppressFinalize(e);
            }

            Finializers = new List<Object>();
        }

        public void Finialize(Object e)
        { 
            Finializers.Add(e);
        }
        #endregion

        #region IDisposable Methods
        public void Dispose()
        {
            Timer.Dispose();

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
