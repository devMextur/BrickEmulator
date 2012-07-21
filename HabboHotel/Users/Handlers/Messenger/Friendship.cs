using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger
{
    class Friendship
    {
        public readonly int UserId;
        public readonly int FriendId;
        public Boolean Pending;

        public Friendship(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(0);

            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            FriendId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Pending = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
        }

        public Friendship(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(0);

            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            FriendId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Pending = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
        }
    }
}
