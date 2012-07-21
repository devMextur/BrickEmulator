using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Users.Handlers.Membership
{
    class Membership
    {
        public readonly int Id;
        public readonly int UserId;
        public DateTime ActivatedTime;
        public int MonthAmount;
        public int MonthsPassedBasic;
        public int MonthsPassedVip;
        public int GiftsSelectedAmount;
        public int MemberScale;

        public Membership(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            ActivatedTime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
            MonthAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MonthsPassedBasic = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MonthsPassedVip = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MemberScale = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            GiftsSelectedAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public Membership(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            ActivatedTime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
            MonthAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MonthsPassedBasic = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MonthsPassedVip = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MemberScale = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            GiftsSelectedAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public Boolean CheckExpired()
        {
            return (TotDaysLeft <= 0);
        }

        public int TotDaysPassedBasic
        {
            get
            {
                int DaysPassedCurrent = 0;

                if (MemberScale == 0)
                {
                    int Days = (MonthAmount * 31);
                    DaysPassedCurrent = (Days - TotDaysLeft);
                }

                int ExtraPassed = (MonthsPassedBasic * 31);
                return DaysPassedCurrent + ExtraPassed;
            }
        }

        public int TotDaysPassedVip
        {
            get
            {
                int DaysPassedCurrent = 0;

                if (MemberScale == 1)
                {
                    int Days = (MonthAmount * 31);
                    DaysPassedCurrent = (Days - TotDaysLeft);
                }

                int ExtraPassed = (MonthsPassedVip * 31);
                return DaysPassedCurrent + ExtraPassed;
            }
        }

        public int TotMonthsLeft
        {
            get
            {
                TimeSpan Span = (DateTime.Now - ActivatedTime); // Shit between Now & Then

                // Activated + months will be equal. else this code is
                // fucked up.

                int Days = Span.Days;
                int MonthsPassed = (Days / 31);
                int Result = MonthAmount - MonthsPassed;

                if (Days <= 31)
                {
                    return 0;
                }

                return Result;
            }
        }

        public int MonthsLeft
        {
            get
            {
                return (TotDaysLeft / 31);
            }
        }

        public int TotDaysLeft
        {
            get
            {
                TimeSpan Span = (DateTime.Now - ActivatedTime); // Shit between Now & Then

                // Activated + months will be equal. else this code is
                // fucked up.

                int Days = Span.Days;
                int DaysAmount = (MonthAmount * 31);
                int Calculated = DaysAmount - Days;

                return Calculated;
            }
        }

        public int DaysLeft
        {
            get
            {
                return (TotDaysLeft - (MonthsLeft * 31));
            }
        }
    }
}
