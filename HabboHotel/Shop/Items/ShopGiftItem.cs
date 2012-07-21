using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Shop.Items
{
    enum MemberType
    {
        BASIC,
        VIP
    }

    class ShopGiftItem
    {
        public readonly int Id;
        public readonly int BaseId;
        public readonly MemberType MemberType;
        public readonly int MemberdaysNeed;

        public ShopGiftItem(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MemberType = (BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]) == "BASIC") ? MemberType.BASIC : MemberType.VIP;
            MemberdaysNeed = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public void GetExternalResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(GetBaseItem().InternalName);
            Response.AppendBoolean(true);
            Response.AppendBoolean(false);
            Response.AppendBoolean(false);
            Response.AppendBoolean(true);
            Response.AppendStringWithBreak(GetBaseItem().InternalType.ToLower());
            Response.AppendInt32(GetBaseItem().SpriteId);
            Response.AppendChar(2);
            Response.AppendBoolean(true);
            Response.AppendInt32(-1);
            Response.AppendInt32((MemberType == Items.MemberType.VIP) ? 2 : 1);
        }

        public void GetInternalResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendBoolean(MemberType == Items.MemberType.VIP);
            Response.AppendInt32(MemberdaysNeed);
            Response.AppendBoolean(true);
        }

        public BaseItem GetBaseItem()
        {
            return BrickEngine.GetFurniReactor().GetItem(BaseId);
        }
    }
}
