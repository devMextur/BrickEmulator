using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;
using System.Data;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Shop.Items
{
    class ShopPage
    {
        public readonly int Id;
        public readonly int MainId;
        public readonly int OrderId;
        public readonly string Name = string.Empty;
        public readonly string Base = "default_3x3";
        public readonly uint NeedRank = new uint();
        public readonly uint IconId = 1;
        public readonly uint IconPaint = 1;
        public readonly Boolean EnableClick = true;
        public readonly string HeadImage;
        public readonly string HeadDescription;
        public readonly string TeaserImage;
        public readonly string TeaserDescription;
        public readonly string ExtraText1;
        public readonly string ExtraText2;

        public ShopPage(DataRow Row)
        {
            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);
            MainId = BrickEngine.GetConvertor().ObjectToInt32(Row[1]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[2]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[3]);
            Base = BrickEngine.GetConvertor().ObjectToString(Row[4]);
            NeedRank = BrickEngine.GetConvertor().ObjectToUInt32(Row[5]);
            IconId = BrickEngine.GetConvertor().ObjectToUInt32(Row[6]);
            IconPaint = BrickEngine.GetConvertor().ObjectToUInt32(Row[7]);
            EnableClick = BrickEngine.GetConvertor().ObjectToBoolean(Row[8]);
            HeadImage = BrickEngine.GetConvertor().ObjectToString(Row[9]);
            HeadDescription = BrickEngine.GetConvertor().ObjectToString(Row[10]);
            TeaserImage = BrickEngine.GetConvertor().ObjectToString(Row[11]);
            TeaserDescription = BrickEngine.GetConvertor().ObjectToString(Row[12]);
            ExtraText1 = BrickEngine.GetConvertor().ObjectToString(Row[13]).Replace("{{10}}", Convert.ToChar(10).ToString());
            ExtraText2 = BrickEngine.GetConvertor().ObjectToString(Row[14]).Replace("{{10}}", Convert.ToChar(10).ToString());
        }

        public void GetButtonData(Response Response)
        {
            Response.AppendBoolean(true); // always true
            Response.AppendUInt32(IconPaint);
            Response.AppendUInt32(IconId);
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(Name);
            Response.AppendInt32(BrickEngine.GetShopReactor().GetButtonTreeAmount(Id));
        }

        public Response GetResponse()
        {
            Response Response = new Response(127);
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(Base);

            BrickEngine.GetShopReactor().SerializeShopData(Response, this);

            var Items = BrickEngine.GetShopReactor().GetItemsForPage(Id);

            Response.AppendInt32(Items.Count);

            foreach (ShopItem Item in Items)
            {
                Item.GetResponse(Response, false);
            }

            Response.AppendInt32(-1);

            return Response;
        }
    }
}
