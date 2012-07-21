using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BrickEmulator.IO
{
    class MusicPlayer
    {
        private readonly string Path;
        private string Command = string.Empty;
        private Boolean Loop = false;
        private Boolean Started = false;

        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        public MusicPlayer(string Path, Boolean Loop)
        {
            this.Path = Path;
            this.Loop = Loop;

            Command = "open \"" + Path + "\" type mpegvideo alias MediaFile";

            mciSendString(Command, null, 0, IntPtr.Zero);
        }

        public void Start()
        {
            if (!Started)
            {
                Command = "play MediaFile";

                if (Loop)
                {
                    Command += " REPEAT";
                }

                mciSendString(Command, null, 0, IntPtr.Zero);
                Started = true;
            }
        }

        public void Stop()
        {
            if (Started)
            {
                Command = "close MediaFile";
                mciSendString(Command, null, 0, IntPtr.Zero);
                Started = false;
            }
        }
    }
}
