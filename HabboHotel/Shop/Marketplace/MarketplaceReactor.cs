using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Security;
using BrickEmulator.Storage;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Furni.Items;
using System.Text.RegularExpressions;

namespace BrickEmulator.HabboHotel.Shop.Marketplace
{
    class MarketplaceReactor
    {
        #region Fields
        private Dictionary<int, MarketOffer> Offers;
        private Dictionary<int, MarketDevelopment> Developments;

        private SecurityCounter OfferIdCounter;
        private SecurityCounter DevelopmentIdCounter;
        #endregion

        #region Constructors
        public MarketplaceReactor()
        {
            LoadOffers();
            LoadDevelopments();
        }
        #endregion

        #region Methods

        #region Caching
        public void LoadOffers()
        {
            Offers = new Dictionary<int, MarketOffer>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT MAX(id) FROM marketplace_offers LIMIT 1");
                OfferIdCounter = new SecurityCounter(Reactor.GetInt32());
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM marketplace_offers");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                MarketOffer Offer = new MarketOffer(Row);

                Offers.Add(Offer.Id, Offer);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Offers.Count + "] MarketplaceOffer(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadDevelopments()
        {
            Developments = new Dictionary<int, MarketDevelopment>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT MAX(id) FROM marketplace_development LIMIT 1");
                DevelopmentIdCounter = new SecurityCounter(Reactor.GetInt32());
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM marketplace_development");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                MarketDevelopment Development = new MarketDevelopment(Row);

                Developments.Add(Development.Id, Development);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Developments.Count + "] Development(s) cached.", IO.WriteType.Outgoing);
        }
        #endregion

        #region Offers

        public List<MarketOffer> OrderBy(List<MarketOffer> Offers, int OrderState)
        {
            if (OrderState.Equals(1))
            {
                return (from offer in Offers orderby offer.CreditsRequestTot descending select offer).ToList();
            }
            else if (OrderState.Equals(2))
            {
                return (from offer in Offers orderby offer.CreditsRequestTot ascending select offer).ToList();
            }
            else if (OrderState.Equals(3))
            {
                return (from offer in Offers orderby offer.TradedToday descending select offer).ToList();
            }
            else if (OrderState.Equals(4))
            {
                return (from offer in Offers orderby offer.TradedToday ascending select offer).ToList();
            }
            else if (OrderState.Equals(5))
            {
                return (from offer in Offers orderby offer.TotOfferCount descending select offer).ToList();
            }
            else if (OrderState.Equals(6))
            {
                return (from offer in Offers orderby offer.TotOfferCount ascending select offer).ToList();
            }

            return Offers;
        }

