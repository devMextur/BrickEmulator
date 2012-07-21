using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using System.IO;

namespace BrickEmulator.Network
{
    /// <summary>
    /// Protects agains Socket-Ddos attacks from outside.
    /// </summary>
    /// <author>Breakz0ne</author>
    class SocketDefender
    {
        #region Fields

        private const string FilePath = "forced_ips.dat";

        /// <summary>
        /// Socket IP + Infractions amount
        /// </summary>
        private Dictionary<string, int> SocketInfo = new Dictionary<string, int>();

        private Dictionary<string, DateTime> SocketActivity = new Dictionary<string, DateTime>();

        private List<string> ForcedIPs;

        #endregion

        #region Constructors

        public SocketDefender()
        {
            LoadForcedIPs();
        }

        #endregion

        #region Methods

        private void LoadForcedIPs()
        {
            ForcedIPs = new List<string>();

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath);
            }
            else
            {
                foreach (string IP in File.ReadAllLines(FilePath))
                {
                    if (!ForcedIPs.Contains(IP))
                    {
                        ForcedIPs.Add(IP);
                    }
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + ForcedIPs.Count + "] ForcedIP(s) cached.", IO.WriteType.Outgoing);
        }

        public Boolean Troubleshoot(string IPAddress)
        {
            IPAddress = IPAddress.ToLower();

            if (!ForcedIPs.Contains(IPAddress))
            {
                if (!SocketActivity.ContainsKey(IPAddress))
                {
                    SocketActivity.Add(IPAddress, DateTime.Now);
                }
                else
                {
                    if ((DateTime.Now - SocketActivity[IPAddress]).TotalSeconds <= 5)
                    {
                        if (!SocketInfo.ContainsKey(IPAddress))
                        {
                            SocketInfo.Add(IPAddress, 1);
                        }
                        else
                        {
                            SocketInfo[IPAddress]++;
                        }

                        if (SocketInfo[IPAddress] >= 10)
                        {
                            HandleForcedIP(IPAddress);
                            return true;
                        }

                        SocketActivity[IPAddress] = DateTime.Now;
                    }
                    else
                    {
                        SocketActivity.Remove(IPAddress);
                        SocketInfo.Remove(IPAddress);
                    }
                }

                return false;
            }
            else
            {
                return ForcedIPs.Contains(IPAddress);
            }
        }

        private void HandleForcedIP(string IP)
        {
            // Write on cache
            ForcedIPs.Add(IP);

            // Write on InfoFile
            if (File.Exists(FilePath))
            {
                using (StreamWriter Writer = new StreamWriter(FilePath))
                {
                    Writer.WriteLine(IP);
                }
            }

            foreach (SocketClient Client in BrickEngine.GetSocketShield().GetSocketsByIP(IP))
            {
                Client.GetClient().Dispose();
            }

            BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
            BrickEngine.GetScreenWriter().ScretchLine("Forced IP Address: " + IP, IO.WriteType.Incoming);
        }

        #endregion
    }
}
