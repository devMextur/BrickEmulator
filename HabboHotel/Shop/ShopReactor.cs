using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using BrickEmulator.Security;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Shop.Items;
using System.Data;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Users.Handlers.Membership;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;

namespace BrickEmulator.HabboHotel.Shop
{
    class ShopReactor
    {
        private Dictionary<string, Intervoke> ShopData;
        private delegate void Intervoke(Response Response, ShopPage Page);

        private Dictionary<int, ShopPage> Pages;
        private Dictionary<int, ShopItem> Items;
        private Dictionary<int, ShopClubItem> ClubItems;
        private Dictionary<int, ShopGiftItem> GiftItems;
        private Dictionary<int, PetRace> PetRaces;

        private VoucherReactor VoucherReactor = new VoucherReactor();

        public ShopReactor() { }

        public void Prepare()
        {
            GenerateShopData();

            LoadPages();
            LoadItems();

            LoadShopClubItems();
            LoadShopGiftItems();

            LoadPetRaces();

            VoucherReactor.Prepare();
        }

        public void LoadPetRaces()
        {
            PetRaces = new Dictionary<int, PetRace>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM shop_pet_races");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    PetRace PetRace = new PetRace(Row);

                    PetRaces.Add(PetRace.Type, PetRace);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + PetRaces.Count + "] ShopPetRaces(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadShopGiftItems()
        {
            GiftItems = new Dictionary<int, ShopGiftItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM shop_club_gifts");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    ShopGiftItem GiftItem = new ShopGiftItem(Row);

                    GiftItems.Add(GiftItem.Id, GiftItem);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + GiftItems.Count + "] ShopGiftItems(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadShopClubItems()
        {
            ClubItems = new Dictionary<int, ShopClubItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM shop_club_items");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    ShopClubItem ClubItem = new ShopClubItem(Row);

                    ClubItems.Add(ClubItem.Id, ClubItem);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + ClubItems.Count + "] ShopClubItems(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadPages()
        {
            Pages = new Dictionary<int, ShopPage>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM shop_pages ORDER BY order_id ASC");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    ShopPage Page = new ShopPage(Row);

                    Pages.Add(Page.Id, Page);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Pages.Count + "] Shop(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadItems()
        {
            Items = new Dictionary<int, ShopItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM shop_items");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    ShopItem Item = new ShopItem(Row);

                    Items.Add(Item.Id, Item);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Items.Count + "] ShopItems(s) cached.", IO.WriteType.Outgoing);
        }

        public PetRace SelectPetRace(int Type)
        {
            try { return PetRaces[Type]; }
            catch { return null; }
        }

        public ShopPage SelectPage(int Id)
        {
            try { return Pages[Id]; }
            catch { return null; }
        }

        public short GetButtonTreeAmount(int Tree)
        {
            short i = new short();

            foreach (ShopPage Page in Pages.Values.ToList())
            {
                if (Page.MainId == Tree)
                {
                    i++;
                }
            }

            return i;
        }

        public List<ShopItem> GetItemsForPage(int Id)
        {
            var List = new List<ShopItem>();

            foreach (ShopItem Item in Items.Values.ToList())
            {
                if (Item.PageId == Id)
                {
                    List.Add(Item);
                }
            }

            return (from item in List orderby item.Id ascending select item).ToList();
        }

        public ShopItem GetShopItem(int Id)
        {
            try { return Items[Id]; }
            catch { return null; }
        }

        public List<ShopPage> PagesForTree(int Tree)
        {
            var List = new List<ShopPage>();

            foreach (ShopPage Page in Pages.Values.ToList())
            {
                if (Page.MainId == Tree)
                {
                    List.Add(Page);
                }
            }

            return List;
        }

        public Response GetButtonData()
        {
            Response Response = new Response(126);
            Response.AppendBoolean(true);
            Response.AppendBoolean(false);
            Response.AppendBoolean(false);
            Response.AppendInt32(-1);
            Response.AppendChar(2);
            Response.AppendInt32(GetButtonTreeAmount(-1));

            var SortedFirst = from pag in PagesForTree(-1)
                              orderby pag.OrderId ascending
                              select pag;

            foreach (ShopPage Page in SortedFirst)
            {
                Page.GetButtonData(Response);

                var SortedLast = from pag in PagesForTree(Page.Id)
                                 orderby pag.OrderId ascending
                                 select pag;

                foreach (ShopPage TreePage in SortedLast)
                {
                    TreePage.GetButtonData(Response);
                }
            }

            return Response;
        }

        public Response GetExtraResponse(ShopPage Page, Client Client)
        {
            if (Page.Base.ToLower() == "club_buy")
            {
                Response Response = new Response(625);
                Response.AppendInt32(ClubItems.Count);

                var Sorted = from item in ClubItems.Values.ToList()
                             orderby item.ShopItemId ascending
                             select item;

                foreach (ShopClubItem ClubItem in Sorted)
                {
                    ClubItem.GetResponse(Response);
                }

                return Response;
            }
            else if (Page.Base.ToLower() == "club_gifts")
            {
                Response Response = new Response(623);

                if (Client.GetUser().GetMembership() != null)
                {
                    Response.AppendInt32(1); // Item for choose in X days // -1 = no member
                }
                else
                {
                    Response.AppendInt32(-1);
                }

                Response.AppendInt32(0); // ItemsToChooseLeft
                Response.AppendInt32(GiftItems.Count);

                foreach (ShopGiftItem GiftItem in GiftItems.Values.ToList())
                {
                    GiftItem.GetExternalResponse(Response);
                }

                Response.AppendInt32(GiftItems.Count);

                foreach (ShopGiftItem GiftItem in GiftItems.Values.ToList())
                {
                    GiftItem.GetInternalResponse(Response);
                }

                return Response;
            }
            else
            {
                return null;
            }
        }

        private void GenerateShopData()
        {
            ShopData = new Dictionary<string, Intervoke>();

            ShopData.Add("frontpage3",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(3);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendStringWithBreak(Page.TeaserImage);
                        Response.AppendChar(2);
                        Response.AppendInt32(11);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                        Response.AppendStringWithBreak(Page.ExtraText1);
                        Response.AppendStringWithBreak(Page.TeaserDescription);
                        Response.AppendStringWithBreak("How to get Habbo Credits");
                        Response.AppendString("You can get Habbo Credits via Prepaid Cards, Home Phone, Credit Card, Mobile, completing offers and more!");
                        Response.AppendChar(10);
                        Response.AppendChar(10);
                        Response.AppendStringWithBreak("To redeem your Habbo Credits, enter your voucher code below.");
                        Response.AppendStringWithBreak("Redeem a voucher code here:");
                        Response.AppendChar(2);
                        Response.AppendStringWithBreak("#FEFEFE");
                        Response.AppendStringWithBreak("#FEFEFE");
                        Response.AppendStringWithBreak("Want all the options?  Click here!");
                        Response.AppendStringWithBreak("magic.credits");
                    })
                    );

            ShopData.Add("default_3x3",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(3);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendStringWithBreak(Page.TeaserImage);
                        Response.AppendStringWithBreak(Page.ExtraText1);
                        Response.AppendInt32(3);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                        Response.AppendStringWithBreak(Page.TeaserDescription);
                        Response.AppendStringWithBreak(Page.ExtraText2);
                    })
                    );

