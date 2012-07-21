using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Utilities;
using BrickEmulator.Security;

namespace BrickEmulator.Messages
{
    class Response
    {
        #region Fields
        private readonly List<byte> Content = new List<byte>();
        #endregion

        #region Constructors
        public Response() { }

        public Response(int Header)
        {
            Initialize(Header);
        }
        #endregion

        #region Methods
        public void Initialize(int Header)
        {
            if (Content.Count > 1)
            {
                AppendChar(1);
            }

            AppendBytes(Base64Encoding.EncodeInt32(Header, 2));
        }

        public void AppendInt32(Int32 i)
        {
            AppendBytes(WireEncoding.EncodeInt32(i));
        }

        public void AppendUInt32(UInt32 i)
        {
            AppendBytes(WireEncoding.EncodeInt32(Convert.ToInt32(i)));
        }

        public void AppendRawInt32(int i)
        {
            AppendString(BrickEngine.GetConvertor().ObjectToString(i));
        }

        public void AppendRawUInt32(uint i)
        {
            AppendString(BrickEngine.GetConvertor().ObjectToString(i));
        }

        public void AppendBoolean(Boolean Bool)
        {
            AppendInt32((Bool) ? 1 : 0);
        }

        public void AppendString(string i)
        {
            AppendBytes(Encoding.Default.GetBytes(i));
        }

        public void AppendStringWithBreak(string i)
        {
            AppendString(i);
            AppendChar(2);
        }

        public void AppendChar(int i)
        {
            AppendBytes(new byte[] { BitConverter.GetBytes(Convert.ToChar(i))[0] });
        }

        public void AppendBytes(byte[] Bytes)
        {
            foreach (byte Byte in Bytes)
            {
                Content.Add(Byte);
            }
        }

        public byte[] GetBytes()
        {
            AppendChar(1);

            return Content.ToArray();
        }
        #endregion
    }
}
