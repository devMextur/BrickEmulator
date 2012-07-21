using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Missions.Achievements.Items
{
    class AchievementGoal
    {
        public readonly int Id;
        public readonly int AchievementId;
        public readonly int Level;
        public readonly int GoalAmount;

        public AchievementGoal(DataRow Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            AchievementId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Level = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            GoalAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
    }
}