            ShopData.Add("soundmachine",
                new Intervoke(delegate(Response Response, ShopPage Page)
                {
                    Response.AppendInt32(2);
                    Response.AppendStringWithBreak(Page.HeadImage);
                    Response.AppendStringWithBreak(Page.TeaserImage);
                    Response.AppendInt32(2);
                    Response.AppendStringWithBreak(Page.HeadDescription);
                    Response.AppendStringWithBreak(Page.ExtraText1);
                })
                );

            ShopData.Add("plasto",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(3);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendStringWithBreak(Page.TeaserImage);
                        Response.AppendStringWithBreak(Page.ExtraText1);
                        Response.AppendInt32(3);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                        Response.AppendStringWithBreak(Page.TeaserDescription);
                        Response.AppendStringWithBreak(Page.ExtraText2);
                    })
                    );

            ShopData.Add("pixeleffects",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(2);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendStringWithBreak(Page.TeaserImage);
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                    })
                    );

            ShopData.Add("club_buy",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendInt32(0);
                    })
                    );

            ShopData.Add("club_gifts",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                    })
                    );

            ShopData.Add("pets",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(2);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendChar(2);
                        Response.AppendInt32(4);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                        Response.AppendChar(2);
                        Response.AppendStringWithBreak(Page.TeaserDescription);
                        Response.AppendStringWithBreak(Page.ExtraText1);
                    })
                    );

            ShopData.Add("pets2",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(2);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendStringWithBreak(Page.TeaserImage);
                        Response.AppendInt32(4);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                        Response.AppendStringWithBreak(Page.TeaserDescription);
                        Response.AppendStringWithBreak(Page.ExtraText1);
                        Response.AppendStringWithBreak(Page.ExtraText2);
                    })
                    );

            ShopData.Add("spaces_new",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                    })
                    );

            ShopData.Add("marketplace",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendInt32(0);
                    })
                    );

            ShopData.Add("marketplace_own_items",
                 new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadImage);
                    })
                    );

            ShopData.Add("recycler",
                new Intervoke(delegate(Response Response, ShopPage Page)
                    {
                        Response.AppendInt32(2);
                        Response.AppendStringWithBreak(Page.HeadImage);
                        Response.AppendStringWithBreak(Page.TeaserImage);
                        Response.AppendInt32(1);
                        Response.AppendStringWithBreak(Page.HeadDescription);
                    })
                    );

            ShopData.Add("recycler_prizes",
                new Intervoke(delegate(Response Response, ShopPage Page)
                {
                    Response.AppendInt32(1);
                    Response.AppendStringWithBreak(Page.HeadImage);
                    Response.AppendInt32(1);
                    Response.AppendStringWithBreak(Page.HeadDescription);
                })
                );

            ShopData.Add("recycler_info",
                new Intervoke(delegate(Response Response, ShopPage Page)
                {
                    Response.AppendInt32(2);
                    Response.AppendStringWithBreak(Page.HeadImage);
                    Response.AppendStringWithBreak(Page.TeaserImage);
                    Response.AppendInt32(3);
                    Response.AppendStringWithBreak(Page.HeadDescription);
                    Response.AppendStringWithBreak(Page.TeaserDescription);
                    Response.AppendStringWithBreak(Page.ExtraText1);
                })
                );

            ShopData.Add("trophies",
                new Intervoke(delegate(Response Response, ShopPage Page)
                {
                    Response.AppendInt32(1);
                    Response.AppendStringWithBreak(Page.HeadImage);
                    Response.AppendInt32(2);
                    Response.AppendStringWithBreak(Page.HeadDescription);
                    Response.AppendStringWithBreak(Page.ExtraText1);
                })
                );
        }

        public ShopClubItem GetClubItem(int Id)
        {
            foreach (ShopClubItem Item in ClubItems.Values.ToList())
            {
                if (Item.ShopItemId == Id)
                {
                    return Item;
                }
            }

            return null;
        }

        public void SerializeShopData(Response Response, ShopPage Page)
        {
            ShopData[Page.Base].Invoke(Response, Page);
        }

        public void PurchaseItem(int Id, Client Client, string ExtraData, int PageId)
        {
            ShopItem Item = null;

            if ((Item = GetShopItem(Id)) == null)
            {
                return;
            }

            Boolean CreditsToLow = false;
            Boolean PixelsToLow = false;

            if (Client.GetUser().Credits < Item.CreditsCost)
            {
                CreditsToLow = true;
            }

            if (Client.GetUser().Pixels < Item.PixelsCost)
            {
                PixelsToLow = true;
            }

            if (CreditsToLow || PixelsToLow)
            {
                Response Response = new Response(68);
                Response.AppendBoolean(CreditsToLow);
                Response.AppendBoolean(PixelsToLow);
                Client.SendResponse(Response);
                return;
            }

            if (Item.CreditsCost > 0)
            {
                Client.GetUser().Credits -= Item.CreditsCost;
                Client.GetUser().UpdateCredits(true);
            }

            if (Item.PixelsCost > 0)
            {
                Client.GetUser().Pixels -= Item.PixelsCost;
                Client.GetUser().UpdatePixels(true);
            }

            if (!Item.GetBaseItem(Item.InternalItemIds[0]).InternalType.ToLower().Equals("e"))
            {
                if (Item.GetBaseItem(Item.InternalItemIds[0]).ExternalType.ToLower().Equals("membership"))
                {
                    Client.SendResponse(Item.GetPurchaseResponse(true));

                    ShopClubItem ClubItem = GetClubItem(Id);

                    if (Item == null)
                    {
                        return;
                    }

                    int Type = (ClubItem.MemberShipType.ToUpper() == "VIP") ? 1 : 0;
                    int Months = ClubItem.MonthsAmount;

                    BrickEngine.GetMembershipHandler().DeliverMembership(Client.GetUser().HabboId, Type, Months);

                    BrickEngine.GetPacketHandler().GetMembershipParams(Client, null);

                    Response Rights = new Response(2);

                    if (Client.GetUser().GetMembership() != null)
                    {
                        Rights.AppendInt32(Type + 1);
                    }
                    else
                    {
                        Rights.AppendInt32(0);
                    }

                    Client.SendResponse(Rights);

                    BrickEngine.GetAchievementReactor().UpdateUsersAchievement(Client, 8);
                    BrickEngine.GetAchievementReactor().UpdateUsersAchievement(Client, 9);
                }
                else
                {
                    if (Item.IsDeal)
                    {
                        var FloorItems = new List<int>();
                        var WallItems = new List<int>();

                        Client.SendResponse(Item.GetPurchaseResponse(false));

                        Response ShowItems = new Response(832);
                        ShowItems.AppendInt32(Item.InternalItemIds.Count);

                        for (int i = 0; i < Item.Amount; i++)
                        {
                            foreach (int ItemId in Item.InternalItemIds)
                            {
                                if (Item.GetBaseItem(ItemId).InternalType.ToLower().Equals("s"))
                                {
                                    FloorItems.Add(BrickEngine.GetItemReactor().InsertItem(Client.GetUser().HabboId, ItemId, ExtraData, -1));
                                }
                                else
                                {
                                    WallItems.Add(BrickEngine.GetItemReactor().InsertItem(Client.GetUser().HabboId, ItemId, ExtraData, -1));
                                }
                            }
                        }

                        ShowItems.AppendInt32(1);
                        ShowItems.AppendInt32(FloorItems.Count);

                        foreach (int ItemId in FloorItems)
                        {
                            ShowItems.AppendInt32(ItemId);
                        }

                        ShowItems.AppendInt32(2);
                        ShowItems.AppendInt32(WallItems.Count);

                        foreach (int ItemId in WallItems)
                        {
                            ShowItems.AppendInt32(ItemId);
                        }

                        Client.SendResponse(ShowItems);
                    }
                    else
                    {
                        if (Item.GetBaseItem(Item.InternalItemIds[0]).InternalName.ToLower().Contains("pet"))
                        {
                            string[] PetData = ExtraData.Split('\n');

                            int PetType = -1;
                            int PetRace = -1;
                            string PetName = string.Empty;
                            string PetColor = string.Empty;

                            PetType = Convert.ToInt32(Item.GetBaseItem(Item.InternalItemIds[0]).InternalName.ToLower().Substring(6));
                            PetRace = Convert.ToInt32(PetData[1]);
                            PetName = PetData[0];
                            PetColor = PetData[2];

                            int PetId = BrickEngine.GetPetReactor().GeneratePet(Client.GetUser().HabboId, PetType, PetRace, PetColor, PetName);

                            Response ShowPet = new Response(832);
                            ShowPet.AppendInt32(Item.Amount);
                            ShowPet.AppendInt32(3);
                            ShowPet.AppendInt32(Item.Amount);
                            ShowPet.AppendInt32(PetId);
                            Client.SendResponse(ShowPet);

                            Response AddMessage = new Response(603);
                            BrickEngine.GetPetReactor().GetPetInfo(PetId).GetInventoryResponse(AddMessage);
                            Client.SendResponse(AddMessage);
                        }

                        Response ShowItem = new Response(832);
                        ShowItem.AppendInt32(Item.Amount);
                        ShowItem.AppendInt32(Item.GetBaseItem(Item.InternalItemIds[0]).InternalType.ToLower().Equals("s") ? 1 : 2);
                        ShowItem.AppendInt32(Item.Amount);

                        Client.SendResponse(Item.GetPurchaseResponse(false));

                        for (int i = 0; i < Item.Amount; i++)
                        {
                            ShowItem.AppendInt32(BrickEngine.GetItemReactor().InsertItem(Client.GetUser().HabboId, Item.InternalItemIds[0], ExtraData, -1));
                        }

                        Client.SendResponse(ShowItem);
                    }

                    Client.SendResponse(new Response(101));
                }
            }
            else
            {
                if (Item.IsDeal)
                {
                    Client.SendResponse(Item.GetPurchaseResponse(false));

                    for (int i = 0; i < Item.Amount; i++)
                    {
                        foreach (int ItemId in Item.InternalItemIds)
                        {
                            BrickEngine.GetEffectsHandler().InsertEffect(Client, Item.GetBaseItem(ItemId).SpriteId, Item.GetBaseItem(ItemId).InteractorAmount);
                        }
                    }
                }
                else
                {
                    Client.SendResponse(Item.GetPurchaseResponse(false));

                    for (int i = 0; i < Item.Amount; i++)
                    {
                        BrickEngine.GetEffectsHandler().InsertEffect(Client, Item.GetBaseItem(Item.InternalItemIds[0]).SpriteId, Item.GetBaseItem(Item.InternalItemIds[0]).InteractorAmount);
                    }
                }
            }

            if (Item.CreditsCost > 0)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("INSERT INTO user_transactions (user_id, datetime, activity) VALUES (@user_id, @datetime, @activity)");
                    Reactor.AddParam("user_id", Client.GetUser().HabboId);
                    Reactor.AddParam("datetime", DateTime.Now);
                    Reactor.AddParam("activity", -Item.CreditsCost);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void RedeemPresent(Client Client, Item Item)
        {
            iPoint OldPlace = Item.Point;
            int OldRot = Item.Rotation;

            iPoint NewPlace = new iPoint(-1, -1);
            int NewRot = 0;

            Client.GetUser().GetRoom().GetRoomEngine().HandleIncomingItemPickUp(Item, OldPlace, NewPlace, OldRot, NewRot, Client.GetUser().GetRoomUser());

            Item.RoomId = 0;
            Item.Rotation = NewRot;
            Item.Point = NewPlace;
            Item.BaseId = Item.InsideItemId;
            Item.InsideItemId = -1;

            Response ResponseA = new Response(219);
            ResponseA.AppendInt32(Item.Id);
            Client.SendResponse(ResponseA);

            Response ResponseB = new Response(129);
            ResponseB.AppendStringWithBreak(Item.GetBaseItem().InternalType.ToLower());
            ResponseB.AppendInt32(Item.GetBaseItem().SpriteId);
            ResponseB.AppendStringWithBreak(Item.GetBaseItem().InternalName);
            Client.SendResponse(ResponseB);

            Response Response = new Response(832);
            Response.AppendInt32(1);
            Response.AppendInt32(Item.GetBaseItem().InternalType.ToLower().Equals("s") ? 1 : 2);
            Response.AppendInt32(1);
            Response.AppendInt32(Item.Id);
            Client.SendResponse(Response);

            Client.SendResponse(new Response(101));

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET base_id = @baseid, inside_base_id = '-1' WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("baseid", Item.BaseId);
                Reactor.AddParam("itemid", Item.Id);
                Reactor.ExcuteQuery();
            }
        }
    }
}
