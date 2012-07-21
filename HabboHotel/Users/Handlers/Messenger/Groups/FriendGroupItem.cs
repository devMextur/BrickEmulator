using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger.Groups
{
    class FriendGroupItem
    {
        #region Fields
        public readonly int Id;
        public readonly int UserId;
        public readonly int FriendId;
        public readonly int CategoryId;
        #endregion

        #region Constructors
        public FriendGroupItem(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            FriendId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public FriendGroupItem(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            FriendId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
        #endregion
    }
}
