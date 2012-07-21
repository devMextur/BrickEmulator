using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Tools
{
    class BannedItem
    {
        public readonly int UserId;
        public readonly string UserIP;
        public readonly Boolean IPBan;
        public readonly DateTime Started;
        public readonly DateTime Ended;
        public readonly int GivenModId;
        public readonly string Reason;

        public double DaysLeft
        {
            get
            {
                return (Ended - DateTime.Now).TotalDays;
            }
        }

        public BannedItem(int UserId, string UserIP, Boolean IPBan, DateTime Started, DateTime Ended, int GivenModId, string Reason)
        {
            this.UserId = UserId;
            this.UserIP = UserIP;
            this.IPBan = IPBan;
            this.Started = Started;
            this.Ended = Ended;
            this.GivenModId = GivenModId;
            this.Reason = Reason;
        }

        public BannedItem(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(0);

            this.UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.UserIP = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            this.IPBan = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.Started = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
            this.Ended = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
            this.GivenModId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.Reason = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }
    }
}
