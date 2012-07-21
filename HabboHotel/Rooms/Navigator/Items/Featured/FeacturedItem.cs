using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Rooms.Navigator.Items.Featured
{
    enum FeacturedType
    {
        TagLink = 1,
        Category = 2,
        FeacturedRoom = 3,
    }

    class FeacturedItem
    {
        public readonly int Id;
        public readonly FeacturedType Type;
        public readonly int CategoryId;
        public readonly string Name;
        public readonly string Description;
        public readonly string SearchParams;
        public readonly int RoomId;
        public readonly string ImageLink;
        public readonly Boolean ImageButton;
        public readonly int OrderId;

        public Boolean ValidRoom
        {
            get
            {
                return RoomId > -1 && GetRoom() != null;
            }
        }

        public FeacturedItem(DataRow Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            Type = (FeacturedType)BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Description = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            SearchParams = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            RoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            ImageLink = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            ImageButton = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public FeacturedItem(IDictionary<int, Object> Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            Type = (FeacturedType)BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Description = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            SearchParams = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            RoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            ImageLink = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            ImageButton = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public void GetResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(ValidRoom ? GetRoom().Name : Name);
            Response.AppendStringWithBreak(ValidRoom ? GetRoom().Description : Description);
            Response.AppendBoolean(ImageButton);
            Response.AppendStringWithBreak(ValidRoom ? GetRoom().Name : Name);
            Response.AppendStringWithBreak(ImageLink);
            Response.AppendInt32(CategoryId);
            Response.AppendInt32(ValidRoom ? GetRoom().RoomUserAmount : 0);

            if (Type.Equals(FeacturedType.TagLink))
            {
                Response.AppendInt32(1);
                Response.AppendStringWithBreak(SearchParams);
            }
            else if (Type.Equals(FeacturedType.Category))
            {
                Response.AppendInt32(4);
                Response.AppendBoolean(false);
            }
            else
            {
                Response.AppendInt32(3);
                Response.AppendStringWithBreak(ImageLink);
                Response.AppendBoolean(false);
                Response.AppendBoolean(false);
                Response.AppendChar(2);
                Response.AppendInt32(ValidRoom ? GetRoom().LimitUsers : 50);
                Response.AppendInt32(RoomId);
            }
        }

        public VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Dead);
        }
    }
}
