using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using System.Threading;

namespace BrickEmulator.Network.Site
{
    class SiteRequest
    {
        #region Fields
        private int HeaderId = -1;
        private int UserId = -1;

        private int MiniPointer = 0;
        private int Pointer = 0;

        private string MainPacket = string.Empty;

        private ReaderWriterLock Lock = new ReaderWriterLock();
        #endregion

        #region Properties
        private string[] InfoSplit
        {
            get
            {
                return MainPacket.Split('_');
            }
        }

        private int RemainingLength
        {
            get
            {
                return InfoSplit.Length - Pointer;
            }
        }
        #endregion

        #region Constructors
        public SiteRequest(string Packet)
        {
            this.MainPacket = Packet;

            this.HeaderId = PopInt32();
            this.UserId = PopInt32();
        }
        #endregion

        #region Popper
        public Boolean PopBoolean()
        {
            return (PopString().ToUpper().Equals("I"));
        }

        public Int32 PopInt32()
        {
            Int32 Result = new Int32();

            try
            {
                Result = int.Parse(PopString());
            }
            catch { }

            return Result;
        }

        public string PopStringToEnd()
        {
            string Result = string.Empty;

            try
            {
                Result = MainPacket.Substring(MiniPointer);
            }
            catch { }

            return Result;
        }

        public string PopString()
        {
            string Result = string.Empty;

            try
            {
                Result = InfoSplit[Pointer];
            }
            catch { }

            MiniPointer += Result.Length;

            UpdatePointer();

            return Result;
        }

        private void UpdatePointer()
        {
            lock (Lock)
            {
                Pointer++;
            }

            MiniPointer++;
        }
        #endregion

        #region Methods
        public int GetHeader()
        {
            return HeaderId;
        }

        public int GetUserId()
        {
            return UserId;
        }
        #endregion
    }
}
