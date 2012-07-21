using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using BrickEmulator.Security;

namespace BrickEmulator.Network.Site
{
    class SiteSocketListener : Socket, IDisposable
    {
        #region Fields
        private readonly IPAddress HostIP;
        private readonly int AcceptPort = 30001;

        private List<SiteSocket> Sockets = new List<SiteSocket>();

        private int ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

        private AsyncCallback IncomingClient;

        private SiteRequestHandler SiteRequestHandler = new SiteRequestHandler();
        #endregion

        #region Constructors
        public SiteSocketListener() : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            IPAddress outIP;

            if (!IPAddress.TryParse(BrickEngine.GetConfigureFile().CallStringKey("site.socket.host"), out outIP))
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("SiteSocket IP is invalid, check you configure file.", IO.WriteType.Outgoing);
                return;
            }

            HostIP = outIP;

            int outPort = BrickEngine.GetConfigureFile().CallIntKey("site.socket.port");

            if (outPort <= 0 || outPort > 35000)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("SiteSocket Port is invalid, check you configure file.", IO.WriteType.Outgoing);
                return;
            }

            AcceptPort = outPort;

            base.Blocking = false;
            base.Bind(new IPEndPoint(IPAddress.Any, outPort));
            base.Listen(1);

            IncomingClient = new AsyncCallback(HandleConnection);

            BeginListen();

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("Site Socket started on {0}", base.LocalEndPoint), IO.WriteType.Outgoing);
        }
        #endregion

        #region Methods
        private void BeginListen()
        {
            base.BeginAccept(IncomingClient, this);
        }

        private void HandleConnection(IAsyncResult Result)
        {
            try
            {
                SocketInformation SocketInfo = base.EndAccept(Result).DuplicateAndClose(ProcessId);

                SiteSocket i = new SiteSocket(SocketInfo);

                if (HostIP.Equals(i.GetIPAddress()))
                {
                    Sockets.Add(i);

                    i.Start();
                }
                else
                {
                    i.Stop();

                    GC.SuppressFinalize(i);
                }
            }
            catch { }
            finally
            {
                BeginListen();
            }
        }

        public void HandlePacket(string Packet)
        {
            if (!string.IsNullOrEmpty(Packet))
            {
                if (Packet.Contains('_'))
                {
                    SiteRequest Request = new SiteRequest(BrickEngine.CleanString(Packet));

                    SiteRequestHandler.HandleRequest(Request);
                }
            }
        }

        #endregion
    }
}
