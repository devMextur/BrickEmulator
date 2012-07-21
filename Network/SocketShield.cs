using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using BrickEmulator.Security;
using BrickEmulator.Messages;
using System.Net;
using System.IO;

namespace BrickEmulator.Network
{
    /// <summary>
    /// A manual class that handles the Network Classes
    /// </summary>
    class SocketShield
    {
        #region Fields
        private SocketListener MainSocketListener;
        private SocketDefender SocketDefender = new SocketDefender();

        public List<SocketClient> Sessions;

        public int GetSessionId(SocketClient Client)
        {
            return Array.IndexOf(Sessions.ToArray(), Client);
        }

        public SocketShield()
        {
            Sessions = new List<SocketClient>();
        }

        #endregion

        #region Methods

        public void Start()
        {
            MainSocketListener = new SocketListener();
        }

        public void HandleIncoming(SocketInformation Info)
        {
            SocketClient Client = new SocketClient(Info);

            Sessions.Add(Client);

            Client.Start();

            BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.DarkGreen, IO.PaintType.ForeColor);
            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] Socket Handled: {1}", Client.GetDeliverId(), Client.GetIPAddress()), IO.WriteType.Incoming);
        }

        public void HandleLeaving(SocketClient Socket)
        {
            Sessions.Remove(Socket);
        }

        public SocketClient GetSocketClientByHabboId(int Id)
        {
            foreach (SocketClient Set in Sessions)
            {
                if (!Set.GetClient().IsValidUser)
                {
                    continue;
                }

                if (Set.GetClient().GetUser().HabboId == Id)
                {
                    return Set;
                }
            }

            return null;
        }

        public List<SocketClient> GetSocketsByIP(string IP)
        {
            var List = new List<SocketClient>();

            foreach (SocketClient Set in Sessions)
            {
                if (Set.GetClient().IPAddress.ToLower() == IP.ToLower())
                {
                    List.Add(Set);
                }
            }

            return List;
        }

        public SocketClient GetSocketClientByHabboName(string Name)
        {
            foreach (SocketClient Set in Sessions)
            {
                if (!Set.GetClient().IsValidUser)
                {
                    continue;
                }

                if (Set.GetClient().GetUser().Username.ToLower() == Name.ToLower())
                {
                    return Set;
                }
            }

            return null;
        }

        public void BroadcastResponse(Response Response)
        {
            foreach (SocketClient Set in Sessions)
            {
                if (!Set.GetClient().IsValidUser)
                {
                    continue;
                }

                Set.GetClient().SendResponse(Response);
            }
        }

        public SocketListener GetSocketListener()
        {
            return this.MainSocketListener;
        }

        public SocketDefender GetSocketDefender()
        {
            return SocketDefender;
        }

        #endregion
    }
}
