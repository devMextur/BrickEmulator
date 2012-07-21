using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BrickEmulator.Security
{
    /// <summary>
    /// A Int32 ProgramCounter that updates a Number with a ReadWriterLock
    /// </summary>
    struct SecurityCounter
    {
        #region Fields

        private readonly ReaderWriterLock Lock;
        private Int32 Counter;

        public SecurityCounter(int StartAmount)
        {
            Lock = new ReaderWriterLock();

            lock (Lock)
            {
                Counter = StartAmount;
            }
        }

        #endregion

        #region Methods

        public void Skip()
        {
            int i = Next;
        }

        public void Update(int i)
        {
            lock (Lock)
            {
                Counter += i;
            }
        }

        public Int32 Next
        {
            get
            {
                lock (Lock)
                {
                    Counter++;
                    return Counter;
                }
            }
        }

        public void Clear()
        {
            lock (Lock)
            {
                Counter = new int();
            }
        }

        #endregion
    }
}
