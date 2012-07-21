using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using BrickEmulator.HabboHotel.Users;
using System.Net;

namespace BrickEmulator.Network
{
    /// <summary>
    /// Implements Receiving and Sending to the client.
    /// </summary>
    class SocketClient : Socket, IDisposable
    {
        #region Fields
        private const int ARRAY_BUFFER_SIZE = 512;

        private int DeliverId
        {
            get
            {
                return BrickEngine.GetSocketShield().GetSessionId(this);
            }
        }

        private AsyncCallback ReceiveCallback;
        private AsyncCallback SendCallback;

        private byte[] ArrayBuffer;

        private Client Client;

        #endregion

        #region Constructors

        public SocketClient(SocketInformation Info) : base(Info)
        {
            this.Client = new Client(this);

            this.ArrayBuffer = new byte[ARRAY_BUFFER_SIZE];

            base.Blocking = false;

            ReceiveCallback = new AsyncCallback(EndReceiving);
            SendCallback = new AsyncCallback(EndSending);
        }

        #endregion

        #region Methods

        #region MainHandling

        public void Start()
        {
            BeginReceiving();
        }

        private void ClearBuffer()
        {
            Array.Clear(ArrayBuffer, 0, ARRAY_BUFFER_SIZE);
        }

        private void Stop()
        {
            BrickEngine.GetSocketShield().HandleLeaving(this);

            if (Client != null)
            {
                Client.AtDisconnect();
            }

            try
            {
                base.Shutdown(SocketShutdown.Both);
                base.Dispose();
            }
            catch { }

            ClearBuffer();
        }

        #endregion

        #region Traffic

        public void BeginReceiving()
        {
            try
            {
                base.BeginReceive(ArrayBuffer, 0, ArrayBuffer.Length, SocketFlags.None, ReceiveCallback, this);
            }
            catch { Stop(); }
        }

        protected void EndReceiving(IAsyncResult Result)
        {
            try
            {
                int Bits = base.EndReceive(Result);

                if (Bits > 0)
                {
                    byte[] Bytes = BrickEmulator.Utilities.ByteUtil.ChompBytes(ArrayBuffer, 0, Bits);

                    Client.HandleRequest(ref Bytes);
                }
                else
                {
                    Stop();
                    return;
                }
            }
            catch { Stop(); return; }
            finally
            {
                ClearBuffer();

                BeginReceiving();
            }
        }

        public void BeginSending(byte[] Bytes)
        {
            try
            {
                base.BeginSend(Bytes, 0, Bytes.Length, SocketFlags.None, SendCallback, this);
            }
            catch { Stop(); }
        }

        protected void EndSending(IAsyncResult Result)
        {
            try
            {
                int Bits = 0;

                try
                {
                    Bits = base.EndSend(Result);
                }
                catch { Stop(); }

                if (Bits <= 0)
                {
                    Stop();
                }
            }
            catch { Stop(); }
        }

        #endregion

        #region Members

        private byte[] GetBytes(string String)
        {
            return BrickEngine.GetEncoding().GetBytes(String);
        }

        public int GetDeliverId()
        {
            return DeliverId;
        }

        public Client GetClient()
        {
            return Client;
        }

        public IPAddress GetIPAddress()
        {
            return IPAddress.Parse(RemoteEndPoint.ToString().Split(':')[0]);
        }

        #endregion

        #endregion

        #region IDisposable Methods

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