        public List<MarketOffer> GetRunningOffers(bool Limit, int Min, int Max, string Query, int OrderState)
        {
            var List = new List<MarketOffer>();

            foreach (MarketOffer Offer in Offers.Values.ToList())
            {
                if (Offer.Expired)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(Query))
                {
                    if (!Regex.IsMatch(Offer.BaseName, Query))
                    {
                        continue;
                    }
                }

                if (Min > 0 && Max > 0)
                {
                    if (Offer.CreditsRequestTot >= Min && Offer.CreditsRequestTot <= Max)
                    {
                        if (Offer.State.Equals(1))
                        {
                            if (Limit)
                            {
                                if (List.Count < 250)
                                {
                                    List.Add(Offer);
                                }
                            }
                            else
                            {
                                List.Add(Offer);
                            }
                        }

                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (Offer.State.Equals(1))
                {
                    if (Limit)
                    {
                        if (List.Count < 250)
                        {
                            List.Add(Offer);
                        }
                    }
                    else
                    {
                        List.Add(Offer);
                    }
                }
            }

            return OrderBy(List.ToList(), OrderState);
        }

        public List<MarketOffer> GetOffersForHabboId(int HabboId)
        {
            var List = new List<MarketOffer>();

            foreach (MarketOffer Offer in Offers.Values.ToList())
            {
                if (Offer.UserId == HabboId)
                {
                    List.Add(Offer);
                }
            }

            return List;
        }

        public int GetSaledCredits(int HabboId)
        {
            int Credits = 0;

            foreach (MarketOffer Offer in GetOffersForHabboId(HabboId))
            {
                if (Offer.State.Equals(2))
                {
                    Credits += Offer.CreditsRequest;
                }
            }

            return Credits;
        }

        public List<MarketOffer> GetOffersForSpriteId(int SpriteId)
        {
            var List = new List<MarketOffer>();

            foreach (MarketOffer Offer in Offers.Values.ToList())
            {
                if (Offer.State.Equals(1))
                {
                    if (Offer.GetBaseItem().SpriteId == SpriteId)
                    {
                        List.Add(Offer);
                    }
                }
            }

            return List;
        }

        public void PurchaseOffer(int OfferId, Client Client)
        {
            MarketOffer Offer = GetOffer(OfferId);

            if (Offer == null)
            {
                return;
            }

            if (Client.GetUser().Credits < Offer.CreditsRequestTot)
            {
                Response Response = new Response(68);
                Response.AppendBoolean(true);
                Response.AppendBoolean(false);
                Client.SendResponse(Response);
                return;
            }

            Offer.State = 2;

            Client.GetUser().Credits -= Offer.CreditsRequestTot;
            Client.GetUser().UpdateCredits(true);

            Response ItemUpdate = new Messages.Response(832);
            ItemUpdate.AppendInt32(1);
            ItemUpdate.AppendInt32(Offer.GetBaseItem().InternalType.ToLower().Equals("s") ? 1 : 2);
            ItemUpdate.AppendInt32(1);
            ItemUpdate.AppendInt32(BrickEngine.GetItemReactor().InsertItem(Client.GetUser().HabboId, Offer.BaseId, string.Empty, -1));
            Client.SendResponse(ItemUpdate);

            Client.SendResponse(new Response(101));
            
            Client.SendResponse(Offer.GetPurchaseResponse());

            Client.SendResponse(GetResponse(-1, -1, string.Empty, 1));

            // Update @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE marketplace_offers SET state = '2' WHERE id = @offerid LIMIT 1");
                Reactor.AddParam("offerid", Offer.Id);
                Reactor.ExcuteQuery();
            }

            if (Offer.CreditsRequestTot > 0)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("INSERT INTO user_transactions (user_id, datetime, activity, description) VALUES (@user_id, @datetime, @activity, @description)");
                    Reactor.AddParam("user_id", Client.GetUser().HabboId);
                    Reactor.AddParam("datetime", DateTime.Now);
                    Reactor.AddParam("activity", -Offer.CreditsRequestTot);
                    Reactor.AddParam("description", "Marketplace Purchase");
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void InsertOffer(Client Client, Item Item, int GeneralPrice)
        {
            Response Response = new Response(610);

            // Verifying.
            if (GeneralPrice > 10000 || !Item.GetBaseItem().EnableAuction)
            {
                Response.AppendBoolean(false);
                Client.SendResponse(Response);
                return;
            }

            // OfferDetails
            int OfferId = OfferIdCounter.Next;

            // Developmentdetails
            int DevelopmentId = DevelopmentIdCounter.Next;

            // Both details
            int TotalPrice = (GeneralPrice + GetPriceCommission(GeneralPrice));
            DateTime DateTime = DateTime.Now;

            // Generating offer, and adding offer
            Dictionary<int, Object> OfferRow = new Dictionary<int, object>();

            OfferRow[0] = OfferId;
            OfferRow[1] = Client.GetUser().HabboId;
            OfferRow[2] = Item.BaseId;
            OfferRow[3] = 1;
            OfferRow[4] = Item.GetBaseItem().InternalName.ToLower();
            OfferRow[5] = GeneralPrice;
            OfferRow[6] = TotalPrice;
            OfferRow[7] = DateTime;

            // Generating from leak-row
            MarketOffer Offer = new MarketOffer(OfferRow);

            Offers.Add(Offer.Id, Offer);

            // Generating development, and adding development
            Dictionary<int, Object> DevelopmentRow = new Dictionary<int, object>();

            DevelopmentRow[0] = DevelopmentId;
            DevelopmentRow[1] = Item.BaseId;
            DevelopmentRow[2] = DateTime;
            DevelopmentRow[3] = TotalPrice;

            // Generating from leak-row
            MarketDevelopment Development = new MarketDevelopment(DevelopmentRow);

            Developments.Add(Development.Id, Development);

            // Sending positive response
            Response.AppendBoolean(true);
            Client.SendResponse(Response);

            // Delete furniture @ cache + response update
            Client.SendResponse(BrickEngine.GetItemReactor().RemoveItem(Item.Id));

            // Delete furniture @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM items WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("itemid", Item.Id);
                Reactor.ExcuteQuery();
            }

            // Insert Offer @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO marketplace_offers (user_id, base_id, base_name, credits_request, credits_request_tot, datetime) VALUES (@habboid, @baseid, @basename, @creditsreq, @creditsreqtot, @datetime)");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.AddParam("baseid", Item.BaseId);
                Reactor.AddParam("basename", Item.GetBaseItem().InternalName.ToLower());
                Reactor.AddParam("creditsreq", GeneralPrice);
                Reactor.AddParam("creditsreqtot", TotalPrice);
                Reactor.AddParam("datetime", DateTime);
                Reactor.ExcuteQuery();
            }

