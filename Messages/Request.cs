using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Utilities;
using BrickEmulator.Security;

namespace BrickEmulator.Messages
{
    enum RequestType
    {
        Default,
        Policy
    }

    class Request
    {
        #region Fields
        private int PacketId = -1;
        private byte[] Context = new byte[0];
        private short Pointer = new short();

        public readonly RequestType Type = RequestType.Default; 
        #endregion

        #region Properties
        public int Id
        {
            get
            {
                return PacketId;
            }
        }

        public int Length
        {
            get
            {
                return Context.Length;
            }
        }

        public int RemainingLength
        {
            get
            {
                return Context.Length - Pointer;
            }
        }

        public string Header
        {
            get
            {
                return Encoding.Default.GetString(Base64Encoding.EncodeInt32(PacketId, 2));
            }
        }
        #endregion

        #region Constructors
        public Request(int PacketId, byte[] Context, RequestType Type)
        {
            this.Type = Type;

            if (!Context.Equals(null))
            {
                this.Context = Context;
            }

            if (PacketId > 0)
            {
                this.PacketId = PacketId;
            }
        }
        #endregion

        #region Methods

        public override string ToString()
        {
            return Header + Encoding.Default.GetString(Context);
        }

        public void ResetPointer()
        {
            Pointer = 0;
        }

        public void AdvancePointer(short i)
        {
            Pointer += i;
        }

        public string GetBody()
        {
            return Encoding.Default.GetString(Context);
        }

        public byte[] ReadBytes(int Bytes)
        {
            if (Bytes > this.RemainingLength)
            {
                Bytes = this.RemainingLength;
            }

            byte[] data = new byte[Bytes];

            for (int i = 0; i < Bytes; i++)
            {
                data[i] = Context[Pointer++];
            }

            return data;
        }

        public byte[] PlainReadBytes(int Bytes)
        {
            if (Bytes > RemainingLength)
            {
                Bytes = RemainingLength;
            }

            byte[] data = new byte[Bytes];

            for (int x = 0, y = Pointer; x < Bytes; x++, y++)
            {
                data[x] = Context[y];
            }

            return data;
        }

        public byte[] ReadFixedValue()
        {
            return ReadBytes(Base64Encoding.DecodeInt32(ReadBytes(2)));
        }

        public Boolean PopBase64Boolean()
        {
            if (RemainingLength > 0 && Context[Pointer++] == Base64Encoding.POSITIVE)
            {
                return true;
            }

            return false;
        }

        public Int32 PopInt32()
        {
            return Base64Encoding.DecodeInt32(ReadBytes(2));
        }

        public UInt32 PopUInt32()
        {
            return BrickEngine.GetConvertor().ObjectToUInt32( PopInt32() );
        }

        public string PopFixedString()
        {
            return PopFixedString(Encoding.Default);
        }

        public string PopFixedString(Encoding Encoding)
        {
            try
            {
                return Encoding.GetString(ReadFixedValue()).Replace(Convert.ToChar(1), ' ');
            }
            catch { return string.Empty; }
        }

        public Int32 PopFixedInt32()
        {
            Int32 i = 0;

            if (!Int32.TryParse(PopFixedString(Encoding.ASCII), out i))
            {
                return 0;
            }
            else
            {
                return i;
            }
        }

        public uint PopFixedUInt32()
        {
            return BrickEngine.GetConvertor().ObjectToUInt32( PopFixedInt32() );
        }

        public Boolean PopWiredBoolean()
        {
            if (this.RemainingLength > 0 && Context[Pointer++] == WireEncoding.POSITIVE)
            {
                return true;
            }

            return false;
        }

        public int PopWiredInt32()
        {
            if (RemainingLength < 1)
            {
                return new int();
            }

            byte[] Data = PlainReadBytes(WireEncoding.MAX_INTEGER_BYTE_AMOUNT);

            int TotalBytes = 0;
            int i = WireEncoding.DecodeInt32(Data, out TotalBytes);

            Pointer += BrickEngine.GetConvertor().ObjectToShort( TotalBytes );

            return i;
        }

        public uint PopWiredUInt()
        {
            return BrickEngine.GetConvertor().ObjectToUInt32( PopWiredInt32() );
        }

        #endregion
    }
}
