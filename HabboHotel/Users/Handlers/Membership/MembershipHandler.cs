using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Storage;

namespace BrickEmulator.HabboHotel.Users.Handlers.Membership
{
    class MembershipHandler
    {
        private Dictionary<int, Membership> MemberShips;

        public MembershipHandler()
        {
            LoadMemberShips();
        }

        public Membership GetCurrentMembership(int UserId)
        {
            try { return MemberShips[UserId]; }
            catch { return null; }
        }

        public void LoadMemberShips()
        {
            MemberShips = new Dictionary<int, Membership>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_memberships");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    Membership Membership = new Membership(Row);

                    if (!MemberShips.ContainsKey(Membership.UserId))
                    {
                        MemberShips.Add(Membership.UserId, Membership);
                    }
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + MemberShips.Count + "] Membership(s) cached.", IO.WriteType.Outgoing);
        }

        public void DeliverMembership(int UserId, int Type, int Months)
        {
            Membership Membership;
            DateTime Activated = DateTime.Now;

            // User has already a Membership
            if (MemberShips.ContainsKey(UserId))
            {
                Membership = MemberShips[UserId];

                if (Membership.MemberScale == Type)
                {
                    Membership.MonthAmount += Months;

                    using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                    {
                        Reactor.SetQuery("UPDATE user_memberships SET month_amount = month_amount + @months, member_scaler = @type WHERE user_id = @userid LIMIT 1");
                        Reactor.AddParam("userid", UserId);
                        Reactor.AddParam("months", Months);
                        Reactor.AddParam("type", Type);
                        Reactor.ExcuteQuery();
                    }
                }
                else
                {
                    Membership.ActivatedTime = Activated;
                    Membership.MonthAmount = Months;

                    using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                    {
                        Reactor.SetQuery("UPDATE user_memberships SET activated_datetime = @activated, month_amount = @months, member_scaler = @type WHERE user_id = @userid LIMIT 1");
                        Reactor.AddParam("userid", UserId);
                        Reactor.AddParam("activated", Activated);
                        Reactor.AddParam("months", Months);
                        Reactor.AddParam("type", Type);
                        Reactor.ExcuteQuery();
                    }
                }

                Membership.MemberScale = Type;
            }
            else
            {
                var Dic = new Dictionary<int, Object>();

                Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

                Dic[Counter.Next] = -1;
                Dic[Counter.Next] = UserId;
                Dic[Counter.Next] = Activated;
                Dic[Counter.Next] = Months;
                Dic[Counter.Next] = 0;
                Dic[Counter.Next] = 0;
                Dic[Counter.Next] = Type;
                Dic[Counter.Next] = 0;

                Membership = new Membership(Dic);
                MemberShips.Add(UserId, Membership);

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("INSERT INTO user_memberships (user_id, activated_datetime, month_amount, member_scaler) VALUES (@userid, @activated, @months, @type)");
                    Reactor.AddParam("userid", UserId);
                    Reactor.AddParam("activated", Activated);
                    Reactor.AddParam("months", Months);
                    Reactor.AddParam("type", Type);
                    Reactor.ExcuteQuery();
                }
            }
        }
    }
}
