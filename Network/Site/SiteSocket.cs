using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using BrickEmulator.HabboHotel.Users.Handlers.Messenger;

namespace BrickEmulator.Network.Site
{
    class SiteSocket : Socket
    {
        #region Fields
        private const int ARRAY_BUFFER_SIZE = 512;

        private byte[] ArrayBuffer;

        private AsyncCallback ReceiveCallback;
        #endregion

        #region Constructors
        public SiteSocket(SocketInformation SocketInfo) : base(SocketInfo)
        {
            base.Blocking = false;
        }
        #endregion

        #region Methods
        public void Start()
        {
            this.ArrayBuffer = new byte[ARRAY_BUFFER_SIZE];
            this.ReceiveCallback = new AsyncCallback(IncomingPacket);

            GainData();
        }

        public void Stop()
        {
            Array.Clear(ArrayBuffer, 0, ARRAY_BUFFER_SIZE);

            try
            {
                base.Shutdown(SocketShutdown.Both);
                base.Dispose();
            }
            catch { }

            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        void GainData()
        {
            try
            {
                base.BeginReceive(ArrayBuffer, 0, ARRAY_BUFFER_SIZE, SocketFlags.None, ReceiveCallback, this);
            }
            catch { Stop(); }
        }

        private void IncomingPacket(IAsyncResult Result)
        {
            try
            {
                int ArrayBits = base.EndReceive(Result);

                if (ArrayBits > 0)
                {
                    string Packet = BrickEngine.GetEncoding().GetString(ArrayBuffer, 0, ArrayBits);

                    BrickEngine.GetSiteSocketListener().HandlePacket(Packet);
                }
            }
            catch { }
            finally
            {
                Stop();
            }
        }

        public IPAddress GetIPAddress()
        {
            return IPAddress.Parse(RemoteEndPoint.ToString().Split(':')[0]);
        }

        public int GetPort()
        {
            return int.Parse(RemoteEndPoint.ToString().Split(':')[1]);
        }
        #endregion
    }
}
