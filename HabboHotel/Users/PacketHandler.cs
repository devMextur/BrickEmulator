using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        #region Fields
        private Dictionary<int, Interaction> Interactions;

        private delegate void Interaction(Client Client, Request Request);

        public readonly Boolean ShowResponses = false;
        public readonly Boolean ShowRequests = false;
        #endregion

        #region Constructors
        public PacketHandler()
        {
            ShowResponses = BrickEngine.GetConfigureFile().CallBooleanKey("packethandler.showresponses");
            ShowRequests = BrickEngine.GetConfigureFile().CallBooleanKey("packethandler.showrequests");
        }
        #endregion

        #region Methods
        public void LoadInteractions()
        {
            Interactions = new Dictionary<int, Interaction>();

            RegisterOthers();
            RegisterShop();
            RegisterUsers();
            RegisterNavigator();
            RegisterMissions();
            RegisterRooms();
            RegisterMessenger();
            RegisterTools();

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] RequestInteractions(s) ready for invoke.", Interactions.Count), IO.WriteType.Outgoing);
        }

        private void RegisterOthers()
        {
            Interactions[206] = new Interaction(HandleSessionDetails);
            Interactions[415] = new Interaction(HandleTicket);
        }

        private void RegisterUsers()
        {
            Interactions[7] = new Interaction(GetUserParams);
            Interactions[8] = new Interaction(GetCredits);
            Interactions[26] = new Interaction(GetMembershipParams);
            Interactions[3110] = new Interaction(GetAchievementScore);
            Interactions[44] = new Interaction(UpdateLook);
            Interactions[94] = new Interaction(Wave);
            Interactions[52] = new Interaction(Talk);
            Interactions[55] = new Interaction(Shout);
            Interactions[93] = new Interaction(Dance);
            Interactions[317] = new Interaction(BeginTyping);
            Interactions[318] = new Interaction(EndTyping);
            Interactions[484] = new Interaction(UpdateMotto);
            Interactions[79] = new Interaction(FocusOnTile);
            Interactions[53] = new Interaction(GoHotelView);
            Interactions[75] = new Interaction(WalkTo);
            Interactions[315] = new Interaction(HandleIncomingAliveChecker);
            Interactions[375] = new Interaction(GetClothes);
            Interactions[376] = new Interaction(UpdateClothe);
            Interactions[371] = new Interaction(RespectUser);
            Interactions[56] = new Interaction(Whisper);
            Interactions[321] = new Interaction(GetIgnoredUsers);
            Interactions[319] = new Interaction(IgnoreUser);
            Interactions[322] = new Interaction(UnIgnoreUser);
            Interactions[404] = new Interaction(GetFurniture);
            Interactions[157] = new Interaction(GetBadges);
            Interactions[158] = new Interaction(UpdateBadge);
            Interactions[159] = new Interaction(GetUserBadges);
            Interactions[263] = new Interaction(GetUserTags);
            Interactions[3000] = new Interaction(GetPets);
            Interactions[3111] = new Interaction(HandleSeenItems);
            Interactions[372] = new Interaction(ShowEffect);
            Interactions[373] = new Interaction(ActivateEffect);
            Interactions[471] = new Interaction(CheckNewName);
            Interactions[470] = new Interaction(ChangeName);
            Interactions[490] = new Interaction(GoFindNewFriends);
        }

        private void RegisterShop()
        {
            Interactions[100] = new Interaction(PurchaseShopItem);
            Interactions[101] = new Interaction(SerializeShopButtons);
            Interactions[102] = new Interaction(SerialzeShopPage);
            Interactions[3007] = new Interaction(SerializePetRaces);
            Interactions[42] = new Interaction(GetPetName);

            Interactions[3010] = new Interaction(PostItemOnMarketplace);
            Interactions[3012] = new Interaction(GetMarketPlaceTickets);
            Interactions[3015] = new Interaction(TakeBackItem);
            Interactions[3018] = new Interaction(GetMarketPlaceOffers);
            Interactions[3020] = new Interaction(GetMarketPlaceItemInfo);
            Interactions[3019] = new Interaction(GetMarketPlaceOwnOffers);
            Interactions[3013] = new Interaction(PurchaseTickets);
            Interactions[3016] = new Interaction(GetSaledCredits);
            Interactions[3014] = new Interaction(PurchaseOffer);

            Interactions[3011] = new Interaction(GetShopInfoA);
            Interactions[473] = new Interaction(GetShopInfoB);

            Interactions[412] = new Interaction(GetEcotronRewards);
            Interactions[413] = new Interaction(GetEcotronInfo);
            Interactions[414] = new Interaction(GetEcotronPackage);
        }

        private void RegisterNavigator()
        {
            Interactions[380] = new Interaction(GetFeatured);
            Interactions[434] = new Interaction(GetMe);
            Interactions[430] = new Interaction(GetRooms);
            Interactions[431] = new Interaction(GetHighestScore);
            Interactions[387] = new Interaction(CheckRoomCreate);
            Interactions[19] = new Interaction(UpdateFavorite);
            Interactions[20] = new Interaction(UpdateFavorite);
            Interactions[439] = new Interaction(GetEventRooms);
            Interactions[382] = new Interaction(GetPopulairTags);
            Interactions[437] = new Interaction(SearchRooms);
            Interactions[438] = new Interaction(SearchRooms);
            Interactions[29] = new Interaction(CreateRoom);
            Interactions[151] = new Interaction(GetPrivates);
            Interactions[435] = new Interaction(GetFavoriteRooms);
            Interactions[436] = new Interaction(GetLastVisitedRooms);
            Interactions[384] = new Interaction(UpdateHomeRoom);
        }

        private void RegisterMissions()
        {
            Interactions[370] = new Interaction(GetAchievementResponse);
            Interactions[3101] = new Interaction(GetQuestList);
        }

        private void RegisterRooms()
        {
            Interactions[388] = new Interaction(OpenFeacturedRoom);
            Interactions[391] = new Interaction(OpenPrivateRoom);
            Interactions[215] = new Interaction(ActivateLoading);
            Interactions[390] = new Interaction(GetMapParams);
            Interactions[126] = new Interaction(EndLoadRoom);
            Interactions[345] = new Interaction(GetEventMenu);
            Interactions[346] = new Interaction(UpdateEvent);
            Interactions[348] = new Interaction(UpdateEvent);
            Interactions[347] = new Interaction(EndEvent);
            Interactions[400] = new Interaction(BeginEditRoom);
            Interactions[401] = new Interaction(EndEditRoom);
            Interactions[386] = new Interaction(UpdateRoomIcon);
            Interactions[23] = new Interaction(DestroyRoom);
            Interactions[95] = new Interaction(KickUser);
            Interactions[320] = new Interaction(BanUser);
            Interactions[96] = new Interaction(GiveRights);
            Interactions[97] = new Interaction(TakeRights);
            Interactions[155] = new Interaction(TakeAllRights);
            Interactions[261] = new Interaction(RateRoom);
            Interactions[90] = new Interaction(MoveItemToRoom);
            Interactions[73] = new Interaction(UpdateFloorItem);
            Interactions[91] = new Interaction(UpdateWallItem);
            Interactions[67] = new Interaction(PickUpItem);
            Interactions[392] = new Interaction(TriggerSelectedItem);
            Interactions[66] = new Interaction(UpdateRoomLayout);
            Interactions[78] = new Interaction(RedeemPresent);

            Interactions[3001] = new Interaction(GetPetInfo);
            Interactions[3002] = new Interaction(MovePetToRoom);
            Interactions[3003] = new Interaction(MovePetToInventory);
            Interactions[3004] = new Interaction(GetPetsTool);
            Interactions[3005] = new Interaction(RespectPet);
        }

        private void RegisterMessenger()
        {
            Interactions[12] = new Interaction(GetMessenger);
            Interactions[41] = new Interaction(SearchUsers);
            Interactions[33] = new Interaction(ChatWithFriend);
            Interactions[34] = new Interaction(InviteFriends);
            Interactions[40] = new Interaction(DeleteFriends);
            Interactions[39] = new Interaction(RequestUser);
            Interactions[37] = new Interaction(AcceptRequests);
            Interactions[38] = new Interaction(DenyRequests);
            Interactions[262] = new Interaction(FollowBuddy);
            Interactions[500] = new Interaction(GetFriendStreams);
            Interactions[501] = new Interaction(UpdateFriendStreaming);
        }

        private void RegisterTools()
        {
            Interactions[459] = new Interaction(GetRoomInfo);
            Interactions[454] = new Interaction(GetUserInfo);
            Interactions[458] = new Interaction(GetUserRoomVisits);
            Interactions[455] = new Interaction(GetUserChatlogs);
            Interactions[456] = new Interaction(GetRoomChatlogs);
            Interactions[462] = new Interaction(AlertSelectedUser);
            Interactions[463] = new Interaction(KickSelectedUser);
            Interactions[461] = new Interaction(WarnSelectedUser);
            Interactions[464] = new Interaction(BanSelectedUser);
            Interactions[200] = new Interaction(AlertSelectedRoom);
            Interactions[460] = new Interaction(PreformAction);
        }

        public void HandlePacket(Request Request, Client Client)
        {
            if (Request == null)
            {
                return;
            }

            if (BrickEngine.GetPacketHandler().ShowRequests)
            {
                BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] > {1}", Request.Id, Request), IO.WriteType.Incoming);
            }

            if (!Interactions.ContainsKey(Request.Id) && !Request.Type.Equals(RequestType.Policy))
            {
                return;
            }

            if (Request.Type == RequestType.Policy)
            {
                SendPolicy(Client);
            }
            else if (Request.Type == RequestType.Default)
            {
                if (!Client.Authenticated)
                {
                    if (!Request.Id.Equals(206) && !Request.Id.Equals(415))
                    {
                        return;
                    }
                }

                if (Interactions[Request.Id] != null)
                {
                    Interactions[Request.Id].Invoke(Client, Request);
                }
            }
        }

        public void SendPolicy(Client Client)
        {
            Client.SendResponse(Encoding.Default.GetBytes("<?xml version=\"1.0\"?> " +
                                "<!DOCTYPE cross-domain-policy SYSTEM \"/xml/dtds/cross-domain-policy.dtd\"> " +
                                "<cross-domain-policy> " +
                                "<allow-access-from domain=\"*\" to-ports=\"1-31111\" /> " +
                                "</cross-domain-policy>\x0"));
        }
        #endregion
    }
}
