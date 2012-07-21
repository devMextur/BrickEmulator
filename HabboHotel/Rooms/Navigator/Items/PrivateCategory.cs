using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Rooms.Navigator.Items
{
    class PrivateCategory
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int RankAllowed;
        public readonly Boolean EnableTrading;

        public PrivateCategory(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            RankAllowed = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            EnableTrading = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
        }
    }
}
