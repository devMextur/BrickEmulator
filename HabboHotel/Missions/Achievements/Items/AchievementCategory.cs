using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Missions.Achievements.Items
{
    class AchievementCategory
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int OrderId;

        public AchievementCategory(DataRow Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
    }
}
