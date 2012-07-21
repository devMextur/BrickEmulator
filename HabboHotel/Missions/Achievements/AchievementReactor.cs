using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.HabboHotel.Missions.Achievements.Items;
using BrickEmulator.Storage;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Missions.Achievements
{
    class AchievementReactor
    {
        private Dictionary<int, AchievementCategory> AchievementsCategorys;
        private Dictionary<int, Achievement> Achievements;
        private List<AchievementGoal> AchievementGoals;

        private int CoupledAchievements = new int();

        public AchievementReactor() { }

        public void Prepare()
        {
            LoadAchievementsCategorys();
            LoadAchievements();
            LoadAchievementGoals();

            CoupleAchievements();
        }

        public void LoadAchievementsCategorys()
        {
            AchievementsCategorys = new Dictionary<int, AchievementCategory>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM achievements_categorys ORDER BY order_id ASC");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                AchievementCategory Category = new AchievementCategory(Row);

                AchievementsCategorys.Add(Category.Id, Category);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + AchievementsCategorys.Count + "] AchievementCategory(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadAchievements()
        {
            Achievements = new Dictionary<int, Achievement>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM achievements_items ORDER BY order_id ASC");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                Achievement Achievement = new Achievement(Row);

                Achievements.Add(Achievement.Id, Achievement);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Achievements.Count + "] Achievement(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadAchievementGoals()
        {
            AchievementGoals = new List<AchievementGoal>();

            DataTable Table = null;
           
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM achievements_goals");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                AchievementGoal AchievementGoal = new AchievementGoal(Row);

                AchievementGoals.Add(AchievementGoal);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + AchievementGoals.Count + "] AchievementGoal(s) cached.", IO.WriteType.Outgoing);
        }

        public void CoupleAchievements()
        {
            foreach (Achievement Achievement in Achievements.Values.ToList())
            {
                if (AchievementsCategorys.ContainsKey(Achievement.CategoryId))
                {
                    CoupledAchievements++;
                }
            }
        }

        public Response GetResponse(Client Client)
        {
            Response Response = new Response(436);
            Response.AppendInt32(CoupledAchievements);

            var SortedCategorys = (from Cat in AchievementsCategorys.Values.ToList() orderby Cat.OrderId ascending select Cat).ToList();

            foreach (AchievementCategory Category in SortedCategorys)
            {
                var SortedAchievements = (from Ach in GetAchievementsForCategory(Category.Id) orderby Ach.OrderId ascending select Ach).ToList();

                foreach (Achievement Achievement in SortedAchievements)
                {
                    Achievement.GetResponse(Response, Client);
                }
            }

            Response.AppendChar(2);

            return Response;
        }

        public void UpdateUsersAchievement(Client Client, int Id)
        {
            Achievement Achievement = GetAchievement(Id);

            if (Achievement == null)
            {
                return;
            }

            int FollowingLevel = Achievement.GetNextLevel(Client);

            if (FollowingLevel.Equals(-1))
            {
                return;
            }

            if (Achievement == null)
            {
                return;
            }

            // Doing Response
            Client.SendResponse(Achievement.GetUpdateResponse(Client));

            int NeedForLevel = Achievement.GetNeeded(FollowingLevel);

            if (Achievement.CompletedAchievement(Client))
            {
                return;
            }

            if (GetGotAmount(Client, Id) >= NeedForLevel)
            {
                // Update Achievementscore @ cache + response update
                Client.GetUser().AchievementScore += Achievement.GetScoreReward(FollowingLevel);
                BrickEngine.GetPacketHandler().GetAchievementScore(Client, null);

                // Update Pixels @ cache,database + response update
                Client.GetUser().Pixels += Achievement.GetPixelReward(FollowingLevel);
                Client.GetUser().UpdatePixels(true);

                // refresh user (achievementscore, badges) @ room
                if (Client.GetUser().IsInRoom)
                {
                    Client.GetUser().RefreshUser();
                }

                // Send Unlock Response
                Client.SendResponse(Achievement.GetUnlockResponse(Client));

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE users SET achievement_score = achievement_score + " + Achievement.GetScoreReward(FollowingLevel) + " WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("habboid", Client.GetUser().HabboId);
                    Reactor.ExcuteQuery();
                }

                // Update @database + cache
                if (FollowingLevel > 1)
                {
                    Client.GetUser().Achievements[Id]++;

                    using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                    {
                        Reactor.SetQuery("UPDATE user_achievements SET current_level = current_level + 1 WHERE user_id = @habboid AND achievement_id = @achievementid LIMIT 1");
                        Reactor.AddParam("habboid", Client.GetUser().HabboId);
                        Reactor.AddParam("achievementid", Id);
                        Reactor.ExcuteQuery();
                    }

                    BrickEngine.GetBadgeHandler().UpdateBadge(Client, Achievement.GetBadgeCode(FollowingLevel - 1), Achievement.GetBadgeCode(FollowingLevel));
                }
                else
                {
                    Client.GetUser().Achievements.Add(Id, 1);

                    using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                    {
                        Reactor.SetQuery("INSERT INTO user_achievements (user_id, achievement_id) VALUES (@habboid, @achievementid)");
                        Reactor.AddParam("habboid", Client.GetUser().HabboId);
                        Reactor.AddParam("achievementid", Id);
                        Reactor.ExcuteQuery();
                    }

                    BrickEngine.GetBadgeHandler().GiveBadge(Client, Achievement.GetBadgeCode(FollowingLevel));
                }

                // Friends Info
                BrickEngine.GetMessengerHandler().AlertFriends(Client.GetUser().HabboId, BrickEngine.GetMessengerHandler().GetAchievedResponse(Client.GetUser().HabboId, true, Achievement.GetBadgeCode(FollowingLevel)));

                // Streaming
                BrickEngine.GetStreamHandler().AddStream(Client.GetUser().HabboId, Users.Handlers.Messenger.Streaming.StreamType.AchievedAchievement, Achievement.GetBadgeCode(FollowingLevel));
            }
        }

        public int GetGotAmount(Client Client, int Id)
        {
            if (Id == 1)
            {
                return (Client.GetUser().ActivatedEmail) ? 1 : 0;
            }
            else if (Id == 3)
            {
                return BrickEngine.GetConvertor().ObjectToInt32(Client.GetUser().MinutesOnline);
            }
            else if (Id == 4)
            {
                return (Client.GetUser().ProgressedNewbie) ? 1 : 0;
            }
            else if (Id == 5)
            {
                return Convert.ToInt32((DateTime.Now - Client.GetUser().RegisteredDatetime).TotalDays);
            }
            else if (Id == 6)
            {
                return Client.GetUser().Tags.Count;
            }
            else if (Id == 7)
            {
                return (Client.GetUser().ActivatedEmail && Convert.ToInt32((DateTime.Now - Client.GetUser().RegisteredDatetime).TotalDays) >= 1) ? 1 : 0;
            }
            else if (Id == 8)
            {
                return (Client.GetUser().GetMembership() != null && Client.GetUser().GetMembership().MemberScale.Equals(1)) ? 1 : 0;
            }
            else if (Id == 9)
            {
                return (Client.GetUser().GetMembership() != null && Client.GetUser().GetMembership().MemberScale.Equals(0)) ? 1 : 0;
            }

            return 0;
        }

        public Achievement GetAchievement(int Id)
        {
            try { return Achievements[Id]; }
            catch { return null; }
        }

        public List<Achievement> GetAchievementsForCategory(int CatId)
        {
            var List = new List<Achievement>();

            foreach (Achievement Achievement in Achievements.Values.ToList())
            {
                if (Achievement.CategoryId.Equals(CatId))
                {
                    List.Add(Achievement);
                }
            }

            return List;
        }

        public List<AchievementGoal> GetAchievementGoalsForAchievement(int Id)
        {
            var List = new List<AchievementGoal>();

            foreach (AchievementGoal AchievementGoal in AchievementGoals)
            {
                if (AchievementGoal.AchievementId.Equals(Id))
                {
                    List.Add(AchievementGoal);
                }
            }

            return (from Goal in List orderby Goal.Level ascending select Goal).ToList();
        }

        public AchievementCategory GetCategory(int Id)
        {
            try { return AchievementsCategorys[Id]; }
            catch { return null; }
        }
    }
}
