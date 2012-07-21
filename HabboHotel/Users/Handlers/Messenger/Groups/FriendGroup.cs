using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger.Groups
{
    class FriendGroup
    {
        #region Fields
        public readonly int Id;
        public readonly int UserId;
        public string Name;
        #endregion

        #region Constructors
        public FriendGroup(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }

        public FriendGroup(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }
        #endregion
    }
}
