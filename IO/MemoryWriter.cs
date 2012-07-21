using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace BrickEmulator.IO
{
    /// <summary>
    /// Writes Every 30 seconds the Writed Lines on file (.txt).
    /// </summary>
    class MemoryWriter
    {
        #region Fields
        private string Path = "logging/" + DateTime.Now.ToShortDateString() + ".txt";
        private StreamWriter FileWriter;

        private List<StringBuilder> LinesToWrite;

        private readonly Object WriteLock = new Object();
        private readonly Object ReadLock = new Object();

        private Timer Timer;
        #endregion

        #region Constructors
        public MemoryWriter() { HandleFiles(); }
        #endregion

        #region Methods

        public int StartLogging()
        {
            int outInterval = BrickEngine.GetConfigureFile().CallIntKey("writer.interval");

            if (outInterval < 30)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("Writer Interval is to low, check you configure file.", IO.WriteType.Outgoing);
                return -1;
            }

            if (outInterval > 100)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("Writer Interval is to high, check you configure file.", IO.WriteType.Outgoing);
                return -1;
            }

            Timer = new Timer(new TimerCallback(CheckLines), Timer, 0, 1000 * outInterval);

            return outInterval;
        }

        private void CheckLines(Object Obj)
        {
            lock (ReadLock)
            {
                foreach (StringBuilder Line in LinesToWrite.ToList())
                {
                    if (Line.Length > 0)
                    {
                        lock (WriteLock)
                        {
                            FileWriter.WriteLine(Line.ToString());
                            FileWriter.Flush();
                        }
                    }
                }

                LinesToWrite = new List<StringBuilder>();
            }
        }

        private void HandleFiles()
        {
            System.IO.Directory.CreateDirectory("Logging");

            if (!File.Exists(Path))
            {
                File.Create(Path).Dispose();
            }

            FileWriter = File.AppendText(Path);

            LinesToWrite = new List<StringBuilder>();
        }

        public void ImportLine(StringBuilder Builder)
        {
            lock (ReadLock)
            {
                LinesToWrite.Add(Builder);
            }
        }

        public void ImportLine(string Builder)
        {
            lock (ReadLock)
            {
                LinesToWrite.Add(new StringBuilder(Builder));
            }
        }

        #endregion
    }
}
