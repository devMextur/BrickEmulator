using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using MySql.Data.MySqlClient;
using BrickEmulator.Security;
using System.Threading;

namespace BrickEmulator.Storage
{
    /// <summary>
    /// Structure to gain data from DatabaseServer with MySQLHelper
    /// </summary>
    struct QueryReactor : IDisposable
    {
        #region Fields
        public readonly int DeliverId;
        private string ConnectionString;

        private string CurrentQuery;
        private readonly ReaderWriterLock CommandLocker;

        private List<MySqlParameter> Parameters;
        private readonly SecurityCounter ParameterCounter;
        #endregion

        #region Constructors
        public QueryReactor(int DeliverId, string ConnectionString)
        {
            this.DeliverId = DeliverId;
            this.ConnectionString = ConnectionString;

            CurrentQuery = string.Empty;
            CommandLocker = new ReaderWriterLock();
            Parameters = new List<MySqlParameter>();
            ParameterCounter = new SecurityCounter(-1);
        }
        #endregion

        #region Methods

        public void SetQuery(string Query)
        {
            lock (CommandLocker)
            {
                CurrentQuery = Query;
            }
        }

        public UInt32 GetUInt32()
        {
            lock (CommandLocker)
            {
                UInt32 Result = new UInt32();

                try
                {

                    Result = BrickEngine.GetConvertor().ObjectToUInt32(MySqlHelper.ExecuteScalar(ConnectionString, CurrentQuery, Parameters.ToArray()));
                }
                catch { }

                return Result;
            }
        }

        public Int32 GetInt32()
        {
            lock (CommandLocker)
            {
                Int32 Result = new Int32();

                try
                {
                    Result = BrickEngine.GetConvertor().ObjectToInt32(MySqlHelper.ExecuteScalar(ConnectionString, CurrentQuery, Parameters.ToArray()));
                }
                catch { }

                return Result;
            }
        }

        public string GetString()
        {
            lock (CommandLocker)
            {
                string Result = string.Empty;

                try
                {
                    Result = BrickEngine.GetConvertor().ObjectToString(MySqlHelper.ExecuteScalar(ConnectionString, CurrentQuery, Parameters.ToArray()));
                }
                catch { }
                 
                return Result;
            }
        }

        public DataTable GetTable()
        {
            lock (CommandLocker)
            {
                DataTable Data = new DataTable();

                try
                {
                    Data = MySqlHelper.ExecuteDataset(ConnectionString, CurrentQuery, Parameters.ToArray()).Tables[0];
                }
                catch{ }

                return Data;
            }
        }

        public DataRow GetRow()
        {
            lock (CommandLocker)
            {
                DataRow Row = new DataTable().NewRow();

                try
                {
                    Row = MySqlHelper.ExecuteDataRow(ConnectionString, CurrentQuery, Parameters.ToArray());
                }
                catch { }

                return Row;
            }
        }

        public void ExcuteQuery()
        {
            DateTime Started = DateTime.Now;

            lock (CommandLocker)
            {
                try
                {
                    MySqlHelper.ExecuteNonQuery(ConnectionString, CurrentQuery, Parameters.ToArray());
                }
                catch (Exception e)
                {
                    BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                    BrickEngine.GetScreenWriter().ScretchLine("[" + DeliverId + "] QueryReactor exception: " + e.ToString(), IO.WriteType.Incoming);
                }
            }

            Console.WriteLine("doen query in " + (DateTime.Now - Started));
        }

        public void ExcuteQuery(string Query)
        {
            lock (CommandLocker)
            {
                SetQuery(Query);
                ExcuteQuery();
            }
        }

        public void AddParam(string Key, Object Value)
        {
            Parameters.Add(new MySqlParameter("@" + Key, Value.ToString()));
        }

        public void Dispose()
        {
            Array.Clear(Parameters.ToArray(), 0, Parameters.ToArray().Length);

            CurrentQuery = string.Empty;

            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        #endregion
    }
}