            // Insert Development @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO marketplace_development (base_id, datetime, credits_request) VALUES (@baseid, @datetime, @creditsreq)");
                Reactor.AddParam("baseid", Item.BaseId);
                Reactor.AddParam("datetime", DateTime);
                Reactor.AddParam("creditsreq", TotalPrice);
                Reactor.ExcuteQuery();
            }

            // Update tickets @ cache
            Client.GetUser().MarketplaceTickets--;

            // Update tickets @ database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET marketplace_tickets = marketplace_tickets - 1 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        public void TakeBackItem(Client Client, int OfferId)
        {
            MarketOffer Offer = GetOffer(OfferId);

            if (Offer == null)
            {
                return;
            }

            if (Offer.UserId != Client.GetUser().HabboId)
            {
                return;
            }

            // Update @ list
            Response Response = new Response(614);
            Response.AppendInt32(Offer.Id);
            Response.AppendBoolean(true);
            Client.SendResponse(Response);

            Response ItemUpdate = new Messages.Response(832);
            ItemUpdate.AppendInt32(1);
            ItemUpdate.AppendInt32(Offer.GetBaseItem().InternalType.ToLower().Equals("s") ? 1 : 2);
            ItemUpdate.AppendInt32(1);
            ItemUpdate.AppendInt32(BrickEngine.GetItemReactor().InsertItem(Client.GetUser().HabboId, Offer.BaseId, string.Empty, -1));
            Client.SendResponse(ItemUpdate);

            Client.SendResponse(new Response(101));

            Offers.Remove(Offer.Id);
        
            // DELETE @ SQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM marketplace_offers WHERE id = @offerid LIMIT 1");
                Reactor.AddParam("offerid", Offer.Id);
                Reactor.ExcuteQuery();
            }
        }

        private void CleanSaledItems(int HabboId)
        {
            foreach (MarketOffer Offer in GetOffersForHabboId(HabboId))
            {
                if (Offer.State.Equals(2))
                {
                    Offers.Remove(Offer.Id);
                }
            }

            // DELETE @ SQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM marketplace_offers WHERE user_id = @habboid AND state = '2' LIMIT 1");
                Reactor.AddParam("habboid", HabboId);
                Reactor.ExcuteQuery();
            }
        }

        public MarketOffer GetOffer(int Id)
        {
            try { return Offers[Id]; }
            catch { return null; }
        }

        #endregion

        #region Credits

        public void HandleTakeBackCredits(Client Client)
        {
            int Credits = GetSaledCredits(Client.GetUser().HabboId);

            Client.GetUser().Credits += Credits;
            Client.GetUser().UpdateCredits(true);

            CleanSaledItems(Client.GetUser().HabboId);

            if (Credits > 0)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("INSERT INTO user_transactions (user_id, datetime, activity, description) VALUES (@user_id, @datetime, @activity, @description)");
                    Reactor.AddParam("user_id", Client.GetUser().HabboId);
                    Reactor.AddParam("datetime", DateTime.Now);
                    Reactor.AddParam("activity", Credits);
                    Reactor.AddParam("description", "Marketplace item Sold");
                    Reactor.ExcuteQuery();
                }
            }
        }

        public int GetCreditsRequestAverageForSpriteId(int SpriteId)
        {
            return GetCreditsRequestAverageForDevelopment(GetDevelopmentsBySpriteId(SpriteId));
        }

        public int GetCreditsRequestAverageForDevelopment(List<MarketDevelopment> List)
        {
            int Tot = new int();

            foreach (MarketDevelopment Offer in List)
            {
                Tot += Offer.CreditsRequest;
            }

            return (Tot / List.Count);
        }

        public int GetLowestPriceForSprite(int SpriteId)
        {
            if (GetDevelopmentsBySpriteId(SpriteId).Count <= 0)
            {
                return 0;
            }

            return (from development in GetDevelopmentsBySpriteId(SpriteId) orderby development.CreditsRequest ascending select development).ToList()[0].CreditsRequest;
        }

        public int GetAveragePerItem(int SpriteId)
        {
            if (GetDevelopmentForSpriteId(SpriteId).Count <= 0)
            {
                return 0;
            }

            return GetDevelopmentForSpriteId(SpriteId)[0].CreditsRequest;
        }

        public int GetPriceCommission(int BasePrice)
        {
            int Result = BrickEngine.GetConvertor().ObjectToInt32(Math.Ceiling((float)(BasePrice / 100)));

            if (Result <= 0)
            {
                return 1;
            }

            return Result;
        }
        #endregion

        #region Development

        public List<MarketDevelopment> GetDevelopmentsBySpriteId(int SpriteId)
        {
            var List = new List<MarketDevelopment>();

            foreach (MarketDevelopment Development in Developments.Values.ToList())
            {
                if (Development.GetBaseItem().SpriteId == SpriteId)
                {
                    List.Add(Development);
                }
            }

            return List;
        }

        public List<MarketDevelopment> GetDevelopmentForSpriteId(int SpriteId)
        {
            var List = new List<MarketDevelopment>();

            foreach (MarketDevelopment Development in Developments.Values.ToList())
            {
                if (Development.GetBaseItem().SpriteId == SpriteId)
                {
                    List.Add(Development);
                }
            }

            return (from development in List orderby development.DateTime ascending select development).ToList();
        }

        public List<MarketDevelopment> GetDevelopmentDay(int Day, int SpriteId)
        {
            var List = new List<MarketDevelopment>();

            foreach (MarketDevelopment Development in Developments.Values.ToList())
            {
                if (BrickEngine.GetConvertor().ObjectToInt32((DateTime.Now - Development.DateTime).TotalDays) + Day == 0)
                {
                    if (Development.GetBaseItem().SpriteId == SpriteId)
                    {
                        List.Add(Development);
                    }
                }
            }

            return (from development in List orderby development.DateTime ascending select development).ToList();
        }

        #endregion

        #region Responses

        public void GetChartResponse(Response Response, int SpriteId)
        {
            Response.AppendInt32(30);
            Response.AppendInt32(29);

            for (int i = -29; i < 0; i++)
            {
                Response.AppendInt32(i);

                if (GetDevelopmentDay(i, SpriteId).Count > 0)
                {
                    Response.AppendInt32(GetCreditsRequestAverageForDevelopment(GetDevelopmentDay(i, SpriteId)));
                    Response.AppendInt32(GetDevelopmentDay(i, SpriteId).Count);
                }
                else
                {
                    Response.AppendInt32(GetAveragePerItem(SpriteId));
                    Response.AppendInt32(GetOffersForSpriteId(SpriteId).Count);
                }
            }

            Response.AppendInt32(1);
            Response.AppendInt32(SpriteId);
        }


        public Response GetItemInfo(int SpriteId)
        {
            Response Response = new Response(617);
            Response.AppendInt32(GetLowestPriceForSprite(SpriteId));
            Response.AppendInt32(GetOffersForSpriteId(SpriteId).Count);
            GetChartResponse(Response, SpriteId);
            return Response;
        }

        public Response GetResponse(int Min, int Max, string Query, int OrderState)
        {
            Response Response = new Response(615);
            Response.AppendInt32(GetRunningOffers(true, Min, Max, Query, OrderState).Count);

            foreach (MarketOffer Offer in GetRunningOffers(true, Min, Max, Query, OrderState))
            {
                Offer.GetResponse(Response);
            }

            Response.AppendInt32(GetRunningOffers(false, Min, Max, Query, OrderState).Count);

            return Response;
        }

        public Response GetOwnResponse(int HabboId)
        {
            Response Response = new Response(616);
            Response.AppendInt32(GetSaledCredits(HabboId));
            Response.AppendInt32(GetOffersForHabboId(HabboId).Count);

            foreach (MarketOffer Offer in GetOffersForHabboId(HabboId))
            {
                Offer.GetOwnResponse(Response);
            }

            return Response;
        }

        #endregion

        #endregion
    }
}
