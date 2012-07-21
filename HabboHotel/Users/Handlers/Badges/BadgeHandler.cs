using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Storage;
using BrickEmulator.Security;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Badges
{
    class BadgeHandler
    {
        private List<Badge> Badges;
        private Dictionary<string, int> BadgeInfo;

        public BadgeHandler()
        {
            LoadBadges();
            LoadBadgeInfo();
        }

        public void LoadBadges()
        {
            Badges = new List<Badge>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_badges");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                Badge Badge = new Badge(Row);

                Badges.Add(Badge);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Badges.Count + "] Badge(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadBadgeInfo()
        {
            BadgeInfo = new Dictionary<string, int>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM badges_info");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                string Badge = BrickEngine.GetConvertor().ObjectToString(Row[0]);
                int BadgeId = BrickEngine.GetConvertor().ObjectToInt32(Row[1]);

                BadgeInfo.Add(Badge, BadgeId);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + BadgeInfo.Count + "] BadgeInfo(s) cached.", IO.WriteType.Outgoing);
        }

        public int GetIdForBadge(string Badge)
        {
            try { return BadgeInfo[Badge]; }
            catch { return -1; }
        }

        public List<Badge> GetBadgesForUser(int HabboId)
        {
            var List = new List<Badge>();

            foreach (Badge Badge in Badges)
            {
                if (Badge.OwnerId == HabboId)
                {
                    List.Add(Badge);
                }
            }

            return List;
        }

        public Badge GetBadge(string BadgeCode, int HabboId)
        {
            foreach (Badge Badge in GetBadgesForUser(HabboId))
            {
                if (Badge.BadgeCode.ToLower().Equals(BadgeCode.ToLower()))
                {
                    return Badge;
                }
            }

            return null;
        }

        public List<Badge> GetEquipedBadges(int HabboId)
        {
            var Badges = BrickEngine.GetBadgeHandler().GetBadgesForUser(HabboId);
            var Equiped = new Dictionary<int, Badge>();

            foreach (Badge Badge in Badges)
            {
                if (Badge.SlotId <= 0)
                {
                    continue;
                }

                if (Badge.SlotId > 5)
                {
                    continue;
                }

                if (!Equiped.ContainsKey(Badge.SlotId))
                {
                    if (Equiped.Count < 5)
                    {
                        Equiped.Add(Badge.SlotId, Badge);
                    }
                }
            }

            return Equiped.Values.ToList();
        }

        public void UpdateBadge(Client Client, string BadgeCode, string UpdateBadgeCode)
        {
            Badge Badge = GetBadge(BadgeCode, Client.GetUser().HabboId);

            if (Badge == null)
            {
                return;
            }

            // Update @ cache
            Badge.BadgeCode = UpdateBadgeCode;

            // Update @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_badges SET badge = @newbadge WHERE user_id = @habboid AND badge = @badge LIMIT 1");
                Reactor.AddParam("newbadge", UpdateBadgeCode);
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.AddParam("badge", BadgeCode);
                Reactor.ExcuteQuery();
            }

            if (GetIdForBadge(UpdateBadgeCode) > 0)
            {
                Response ShowItem = new Response(832);
                ShowItem.AppendInt32(1);
                ShowItem.AppendInt32(4); // TabID
                ShowItem.AppendInt32(1);
                ShowItem.AppendInt32(GetIdForBadge(BadgeCode));
                Client.SendResponse(ShowItem);

                BrickEngine.GetItemReactor().AddNewUpdate(GetIdForBadge(UpdateBadgeCode), 4, Client.GetUser().HabboId);
            }
        }

        public void GiveBadge(Client Client, string BadgeCode)
        {
            Dictionary<int, Object> Row = new Dictionary<int,object>();

            Row[1] = Client.GetUser().HabboId;
            Row[2] = BadgeCode;
            Row[3] = 0;

            // Add @ cache
            Badges.Add(new Badge(Row));

            // Add @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO user_badges (user_id, badge) VALUES (@habboid, @badge)");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.AddParam("badge", BadgeCode);
                Reactor.ExcuteQuery();
            }

            if (GetIdForBadge(BadgeCode) > 0)
            {
                Response ShowItem = new Response(832);
                ShowItem.AppendInt32(1);
                ShowItem.AppendInt32(4); // TabID
                ShowItem.AppendInt32(1);
                ShowItem.AppendInt32(GetIdForBadge(BadgeCode));
                Client.SendResponse(ShowItem);

                BrickEngine.GetItemReactor().AddNewUpdate(GetIdForBadge(BadgeCode), 4, Client.GetUser().HabboId);
            }
        }
    }
}
