using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MySql.Data.MySqlClient;
using BrickEmulator.Security;

namespace BrickEmulator.Storage
{
    /// <summary>
    /// Database handler that makes clients for clients and requests for it.
    /// </summary>
    class DatabaseEngine
    {
        #region Fields
        private readonly SecurityCounter DeliverCounter = new SecurityCounter(0);

        protected string ConnectionString = string.Empty;

        public QueryReactor GetAvailableReactor()
        {
            return new QueryReactor(DeliverCounter.Next, ConnectionString);
        }
        #endregion

        #region Constructors
        public DatabaseEngine()
        {
        }
        #endregion

        #region Methods

        public void Initialize()
        {
            string outHost = BrickEngine.GetConfigureFile().CallStringKey("mysql.host");
            int outPort = BrickEngine.GetConfigureFile().CallIntKey("mysql.port");
            string outUsername = BrickEngine.GetConfigureFile().CallStringKey("mysql.username");
            string outPassword = BrickEngine.GetConfigureFile().CallStringKey("mysql.password");
            string outDatabase = BrickEngine.GetConfigureFile().CallStringKey("mysql.database");

            int outMinPool = BrickEngine.GetConfigureFile().CallIntKey("mysql.minpoolamount");
            int outMaxPool = BrickEngine.GetConfigureFile().CallIntKey("mysql.maxpoolamount");

            ConnectionString = GenerateString(outHost, outPort, outUsername, outPassword, outDatabase, outMinPool, outMaxPool).ConnectionString;

            if (outMinPool <= 0)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("MySQl Min Pool Amount is to low, check your settings.", IO.WriteType.Outgoing);
                return;
            }

            if (outMinPool > 30)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("MySQl Min Pool Amount is to high, check your settings.", IO.WriteType.Outgoing);
                return;
            }

            if (outMinPool >= outMaxPool)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("MySQl Min Pool Amount could not be higher than Max Pool Amount, check your settings.", IO.WriteType.Outgoing);
                return;
            }

            if (outMaxPool <= 0)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("MySQl Max Pool Amount is to low, check your settings.", IO.WriteType.Outgoing);
                return;
            }

            if (outMaxPool > 30)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("MySQl Max Pool Amount is to high, check your settings.", IO.WriteType.Outgoing);
                return;
            }

            BrickEngine.GetScreenWriter().ScretchLine("DatabaseEngine handles reactors at " + outHost, IO.WriteType.Outgoing);
        }

        public MySqlConnectionStringBuilder GenerateString
            (string Host, 
             int Port,
             string Username,
             string Password,
             string Database,
             int MinPooling,
             int MaxPooling)
        {
            MySqlConnectionStringBuilder StringBuilder = new MySqlConnectionStringBuilder();
            StringBuilder.Server = Host;
            StringBuilder.Port = Convert.ToUInt16(Port);
            StringBuilder.UserID = Username;
            StringBuilder.Password = Password;
            StringBuilder.Database = Database;
            StringBuilder.MinimumPoolSize = Convert.ToUInt32(MinPooling);
            StringBuilder.MaximumPoolSize = Convert.ToUInt32(MaxPooling);
            StringBuilder.Pooling = true;
            return StringBuilder;
        }

        #endregion
    }
}
