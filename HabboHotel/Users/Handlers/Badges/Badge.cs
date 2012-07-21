using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Users.Handlers.Badges
{
    class Badge
    {
        public readonly int OwnerId;
        public string BadgeCode;
        public int SlotId;

        public Badge(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(0);

            OwnerId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BadgeCode = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            SlotId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public Badge(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(0);

            OwnerId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BadgeCode = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            SlotId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
    }
}
