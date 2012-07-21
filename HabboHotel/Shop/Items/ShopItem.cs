using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Security;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Shop.Items
{
    class ShopItem
    {
        public readonly int Id;
        public readonly int PageId;
        public readonly string InternalName;
        public readonly List<int> InternalItemIds;
        public readonly int PurchaserScale;
        public readonly int Amount;
        public readonly int CreditsCost;
        public readonly int PixelsCost;
        public readonly string SpriteLayer;

        public Boolean IsDeal
        {
            get { return InternalItemIds.Count > 1; }
        }

        public ShopItem(DataRow Row)
        {
            Security.SecurityCounter Counter = new SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            PageId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            InternalName = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            string ItemIds = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            InternalItemIds = new List<int>();

            var Splitted = ItemIds.Split(',').ToList();

            if (Splitted.Count <= 0 && ItemIds.Length >= 1)
            {
                InternalItemIds[0] = BrickEngine.GetConvertor().ObjectToInt32(ItemIds);
            }

            Splitted.ToList().ForEach(delegate(string Obj)
            {
                if (Obj.Length >= 1)
                {
                    int i = BrickEngine.GetConvertor().ObjectToInt32(Obj);

                    InternalItemIds.Add(i);
                }
            });

            PurchaserScale = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Amount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CreditsCost = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            PixelsCost = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            SpriteLayer = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }

        private string GetGoodName()
        {
            return (IsDeal || GetBaseItem(InternalItemIds[0]).InternalName.StartsWith("floor")
                || GetBaseItem(InternalItemIds[0]).InternalName.StartsWith("wallpaper")
                || GetBaseItem(InternalItemIds[0]).InternalName.StartsWith("landscape") || GetBaseItem(InternalItemIds[0]).ExternalType.ToLower() == "membership") ? InternalName : GetBaseItem(InternalItemIds[0]).InternalName;
        }

        public void GetResponse(Response Response, Boolean SpecialItem)
        {
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(GetGoodName());

            Response.AppendInt32(CreditsCost);
            Response.AppendInt32(PixelsCost);
            Response.AppendInt32(0);

            if (!SpecialItem)
            {
                Response.AppendInt32(InternalItemIds.Count);

                foreach (int Item in InternalItemIds)
                {
                    Response.AppendStringWithBreak(GetBaseItem(Item).InternalType.ToLower());
                    Response.AppendInt32(GetBaseItem(Item).SpriteId);
                    Response.AppendStringWithBreak(SpriteLayer);

                    Response.AppendInt32(Amount);
                    Response.AppendInt32(-1);
                }

                Response.AppendInt32(PurchaserScale);
            }
            else
            {
                Response.AppendInt32(0);
                Response.AppendInt32(0);
            }
        }

        public Response GetPurchaseResponse(Boolean SpecialItem)
        {
            Response Response = new Response(67);
            GetResponse(Response, SpecialItem);
            Response.AppendChar(2);
            return Response;
        }

        public BaseItem GetBaseItem(int Id)
        {
            return BrickEngine.GetFurniReactor().GetItem(Id);
        }
    }
}
