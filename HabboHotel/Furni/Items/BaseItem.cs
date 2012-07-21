using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Furni.Items
{
    class BaseItem
    {
        #region Fields
        public readonly int Id;
        public readonly string InternalName;
        public readonly string InternalType;
        public readonly int LengthX;
        public readonly int LengthY;
        public readonly double LengthZ;
        public readonly int SpriteId;
        public readonly Boolean EnableStack;
        public readonly Boolean EnableWalk;
        public readonly Boolean EnableSit;
        public readonly Boolean EnableTrade;
        public readonly Boolean EnableAuction;
        public readonly Boolean EnableRecycle;
        public readonly Boolean EnableGift;
        public readonly Boolean EnableInterventoryStack;
        public readonly string ExternalType;
        public readonly int InteractorAmount;
        #endregion

        #region Constructors
        public BaseItem(DataRow Row)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            InternalName = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            InternalType = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            LengthX = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            LengthY = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            LengthZ = BrickEngine.GetConvertor().ObjectToDouble(Row[Counter.Next]);
            SpriteId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            EnableStack = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableWalk = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableSit = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableTrade = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableAuction = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableRecycle = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableGift = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            EnableInterventoryStack = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            ExternalType = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            InteractorAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
        #endregion
    }
}
