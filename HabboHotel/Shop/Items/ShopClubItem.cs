using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Shop.Items
{
    class ShopClubItem
    {
        public readonly int Id;
        public readonly short MonthsAmount;
        public readonly int ShopItemId;
        public readonly string MemberShipType;

        public ShopClubItem(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MonthsAmount = BrickEngine.GetConvertor().ObjectToShort(Row[Counter.Next]);
            ShopItemId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            MemberShipType = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }

        public DateTime ExpireDate()
        {
            return DateTime.Now.AddMonths(MonthsAmount);
        }

        public void GetResponse(Response Response)
        {
            StringBuilder PacketBuilder = new StringBuilder();
            Response.AppendInt32(ShopItemId);

            PacketBuilder.Append("HABBO_CLUB_");
            PacketBuilder.Append(MemberShipType.ToUpper());
            PacketBuilder.Append('_');
            PacketBuilder.Append(MonthsAmount);
            PacketBuilder.Append("_MONTH");

            if (MonthsAmount > 1)
            {
                PacketBuilder.Append('S');
            }

            Response.AppendStringWithBreak(PacketBuilder.ToString());
            Response.AppendInt32(BrickEngine.GetShopReactor().GetShopItem(ShopItemId).CreditsCost);
            Response.AppendBoolean(false);
            Response.AppendBoolean(MemberShipType == "VIP");
            Response.AppendInt32(MonthsAmount);
            Response.AppendInt32(MonthsAmount * 31);
            Response.AppendInt32(ExpireDate().Year);
            Response.AppendInt32(ExpireDate().Month);
            Response.AppendInt32(ExpireDate().Day);
        }
    }
}
