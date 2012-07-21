using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;

using BrickEmulator.Utilities;
using BrickEmulator.Security;
using BrickEmulator.Network;

namespace BrickEmulator.HabboHotel.Users
{
    class Client
    {
        #region Fields
        private readonly SocketClient Socket;

        public string ConnectionCountry;

        private Boolean DisConnected = false;

        private UserCache UserCache = null;

        private readonly Object HandleLocker = new Object();

        public Boolean Authenticated = false;
        #endregion

        #region Properties
        public Boolean IsValidUser
        {
            get { return UserCache != null; }
        }

        public string IPAddress
        {
            get
            {
                return GetSocketClient().RemoteEndPoint.ToString().Split(':')[0];
            }
        }
        #endregion

        #region Constructors
        public Client(SocketClient Socket)
        {
            this.Socket = Socket;
        }
        #endregion

        #region Methods
        public void SendResponse(Response Response)
        {
            if (Response != null)
            {
                SendResponse(Response.GetBytes());

                if (BrickEngine.GetPacketHandler().ShowResponses)
                {
                    BrickEngine.GetScreenWriter().ScretchLine("[RESPONSE] : " + Response, IO.WriteType.Incoming);
                }
            }
        }

        public void SendResponse(byte[] Bytes)
        {
            if (Socket != null)
            {
                Socket.BeginSending(Bytes);
            }
        }

        public void SendRoomResponse(Response Response)
        {
            if (Response != null)
            {
                if (IsValidUser)
                {
                    if (GetUser().IsInRoom)
                    {
                        GetUser().GetRoom().GetRoomEngine().BroadcastResponse(Response);
                    }
                }
            }
        }

        public void LongNotif(string Builder)
        {
            Response Response = new Response(810);
            Response.AppendBoolean(true);
            Response.AppendStringWithBreak(Builder);
            SendResponse(Response);
        }

        public void LongNotif(StringBuilder Builder)
        {
            LongNotif(Builder.ToString());
        }

        public void Notif(StringBuilder Builder, bool Alert)
        {
            Notif(Builder.ToString(), Alert);
        }

        public void Notif(string Builder, bool Alert)
        {
            Response Response = new Response((Alert) ? 139 : 161);
            Response.AppendStringWithBreak(Builder);
            SendResponse(Response);
        }

        public void HandleRequest(ref byte[] Bytes)
        {
            string FixedPacket = BrickEngine.GetEncoding().GetString(Bytes);

            if (FixedPacket.Equals("<policy-file-request/>" + Convert.ToChar(0)))
            {
                Request HandlingRequest = new Request(-1, Bytes, RequestType.Policy);

                BrickEngine.GetPacketHandler().HandlePacket(HandlingRequest, this);

                if (BrickEngine.GetPacketHandler().ShowRequests)
                {
                    BrickEngine.GetScreenWriter().ScretchLine(string.Format("[Policy] : {0}", Socket.GetIPAddress()), IO.WriteType.Incoming);
                }
            }
            else if (FixedPacket.Contains('\x1'))
            {
                Dispose();
            }
            else
            {
                for (int Pointer = 0; Pointer < Bytes.Length; )
                {
                    int PacketLength = Base64Encoding.DecodeInt32(new byte[] { Bytes[Pointer++], Bytes[Pointer++], Bytes[Pointer++] });
                    int PacketId = Base64Encoding.DecodeInt32(new byte[] { Bytes[Pointer++], Bytes[Pointer++] });

                    int PacketRemainingLength = (PacketLength - 2);

                    byte[] ContextBytes = new byte[PacketRemainingLength];

                    if (PacketRemainingLength > 0)
                    {
                        for (int i = 0; i < PacketRemainingLength; i++)
                        {
                            ContextBytes[i] = Bytes[Pointer++];
                        }
                    }

                    Request HandlingRequest = new Request(PacketId, ContextBytes, RequestType.Default);

                    BrickEngine.GetPacketHandler().HandlePacket(HandlingRequest, this);
                }
            }
        }

        public void Dispose()
        {
            if (Socket != null)
            {
                Socket.Dispose();
            }

            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        public void AtDisconnect()
        {
            if (IsValidUser)
            {
                if (!DisConnected)
                {
                    GetUser().AtDisconnect();

                    BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.DarkCyan, IO.PaintType.ForeColor);
                    BrickEngine.GetScreenWriter().ScretchLine("[" + GetUser().HabboId + "] User " + GetUser().Username + " has leaved the building.", IO.WriteType.Outgoing);
                  
                    DisConnected = true;
                }
            }
        }

        public void ParseUser(UserCache User)
        {
            UserCache = User;
        }

        public SocketClient GetSocketClient()
        {
            return Socket;
        }

        public UserCache GetUser()
        {
            return UserCache;
        }
        #endregion
    }
}
