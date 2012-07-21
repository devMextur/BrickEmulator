using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrickEmulator.Utilities
{
    class ByteUtil
    {
        #region Methods
        public static byte[] ChompBytes(byte[] Bytes, int Offset, int numBytes)
        {
            int End = (Offset + numBytes);

            if (End > Bytes.Length)
            {
                End = Bytes.Length;
            }

            int chunkLength = End - numBytes;

            if (numBytes > Bytes.Length)
            {
                numBytes = Bytes.Length;
            }

            if (numBytes < 0)
            {
                numBytes = 0;
            }

            byte[] bzChunk = new byte[numBytes];

            for (int x = 0; x < numBytes; x++)
            {
                bzChunk[x] = Bytes[Offset++];
            }

            return bzChunk;
        }
        #endregion
    }
}
