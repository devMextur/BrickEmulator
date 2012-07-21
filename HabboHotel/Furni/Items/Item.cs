using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.Messages;
using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Furni.Triggers;

namespace BrickEmulator.HabboHotel.Furni.Items
{
    enum ItemPlace
    {
        Inventory,
        Room
    }

    class Item
    {
        #region Fields
        public readonly int Id;
        public int BaseId;
        public int OwnerId;
        public int RoomId;
        public iPoint Point;
        public int Rotation;
        public string WallPoint;
        public string ExtraData;
        public int InsideItemId;
        #endregion

        #region Properties
        public ItemPlace Place
        {
            get
            {
                if (RoomId > 0)
                {
                    return ItemPlace.Room;
                }

                return ItemPlace.Inventory;
            }
        }

        public iPoint FontPoint
        {
            get
            {
                iPoint Point = new iPoint(this.Point.X, this.Point.Y);

                if (Rotation == 0)
                {
                    Point.Y--;
                }
                else if (Rotation == 2)
                {
                    Point.X++;
                }
                else if (Rotation == 4)
                {
                    Point.Y++;
                }
                else if (Rotation == 6)
                {
                    Point.X--;
                }

                return Point;
            }
        }

        public IFurniTrigger GetTrigger()
        {
            if (GetBaseItem().ExternalType.ToLower().Equals("teleport"))
            {
                return new TeleportTrigger();
            }
            else if (GetBaseItem().ExternalType.ToLower().Equals("water"))
            {
                return new WaterTrigger();
            }

            return new DefaultTrigger();
        }
        #endregion

        #region Constructors
        public Item(DataRow Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            OwnerId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            RoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            int X = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            int Y = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            double Z = BrickEngine.GetConvertor().ObjectToDouble(Row[Counter.Next]);

            this.Point = new iPoint(X, Y, Z);

            Rotation = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            WallPoint = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            ExtraData = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            InsideItemId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public Item(Dictionary<int, Object> Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            OwnerId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            RoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            int X = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            int Y = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            double Z = BrickEngine.GetConvertor().ObjectToDouble(Row[Counter.Next]);

            this.Point = new iPoint(X, Y, Z);

            Rotation = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            WallPoint = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            ExtraData = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            InsideItemId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        #endregion

        #region Methods

        public void GetInventoryResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(GetBaseItem().InternalType.ToUpper());
            Response.AppendInt32(Id);
            Response.AppendInt32(GetBaseItem().SpriteId);

            if (GetBaseItem().InternalName.ToLower().Contains("a2") || GetBaseItem().InternalName.ToLower().Contains("floor"))
            {
                Response.AppendInt32(3);
            }
            else if (GetBaseItem().InternalName.ToLower().Contains("wallpaper"))
            {
                Response.AppendInt32(2);
            }
            else if (GetBaseItem().InternalName.ToLower().Contains("landscape"))
            {
                Response.AppendInt32(4);
            }
            else if (!GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.AppendInt32(0);
            }
            else
            {
                Response.AppendInt32(1);
            }

            Response.AppendStringWithBreak(ExtraData);
            Response.AppendBoolean(GetBaseItem().EnableRecycle);
            Response.AppendBoolean(GetBaseItem().EnableTrade);
            Response.AppendBoolean(GetBaseItem().EnableInterventoryStack);
            Response.AppendBoolean(GetBaseItem().EnableAuction);
            Response.AppendInt32(-1);

            if (GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.AppendChar(2);
                Response.AppendInt32(0);
            }
        }

        public void GetRoomResponse(Response Response)
        {
            if (GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.AppendInt32(Id);
            }
            else
            {
                Response.AppendRawInt32(Id);
                Response.AppendChar(2);
            }

            Response.AppendInt32(GetBaseItem().SpriteId);

            if (GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.AppendInt32(Point.X);
                Response.AppendInt32(Point.Y);
                Response.AppendInt32(Rotation);
                Response.AppendStringWithBreak(Point.Z.ToString().Replace(',', '.'));
                Response.AppendBoolean(false);
            }
            else
            {
                Response.AppendStringWithBreak(WallPoint);
            }

            Response.AppendStringWithBreak(ExtraData); // if Sticky (split(' '));

            if (GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.AppendInt32(-1);
            }

            Response.AppendBoolean(false); // Enable Button (Use);
        }

        public Response GetTriggerResponse()
        {
            Response Response = new Response();

            if (GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.Initialize(88);
                Response.AppendRawInt32(Id);
                Response.AppendChar(2);
                Response.AppendStringWithBreak(ExtraData);
            }
            else
            {
                Response.Initialize(85);
                GetRoomResponse(Response);
            }

            return Response;
        }

        public BaseItem GetBaseItem()
        {
            return BrickEngine.GetFurniReactor().GetItem(BaseId);
        }

        #endregion
    }
}
