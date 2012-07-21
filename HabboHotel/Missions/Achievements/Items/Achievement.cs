using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Missions.Achievements.Items
{
    class Achievement
    {
        public readonly int Id;
        public readonly int CategoryId;
        public readonly string BadgeBase;
        public readonly Boolean EnableLeveling;
        public readonly int PixelRewardBase;
        public readonly int ScoreRewardBase;
        public readonly int ExtraScore;
        public readonly int OrderId;

        public AchievementCategory Category
        {
            get
            {
                return BrickEngine.GetAchievementReactor().GetCategory(CategoryId);
            }
        }

        public int MaxLevel
        {
            get
            {
                if (Goals.Count > 0)
                {
                    return Goals[Goals.Count - 1].Level;
                }
                else
                {
                    return 1;
                }
            }
        }

        public List<AchievementGoal> Goals
        {
            get
            {
                return BrickEngine.GetAchievementReactor().GetAchievementGoalsForAchievement(Id);
            }
        }

        public Achievement(DataRow Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BadgeBase = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            EnableLeveling = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            PixelRewardBase = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            ScoreRewardBase = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            ExtraScore = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public void GetResponse(Response Response, Client Client)
        {
            int Next = GetNextLevel(Client);

            Response.AppendInt32(Id);
            Response.AppendInt32(Next);
            Response.AppendStringWithBreak(GetBadgeCode(Next));
            Response.AppendInt32(GetNeeded(Next));
            Response.AppendInt32(GetPixelReward(Next));
            Response.AppendBoolean(false);
            Response.AppendInt32(BrickEngine.GetAchievementReactor().GetGotAmount(Client, Id)); // GotAmount;
            Response.AppendBoolean(CompletedAchievement(Client));
            Response.AppendStringWithBreak(Category.Name.ToLower());
            Response.AppendInt32(MaxLevel);
        }

        public Response GetUnlockResponse(Client Client)
        {
            int Level = GetNextLevel(Client); 
            int OldLevel = (Level - 1);

            if (OldLevel <= 0)
            {
                OldLevel = 1;
            }

            string OldBadge = GetBadgeCode(OldLevel);
            string NewBadge = GetBadgeCode(Level);

            int OldBadgeId = BrickEngine.GetBadgeHandler().GetIdForBadge(OldBadge);
            int NewBadgeId = BrickEngine.GetBadgeHandler().GetIdForBadge(NewBadge);

            int ScoreReward = GetScoreReward(Level);
            int PixelReward = GetPixelReward(Level);

            Response Response = new Response(437);
            Response.AppendInt32(Id);
            Response.AppendInt32(Level);
            Response.AppendInt32(NewBadgeId);
            Response.AppendStringWithBreak(NewBadge);
            Response.AppendInt32(ScoreReward);
            Response.AppendInt32(PixelReward);
            Response.AppendBoolean(false);
            Response.AppendInt32(ExtraScore);
            Response.AppendInt32(OldBadgeId);
            Response.AppendStringWithBreak(OldBadge);
            Response.AppendStringWithBreak(Category.Name.ToLower());

            return Response;
        }

        public Response GetUpdateResponse(Client Client)
        {
            Response Response = new Response(913);
            GetResponse(Response, Client);

            return Response;
        }

        public int GetNextLevel(Client Client)
        {
            if (CompletedAchievement(Client))
            {
                return Client.GetUser().Achievements[Id];
            }

            if (Client.GetUser().Achievements.ContainsKey(Id))
            {
                return Client.GetUser().Achievements[Id] + 1;
            }

            return 1;
        }

        public Boolean CompletedAchievement(Client Client)
        {
            if (Client.GetUser().Achievements.ContainsKey(Id))
            {
                return (Client.GetUser().Achievements[Id] >= MaxLevel);
            }

            return false;
        }

        public int GetScoreReward(int Level)
        {
            return BrickEngine.GetConvertor().ObjectToInt32(ScoreRewardBase * Level * 1.1);
        }

        public int GetPixelReward(int Level)
        {
            return BrickEngine.GetConvertor().ObjectToInt32(PixelRewardBase * Level * 1.1);
        }

        public int GetNeeded(int Level)
        {
            if (Level > MaxLevel)
            {
                Level = MaxLevel;
            }

            foreach (AchievementGoal Goal in Goals)
            {
                if (Goal.Level.Equals(Level))
                {
                    return Goal.GoalAmount;
                }
            }

            return 1;
        }

        public string GetBadgeCode(int Level)
        {
            if (EnableLeveling)
            {
                return string.Format("{0}{1}", BadgeBase, Level);
            }

            return BadgeBase;
        }
    }
}
