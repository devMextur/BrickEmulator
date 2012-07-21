using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.HabboHotel.Shop.Items;
using BrickEmulator.Messages;
using System.Data;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Shop.Ecotron;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void SerializeShopButtons(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetShopReactor().GetButtonData());
        }

        private void SerialzeShopPage(Client Client, Request Request)
        {
            int PageId = Request.PopWiredInt32();

            ShopPage Page = BrickEngine.GetShopReactor().SelectPage(PageId);

            if (Page == null || !Page.EnableClick)
            {
                return;
            }

            Client.SendResponse(Page.GetResponse());
            Client.SendResponse(BrickEngine.GetShopReactor().GetExtraResponse(Page, Client));
        }

        private void SerializePetRaces(Client Client, Request Request)
        {
            string PetType = Request.PopFixedString();

            int Type = Convert.ToInt32(PetType.Substring(6));

            if (Type < 0)
            {
                return;
            }

            Response Response = new Response(827);

            PetRace PetRace = BrickEngine.GetShopReactor().SelectPetRace(Type);

            if (PetRace == null)
            {
                System.Data.DataRow Row = new DataTable().Rows[0];

                Row[0] = 1;
                Row[1] = Type;
                Row[2] = 0;
                Row[3] = 0;

                PetRace = new PetRace(Row);
            }

            Response.AppendStringWithBreak("a0 pet" + Type);
            Response.AppendInt32(PetRace.RaceAmount);

            for (int i = PetRace.StartIndexer; i < PetRace.RaceAmount; i++)
            {
                Response.AppendInt32(Type);
                Response.AppendInt32(i);
                Response.AppendBoolean(true);
                Response.AppendBoolean(false);
            }

            Client.SendResponse(Response);
        }

        private void GetPetName(Client Client, Request Request)
        {
            string PetName = BrickEngine.CleanString(Request.PopFixedString());

            Response Response = new Response(36);
            Response.AppendInt32(BrickEngine.GetPetReactor().NameCheckResult(PetName));
            Client.SendResponse(Response);
        }

        private void CanSenditemAsGift(Client Client, Request Request)
        {

        }

        private void PurchaseShopItem(Client Client, Request Request)
        {
            int PageId = Request.PopWiredInt32();
            int ItemId = Request.PopWiredInt32();

            string ExtraData = Request.PopFixedString();

            BrickEngine.GetShopReactor().PurchaseItem(ItemId, Client, ExtraData, -1);
        }

        #region Info
        private void GetShopInfoA(Client Client, Request Request) // 3011
        {
            Response Response = new Response(612);
            Response.AppendBoolean(true);
            Response.AppendBoolean(true);
            Response.AppendBoolean(true);
            Response.AppendInt32(5);
            Response.AppendBoolean(true);
            Response.AppendInt32(10000);
            Response.AppendInt32(48);
            Response.AppendInt32(7);
            Client.SendResponse(Response);
        }

        private void GetShopInfoB(Client Client, Request Request) // 473
        {
            Response Response = new Response(620);
            Response.AppendBoolean(true);
            Response.AppendBoolean(true);
            Response.AppendInt32(10);

            for (int i = 3064; i < 3074; i++)
            {
                Response.AppendInt32(i);
            }

            Response.AppendInt32(7);

            for (int i = 0; i < 7; i++)
            {
                Response.AppendInt32(i);
            }

            Response.AppendInt32(11);

            for (int i = 0; i < 11; i++)
            {
                Response.AppendInt32(i);
            }

            Client.SendResponse(Response);
        }

        #endregion

        #region Marketplace

        private void GetMarketPlaceOffers(Client Client, Request Request)
        {
            int MinimalPrice = Request.PopWiredInt32();
            int MaximalPrice = Request.PopWiredInt32();
            string Query = Request.PopFixedString();
            int OrderState = Request.PopWiredInt32();

            Client.SendResponse(BrickEngine.GetMarketplaceReactor().GetResponse(MinimalPrice, MaximalPrice, Query, OrderState));
        }

        private void GetMarketPlaceTickets(Client Client, Request Request)
        {
            Response Response = new Response(611);
            Response.AppendInt32((Client.GetUser().MarketplaceTickets > 0) ? 1 : 4);
            Response.AppendInt32(Client.GetUser().MarketplaceTickets);
            Client.SendResponse(Response);
        }

        private void GetMarketPlaceOwnOffers(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetMarketplaceReactor().GetOwnResponse(Client.GetUser().HabboId));
        }

        private void GetMarketPlaceItemInfo(Client Client, Request Request)
        {
            Request.PopWiredInt32();

            int SpriteId = Request.PopWiredInt32();

            if (SpriteId <= 0)
            {
                return;
            }

            Client.SendResponse(BrickEngine.GetMarketplaceReactor().GetItemInfo(SpriteId));
        }

        private void PostItemOnMarketplace(Client Client, Request Request)
        {
            if (Client.GetUser().MarketplaceTickets <= 0)
            {
                return;
            }

            int GeneralPrice = Request.PopWiredInt32();

            if (GeneralPrice <= 0)
            {
                return;
            }

            // Skip JUNK
            Request.PopWiredInt32();

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            Item Item = BrickEngine.GetItemReactor().GetItem(ItemId);

            if (Item == null)
            {
                return;
            }

            BrickEngine.GetMarketplaceReactor().InsertOffer(Client, Item, GeneralPrice);
        }

        private void TakeBackItem(Client Client, Request Request)
        {
            int OfferId = Request.PopWiredInt32();

            if (OfferId <= 0)
            {
                return;
            }

            BrickEngine.GetMarketplaceReactor().TakeBackItem(Client, OfferId);
        }

        private void PurchaseTickets(Client Client, Request Request) // 3013
        {
            // Update credits
            Client.GetUser().Credits -= 1;
            Client.GetUser().UpdateCredits(true);

            // Update tickets @ cache
            Client.GetUser().MarketplaceTickets += 4;

            // Update tickets @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET marketplace_tickets = marketplace_tickets + 4 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }

            Response Response = new Response(611);
            Response.AppendInt32((Client.GetUser().MarketplaceTickets > 0) ? 1 : 4);
            Response.AppendInt32(Client.GetUser().MarketplaceTickets);
            Client.SendResponse(Response);
        }

        private void GetSaledCredits(Client Client, Request Request)
        {
            BrickEngine.GetMarketplaceReactor().HandleTakeBackCredits(Client);
        }

        private void PurchaseOffer(Client Client, Request Request)
        {
            int OfferId = Request.PopWiredInt32();

            if (OfferId <= 0)
            {
                return;
            }

            BrickEngine.GetMarketplaceReactor().PurchaseOffer(OfferId, Client);
        }

        #endregion

        #region Ecotron
        private void GetEcotronInfo(Client Client, Request Request)
        {
            Response Response = new Response(507);
            Response.AppendInt32(BrickEngine.GetEcotronReactor().GetTimerTime(Client.GetUser().HabboId));
            Response.AppendInt32(BrickEngine.GetEcotronReactor().GetTimeToWait(Client.GetUser().HabboId));
            Client.SendResponse(Response);
        }

        private void GetEcotronRewards(Client Client, Request Request)
        {
            Response Response = new Response(506);
            Response.AppendInt32(5);

            for (int i = 5; i > 0; i--)
            {
                var Items = BrickEngine.GetEcotronReactor().GetRewardsForLevel(i);

                Response.AppendInt32(i);
                Response.AppendInt32(BrickEngine.GetEcotronReactor().GetChangeByLevel(i));
                Response.AppendInt32(Items.Count);

                foreach (EcotronReward Item in Items)
                {
                    Response.AppendStringWithBreak(Item.GetBaseItem().InternalType.ToLower());
                    Response.AppendInt32(Item.GetBaseItem().SpriteId);
                }
            }

            Client.SendResponse(Response);
        }

        private void GetEcotronPackage(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int ItemAmount = Request.PopWiredInt32();

            if (ItemAmount < 5 || ItemAmount > 5)
            {
                return;
            }

            var ItemList = new List<int>();

            for (int i = 0; i < ItemAmount; i++)
            {
                int ItemId = Request.PopWiredInt32();

                ItemList.Add(ItemId);
            }

            if (BrickEngine.GetEcotronReactor().GainReward(Client))
            {
                foreach (int ItemId in ItemList)
                {
                    BrickEngine.GetItemReactor().RemoveItem(ItemId);
                }

                Client.SendResponse(new Response(101));

                foreach (int ItemId in ItemList)
                {
                    using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                    {
                        Reactor.SetQuery("DELETE FROM items WHERE id = @itemid LIMIT 1");
                        Reactor.AddParam("itemid", ItemId);
                        Reactor.ExcuteQuery();
                    }
                }
            }
        }

        private void RedeemPresent(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            Item Item = BrickEngine.GetItemReactor().GetItem(ItemId);

            if (!Item.OwnerId.Equals(Client.GetUser().HabboId))
            {
                return;
            }

            Client.GetUser().GetRoom().GetRoomEngine().HandleIncomingItemPickUp(Item, Item.Point, new Rooms.Pathfinding.iPoint(-1, -1, 0.0), Item.Rotation, 0, Client.GetUser().GetRoomUser());

            BrickEngine.GetShopReactor().RedeemPresent(Client, Item);
        }
        #endregion
    }
}
