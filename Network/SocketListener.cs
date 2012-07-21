using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace BrickEmulator.Network
{
    /// <summary>
    /// Stable socketlistener based on Socket <3
    /// </summary>
    class SocketListener : Socket
    {
        #region Fields

        private readonly AsyncCallback Async;

        private int ProcessId
        {
            get
            {
                return System.Diagnostics.Process.GetCurrentProcess().Id;
            }
        }

        #endregion

        #region Constructors

        public SocketListener() : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            IPAddress outIP;

            if (!IPAddress.TryParse(BrickEngine.GetConfigureFile().CallStringKey("sockethandler.ip"), out outIP))
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("Socket IP is invalid, check you configure file.", IO.WriteType.Outgoing);
                return;
            }

            int outPort = BrickEngine.GetConfigureFile().CallIntKey("sockethandler.port");

            if (outPort <= 0 || outPort > 35000)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("Socket Port is invalid, check you configure file.", IO.WriteType.Outgoing);
                return;
            }

            base.Bind(new IPEndPoint(outIP, outPort));

            int outBacklog = BrickEngine.GetConfigureFile().CallIntKey("sockethandler.backlog");

            if (outBacklog < 10)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("Socket backlog is to low, check you configure file.", IO.WriteType.Outgoing);
                return;
            }

            if (outBacklog > 100)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("Socket backlog is to hight, check you configure file.", IO.WriteType.Incoming);
                return;
            }

            Async = new AsyncCallback(AtNewConnection);

            base.Blocking = false;
            base.Listen(outBacklog);

            SearchSockets();

            BrickEngine.GetScreenWriter().ScretchLine("Socket started on " + base.LocalEndPoint, IO.WriteType.Outgoing);
        }

        #endregion

        #region Methods

        private void SearchSockets()
        {
            base.BeginAccept(Async, this);
        }

        private void AtNewConnection(IAsyncResult Result)
        {
            try
            {
                DateTime StartedHandling = DateTime.Now;

                if (!Object.ReferenceEquals(Result, null))
                {
                    KeyValuePair<SocketInformation, Boolean> kvp = GainSocket(Result);

                    if (kvp.Value)
                    {
                        BrickEngine.GetSocketShield().HandleIncoming(kvp.Key);
                    }
                }
            }
            catch { }
            finally
            {
                SearchSockets();
            }
        }

        private KeyValuePair<SocketInformation, Boolean> GainSocket(IAsyncResult Result)
        {
            if (Result.Equals(null))
            {
                return new KeyValuePair<SocketInformation, Boolean>(new SocketInformation(), false);
            }

            try
            {
                Socket Sock = base.EndAccept(Result);

                if (Sock == null)
                {
                    return new KeyValuePair<SocketInformation, Boolean>(new SocketInformation(), false);
                }

                if (BrickEngine.GetSocketShield().GetSocketDefender().Troubleshoot(Sock.RemoteEndPoint.ToString().Split(':')[0]))
                {
                    Sock.Shutdown(SocketShutdown.Both);
                    Sock.Dispose();
                    Sock.Close();

                    BrickEngine.GetProgressReactor().GetCollector().Finialize(Sock);

                    return new KeyValuePair<SocketInformation, Boolean>(new SocketInformation(), false);
                }

                return new KeyValuePair<SocketInformation, Boolean>(Sock.DuplicateAndClose(ProcessId), true);
            }
            catch { return new KeyValuePair<SocketInformation, Boolean>(new SocketInformation(), false); }
        }

        #endregion
    }
}
