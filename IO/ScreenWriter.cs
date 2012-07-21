using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace BrickEmulator.IO
{
    enum WriteType
    {
        Incoming,
        Outgoing
    }

    /// <summary>
    /// A handler of the Console's text (Joins it)
    /// </summary>
    class ScreenWriter
    {
        #region Fields
        private readonly Object LockObject = new Object();
        private readonly Object TitleLockObject = new Object();

        private PaintLogger PaintLogger = new PaintLogger();

        private TextWriter Writer;
        private TextReader Reader;
        #endregion

        #region Constructors
        public ScreenWriter()
        {
            Writer = TextWriter.Synchronized(Console.Out);
            Reader = TextReader.Synchronized(Console.In);

            Console.SetOut(Writer);
            Console.SetIn(Reader);

            Console.InputEncoding = BrickEngine.GetEncoding();
            Console.OutputEncoding = BrickEngine.GetEncoding();

            Application.SetCompatibleTextRenderingDefault(true);
        }
        #endregion

        #region Methods
        public void ScretchLine(string Line, WriteType Type)
        {
            using (TextWriter Writer = this.Writer)
            {
                lock (Writer)
                {
                    string Format = string.Format(" [{0}] {1} {2}", DateTime.Now.ToShortTimeString(), GetCharForLine(Type), Line);

                    Writer.WriteLine(Format);

                    BrickEngine.GetMemoryWriter().ImportLine(Format);
                }
            }

            PaintLogger.Clear();
        }

        private char GetCharForLine(WriteType Type)
        {
            if (Type.Equals(WriteType.Outgoing))
            {
                return '»';
            }

            return '«';
        }

        public void ScretchTitle(string Title)
        {
            if (Console.Title.Equals(Title))
            {
                return;
            }

            lock (Console.Title)
            {
                Console.Title = Title;
            }
        }

        public void ScretchStandardLine(string Line)
        {
            using (TextWriter Writer = this.Writer)
            {
                lock (Writer)
                {
                    Writer.WriteLine(Line);
                }
            }

            PaintLogger.Clear();
        }

        public void KeepAlive()
        {
            lock (Reader)
            {
                try
                {
                    Reader.ReadToEnd();
                }
                catch
                {
                    Environment.Exit(0);
                }
            }
        }

        public void PaintScreen(ConsoleColor Color, PaintType Type)
        {
            lock (PaintLogger)
            {
                if (Type.Equals(PaintType.ForeColor))
                {
                    PaintLogger.UpdateForePaint(Color);
                }
                else
                {
                    PaintLogger.UpdateBackPaint(Color);
                }
            }
        }
        #endregion
    }

    enum PaintType
    {
        ForeColor,
        BackColor
    }

    /// <summary>
    /// A handler of the Console's color (fore- and background)'s color
    /// </summary>
    class PaintLogger
    {
        #region Fields
        private ConsoleColor CollectForeColor;
        private ConsoleColor CollectBackColor;
        private readonly Object PaintLock = new Object();
        #endregion

        #region Properties
        public ConsoleColor CurrentForeColor
        {
            get
            {
                return Console.ForegroundColor;
            }
            set
            {
                Console.ForegroundColor = value;
            }
        }

        public ConsoleColor CurrentBackColor
        {
            get
            {
                return Console.BackgroundColor;
            }
            set
            {
                Console.BackgroundColor = value;
            }
        }
        #endregion

        #region Constructors
        public PaintLogger()
        {
            Clear();
        }
        #endregion

        #region Methods
        public void UpdateForePaint(ConsoleColor Color)
        {
            lock (PaintLock)
            {
                CollectForeColor = @Color;
                POP();
            }
        }

        public void UpdateBackPaint(ConsoleColor Color)
        {
            lock (PaintLock)
            {
                CollectBackColor = @Color;
                POP();
            }
        }

        private void POP()
        {
            lock (PaintLock)
            {
                CurrentForeColor = @CollectForeColor;
                CurrentBackColor = @CollectBackColor;
            }
        }

        public Boolean Updated(PaintType Type, ConsoleColor ShouldBeColor)
        {
            if (Type.Equals(PaintType.ForeColor))
            {
                return CurrentForeColor.CompareTo(ShouldBeColor) > 0;
            }
            else
            {
                return CurrentBackColor.CompareTo(ShouldBeColor) > 0;
            }
        }

        public void Clear()
        {
            lock (PaintLock)
            {
                Console.ResetColor();
            }
        }

        #endregion
    }
}
