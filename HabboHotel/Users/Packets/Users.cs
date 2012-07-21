using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Storage;
using BrickEmulator.Security;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users.Handlers.Membership;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.HabboHotel.Users.Handlers.Clothes;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Users.Handlers.Badges;
using BrickEmulator.HabboHotel.Users.Handlers.Effects;
using BrickEmulator.HabboHotel.Pets;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms;
using System.Text.RegularExpressions;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void HandleIncomingAliveChecker(Client Client, Request Request)
        {
            int INDEX = Request.PopWiredInt32();

            if (INDEX < 0)
            {
                INDEX = 0;
            }

            if (!Client.IsValidUser)
            {
                return;
            }

            Response Response = new Response(354);
            Response.AppendInt32(INDEX);
            Client.SendResponse(Response);

            Client.GetUser().MinutesOnline += 0.5;

            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(Client, 3);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET minutes_online = minutes_online + 0.5 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void GetCredits(Client Client, Request Request)
        {
            Response Response = new Response(6);
            Response.AppendStringWithBreak(Client.GetUser().Credits + ".0");
            Client.SendResponse(Response);
        }

        public void GetUserParams(Client Client, Request Request)
        {
            Response Response = new Response(5);
            Response.AppendRawInt32(Client.GetUser().HabboId);
            Response.AppendChar(2);
            Response.AppendStringWithBreak(Client.GetUser().Username);
            Response.AppendStringWithBreak(Client.GetUser().Look);
            Response.AppendStringWithBreak(Client.GetUser().Gender.ToUpper());
            Response.AppendStringWithBreak(Client.GetUser().Motto);
            Response.AppendStringWithBreak("");
            Response.AppendBoolean(true);
            Response.AppendInt32(Client.GetUser().RespectGained);
            Response.AppendInt32(Client.GetUser().RespectLeft);
            Response.AppendInt32(Client.GetUser().RespectLeftPets);
            Response.AppendBoolean(Client.GetUser().EnabledFriendStream); // StartFriendStreamAuto <3
            Client.SendResponse(Response);
        }

        public void GetAchievementScore(Client Client, Request Request)
        {
            Response Response = new Response(443);
            Response.AppendInt32(Client.GetUser().AchievementScore);
            Client.SendResponse(Response);
        }

        public void GetFurniture(Client Client, Request Request)
        {
            var Items = BrickEngine.GetItemReactor().GetItemsForUser(Client.GetUser().HabboId);

            var FloorItems = new List<Item>();
            var WallItems = new List<Item>();

            foreach (Item Item in Items)
            {
                if (Item.GetBaseItem().InternalType.ToLower() == "s")
                {
                    FloorItems.Add(Item);
                }
                else if (Item.GetBaseItem().InternalType.ToLower() == "i")
                {
                    WallItems.Add(Item);
                }
            }

            Response FloorItemsResponse = new Response(140);
            FloorItemsResponse.AppendStringWithBreak("S");
            FloorItemsResponse.AppendBoolean(true);
            FloorItemsResponse.AppendBoolean(true);
            FloorItemsResponse.AppendInt32(FloorItems.Count);

            foreach (Item Item in FloorItems)
            {
                Item.GetInventoryResponse(FloorItemsResponse);
            }

            Client.SendResponse(FloorItemsResponse);

            Response WallItemsResponse = new Response(140);
            WallItemsResponse.AppendStringWithBreak("I");
            WallItemsResponse.AppendBoolean(true);
            WallItemsResponse.AppendBoolean(true);
            WallItemsResponse.AppendInt32(WallItems.Count);

            foreach (Item Item in WallItems)
            {
                Item.GetInventoryResponse(WallItemsResponse);
            }

            Client.SendResponse(WallItemsResponse);
        }

        public void GetPets(Client Client, Request Request)
        {
            var Pets = BrickEngine.GetPetReactor().GetPetsForInventory(Client.GetUser().HabboId);

            Response Response = new Response(600);
            Response.AppendInt32(Pets.Count);

            foreach (PetInfo Info in Pets)
            {
                Info.GetInventoryResponse(Response);
            }

            Client.SendResponse(Response);
        }

        private void HandleSeenItems(Client Client, Request Request)
        {
            int TabId = Request.PopWiredInt32();

            if (TabId < 1 || TabId > 4)
            {
                return;
            }

            if (TabId <= 4)
            {
                BrickEngine.GetItemReactor().HandleUnseenItems(Client.GetUser().HabboId, TabId);
            }
        }

        public void GetBadges(Client Client, Request Request)
        {
            var Badges = BrickEngine.GetBadgeHandler().GetBadgesForUser(Client.GetUser().HabboId);
            var Equiped = BrickEngine.GetBadgeHandler().GetEquipedBadges(Client.GetUser().HabboId);

            Response Response = new Response(229);
            Response.AppendInt32(Badges.Count);

            foreach (Badge Badge in Badges)
            {
                Response.AppendInt32(BrickEngine.GetBadgeHandler().GetIdForBadge(Badge.BadgeCode));
                Response.AppendStringWithBreak(Badge.BadgeCode);
            }

            Response.AppendInt32(Equiped.Count);

            foreach (Badge Badge in Equiped)
            {
                Response.AppendInt32(Badge.SlotId);
                Response.AppendStringWithBreak(Badge.BadgeCode);
            }

            Client.SendResponse(Response);
        }

        private void UpdateBadge(Client Client, Request Request)
        {
            var Badges = BrickEngine.GetBadgeHandler().GetBadgesForUser(Client.GetUser().HabboId);;

            // Null every badge ready for update
            foreach (Badge Badge in Badges)
            {
                Badge.SlotId = 0;
            }

            // Check new badge updates
            while (Request.RemainingLength > 0)
            {
                int SlotId = Request.PopWiredInt32();
                string Badge = Request.PopFixedString();

                Badge SelectedBadge = BrickEngine.GetBadgeHandler().GetBadge(Badge, Client.GetUser().HabboId);

                if (SelectedBadge != null)
                {
                    SelectedBadge.SlotId = SlotId;
                }
            }

            // Cache Equiped Badges Again for Update
            var Equiped = BrickEngine.GetBadgeHandler().GetEquipedBadges(Client.GetUser().HabboId);

            // Send update response
            Response Response = new Response(228);
            Response.AppendInt32(Client.GetUser().HabboId);
            Response.AppendInt32(Equiped.Count);

            foreach (Badge Badge in Equiped)
            {
                Response.AppendInt32(Badge.SlotId);
                Response.AppendStringWithBreak(Badge.BadgeCode);
            }

            if (Client.GetUser().IsInRoom)
            {
                Client.SendRoomResponse(Response);
            }
            else
            {
                Client.SendResponse(Response);
            }

            foreach (Badge Badge in Badges)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE user_badges SET slot_id = @slotid WHERE badge = @badge AND user_id = @habboid LIMIT 1");
                    Reactor.AddParam("slotid", Badge.SlotId);
                    Reactor.AddParam("badge", Badge.BadgeCode);
                    Reactor.AddParam("habboid", Client.GetUser().HabboId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void GetMembershipParams(Client Client, Request Request)
        {
            Membership Membership = Client.GetUser().GetMembership();

            if (Membership == null)
            {
                Response Response = new Response(7);
                Response.AppendStringWithBreak("club_habbo");
                Response.AppendInt32(0); // daysleft (except months)
                Response.AppendInt32(0); // giftsselected
                Response.AppendInt32(0); // monthsleft (except days /\ )
                Response.AppendBoolean(true); // Static always
                Response.AppendInt32(0); // MembershipType 0 = Basic, 1 = VIP
                Response.AppendInt32(0); // MembershipType 0 = Basic, 1 = VIP
                Response.AppendInt32(0); // hc days done (individual)
                Response.AppendInt32(0); // vip days done
                Response.AppendBoolean(false); // Show dialog (first time / membership expired)
                Response.AppendBoolean(false); // Static always
                Response.AppendInt32(25); // regular price
                Client.SendResponse(Response);
            }
            else
            {
                Response Response = new Response(7);
                Response.AppendStringWithBreak("club_habbo");
                Response.AppendInt32(Membership.DaysLeft); // daysleft (except months)
                Response.AppendInt32(Membership.GiftsSelectedAmount); // giftsselected
                Response.AppendInt32(Membership.MonthsLeft); // monthsleft (except days /\ )
                Response.AppendBoolean(true); // Static always
                Response.AppendInt32(Membership.MemberScale); // MembershipType 0 = Basic, 1 = VIP
                Response.AppendInt32(Membership.MemberScale); // MembershipType 0 = Basic, 1 = VIP
                Response.AppendInt32(Membership.TotDaysPassedBasic); // hc days done (individual)
                Response.AppendInt32(Membership.TotDaysPassedVip); // vip days done
                Response.AppendBoolean(false); // Show dialog (first time / membership expired)
                Response.AppendBoolean(false); // Static always
                Response.AppendInt32(25); // regular price
                Client.SendResponse(Response);
            }
        }

        private void GetIgnoredUsers(Client Client, Request Request)
        {
            Response Response = new Response(420);
            Response.AppendInt32(Client.GetUser().IgnoredUsers.Count);

            foreach (int UserId in Client.GetUser().IgnoredUsers)
            {
                Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(UserId));
            }

            Client.SendResponse(Response);
        }

        private void GetClothes(Client Client, Request Request)
        {
            Membership Membership = Client.GetUser().GetMembership();

            if (Membership != null)
            {
                Client.SendResponse(BrickEngine.GetClothesHandler().GetWardRobeResponse(Client.GetUser().HabboId, ((Membership.MemberScale + 1) * 5)));
            }
        }

        private void UpdateClothe(Client Client, Request Request)
        {            
            Membership Membership = Client.GetUser().GetMembership();

            if (Membership == null)
            {
                return;
            }

            int SlotId = Request.PopWiredInt32();
            string Look = BrickEngine.CleanString(Request.PopFixedString());
            string Gender = BrickEngine.CleanString(Request.PopFixedString());

            var LeakedRow = new Dictionary<int, Object>();

            SecurityCounter Counter = new Security.SecurityCounter(-1);

            LeakedRow[Counter.Next] = Client.GetUser().HabboId;
            LeakedRow[Counter.Next] = SlotId;
            LeakedRow[Counter.Next] = Look;
            LeakedRow[Counter.Next] = Gender;

            Clothe LeakedClothe = new Clothe(LeakedRow);

            BrickEngine.GetClothesHandler().UpdateClothe(LeakedClothe.HabboId, LeakedClothe);
        }

        private void UpdateLook(Client Client, Request Request)
        {
            string Gender = Request.PopFixedString();
            string Look = Request.PopFixedString();

            Client.GetUser().Gender = Gender;
            Client.GetUser().Look = Look;

            Client.GetUser().RefreshUser();

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET look = @look, gender = @gender WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("look", Look);
                Reactor.AddParam("gender", Gender.ToUpper());
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void UpdateMotto(Client Client, Request Request)
        {
            string Motto = BrickEngine.CleanString(Request.PopFixedString());

            Client.GetUser().Motto = Motto;

            Client.GetUser().RefreshUser();

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET motto = @motto WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("motto", Motto);
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }

            BrickEngine.GetStreamHandler().AddStream(Client.GetUser().HabboId, Users.Handlers.Messenger.Streaming.StreamType.EditedMotto, Motto);
        }

        private void Wave(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            Response Response = new Response(481);
            Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);

            Client.SendRoomResponse(Response);
        }

        private void Dance(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int DanceId = Request.PopWiredInt32();

            Client.GetUser().GetRoomUser().DanceId = DanceId;

            Response Response = new Response(480);
            Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);
            Response.AppendInt32(DanceId);

            Client.SendRoomResponse(Response);
        }

        private void Talk(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (Client.GetUser().Muted)
            {
                Client.Notif("You're muted.", false);
                return;
            }

            string Message = Request.PopFixedString();

            Message = BrickEngine.CleanString(Message);

            int Emoticon = Client.GetUser().GetRoomUser().GetEmoticon(Message);

            Client.GetUser().GetRoomUser().Talk(Message, Rooms.Virtual.Units.SpeechType.Talk, Emoticon, "");
        }

        private void Shout(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (Client.GetUser().Muted)
            {
                Client.Notif("You're muted.", false);
                return;
            }

            string Message = Request.PopFixedString();

            Message = BrickEngine.CleanString(Message);

            int Emoticon = Client.GetUser().GetRoomUser().GetEmoticon(Message);

            Client.GetUser().GetRoomUser().Talk(Message, Rooms.Virtual.Units.SpeechType.Shout, Emoticon, "");
        }

        private void Whisper(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (Client.GetUser().Muted)
            {
                Client.Notif("You're muted.", false);
                return;
            }

            string Params = Request.PopFixedString();

            string TriggerdUserName = Params.Split(' ')[0];
            string Message = Params.Replace(TriggerdUserName + ' ',"");

            int Emoticon = Client.GetUser().GetRoomUser().GetEmoticon(Message);

            Client.GetUser().GetRoomUser().Talk(Message, Rooms.Virtual.Units.SpeechType.Whisper, Emoticon, TriggerdUserName);
        }

        private void BeginTyping(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            Response Response = new Response(361);
            Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);
            Response.AppendBoolean(true);

            Client.SendRoomResponse(Response);
        }

        private void EndTyping(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            Response Response = new Response(361);
            Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);
            Response.AppendBoolean(false);

            Client.SendRoomResponse(Response);
        }

        private void WalkTo(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int X = Request.PopWiredInt32();
            int Y = Request.PopWiredInt32();

            if (Client.GetUser().GetRoomUser().TeleportingEnabled)
            {
                Client.GetUser().GetRoomUser().Teleport(new iPoint(X, Y));
                return;
            }

            if (!Client.GetUser().GetRoomUser().WalkFreezed)
            {
                Client.GetUser().GetRoomUser().UnhandledGoalPoint = new iPoint(X, Y);
            }
        }

        private void FocusOnTile(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (Client.GetUser().GetRoomUser().IsWalking)
            {
                return;
            }

            int X = Request.PopWiredInt32();
            int Y = Request.PopWiredInt32();

            iPoint MainPlace = Client.GetUser().GetRoomUser().Point;
            iPoint FocusPlace = new iPoint(X,Y);

            if (MainPlace.Compare(FocusPlace) || Client.GetUser().GetRoomUser().NeedsLastWalk)
            {
                return;
            }

            int RotationToPoint = Rotation.Calculate(MainPlace,FocusPlace);

            Client.GetUser().GetRoomUser().RotBody = RotationToPoint;
            Client.GetUser().GetRoomUser().RotHead = RotationToPoint;
            Client.GetUser().GetRoomUser().UpdateStatus(true);
        }

        private void GetUserBadges(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int HabboId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            var Equiped = BrickEngine.GetBadgeHandler().GetEquipedBadges(HabboId);

            Response Response = new Response(228);
            Response.AppendInt32(HabboId);
            Response.AppendInt32(Equiped.Count);

            foreach (Badge Badge in Equiped)
            {
                Response.AppendInt32(Badge.SlotId);
                Response.AppendStringWithBreak(Badge.BadgeCode);
            }

            Client.SendResponse(Response);
        }

        private void GetUserTags(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int HabboId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            Response Response = new Response(350);
            Response.AppendInt32(HabboId);
            Response.AppendInt32(TriggeredUser.GetClient().GetUser().Tags.Count);

            TriggeredUser.GetClient().GetUser().Tags.ForEach(Response.AppendStringWithBreak);

            Client.SendResponse(Response);
        }

        private void RespectPet(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (Client.GetUser().RespectLeftPets < 0)
            {
                return;
            }

            int PetId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredPet = Client.GetUser().GetRoom().GetRoomEngine().GetUserByPetId(PetId);

            if (TriggeredPet == null)
            {
                return;
            }

            PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(TriggeredPet.HabboId);

            Client.GetUser().RespectLeftPets--;

            Info.GiveRespect();

            // Update Left
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET respect_left_pets = respect_left_pets - 1 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void RespectUser(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (Client.GetUser().RespectLeft < 0)
            {
                return;
            }

            int HabboId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            TriggeredUser.GetClient().GetUser().RespectGained++;
                
            Response Response = new Response(440);
            Response.AppendInt32(HabboId);
            Response.AppendInt32(TriggeredUser.GetClient().GetUser().RespectGained);
            Client.SendRoomResponse(Response);

            Client.GetUser().RespectLeft--;
            Client.GetUser().RespectGiven++;

            // Update TriggeredUser
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET respect_gained = respect_gained + 1 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", HabboId);
                Reactor.ExcuteQuery();
            }

            // Update Left
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET respect_left = respect_left - 1 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }

            // Update Given
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET respect_given = respect_given + 1 WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void IgnoreUser(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            string TriggeredUsername = Request.PopFixedString();

            int HabboId = BrickEngine.GetUserReactor().GetId(TriggeredUsername);

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            if (TriggeredUser.GetClient().GetUser().Rank > 1)
            {
                Client.Notif("You can't ignore staff.", false);
                return;
            }

            if (Client.GetUser().HasIgnoredUser(HabboId))
            {
                return;
            }

            Client.GetUser().IgnoredUsers.Add(HabboId);

            Response Response = new Response(419);
            Response.AppendBoolean(true);
            Client.SendResponse(Response);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO user_ignores (user_id, ignore_id) VALUES (@userid, @ignoreid)");
                Reactor.AddParam("userid", Client.GetUser().HabboId);
                Reactor.AddParam("ignoreid", HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void UnIgnoreUser(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            string TriggeredUsername = Request.PopFixedString();

            int HabboId = BrickEngine.GetUserReactor().GetId(TriggeredUsername);

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            if (!Client.GetUser().HasIgnoredUser(HabboId))
            {
                return;
            }

            Client.GetUser().IgnoredUsers.Remove(HabboId);

            Response Response = new Response(419);
            Response.AppendInt32(3);
            Client.SendResponse(Response);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM user_ignores WHERE user_id = @userid AND ignore_id = @ignoreid LIMIT 1");
                Reactor.AddParam("userid", Client.GetUser().HabboId);
                Reactor.AddParam("ignoreid", HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void ShowEffect(Client Client, Request Request)
        {
            int EffectId = Request.PopWiredInt32();

            if (EffectId == 0)
            {
                return;
            }

            if (EffectId == -1)
            {
                BrickEngine.GetEffectsHandler().RunEffect(Client, null);
            }
            else
            {
                Effect Effect = BrickEngine.GetEffectsHandler().GetLastAddedEffect(Client.GetUser().HabboId, EffectId);

                if (Effect == null)
                {
                    return;
                }

                BrickEngine.GetEffectsHandler().RunEffect(Client, Effect);
            }
        }

        private void ActivateEffect(Client Client, Request Request)
        {
            int EffectId = Request.PopWiredInt32();

            if (EffectId == 0)
            {
                return;
            }

            Effect Effect = BrickEngine.GetEffectsHandler().GetLastAddedEffect(Client.GetUser().HabboId, EffectId);

            if (Effect == null)
            {
                return;
            }


            BrickEngine.GetEffectsHandler().ActivateEffect(Client, Effect);
        }

        private void CheckNewName(Client Client, Request Request)
        {
            string Username = BrickEngine.CleanString(Request.PopFixedString());

            if (Username.ToLower().Equals(Client.GetUser().Username))
            {
                return;
            }

            int ErrorMessage = 0;

            if (Username.Length < 3)
            {
                ErrorMessage = 2;
            }
            else if (Username.Length > 15)
            {
                ErrorMessage = 3;
            }
            else if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Username))
            {
                ErrorMessage = 4;
            }
            else if (!BrickEngine.GetUserReactor().GetId(Username).Equals(0))
            {
                ErrorMessage = 5;
            }
            else if (!BrickEngine.GetConfigureFile().CallBooleanKey("welcome.changename"))
            {
                ErrorMessage = 6;
            }

            Response Response = new Response(571);
            Response.AppendInt32(ErrorMessage);
            Response.AppendStringWithBreak(Username);

            if (ErrorMessage.Equals(5))
            {
                char[] RandomChars = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', '1', '2', '3', '4', 'x', 'y', 'z', '?', '!' };

                Response.AppendInt32(5);

                for (int i = 0; i < 6; i++)
                {
                    Response.AppendStringWithBreak(
                        string.Format("{0}{1}", Username,
                            string.Format("{0}{1}{2}",
                                RandomChars[BrickEngine.Random.Next(0, RandomChars.Length - 1)],
                                RandomChars[BrickEngine.Random.Next(0, RandomChars.Length - 1)],
                                RandomChars[BrickEngine.Random.Next(0, RandomChars.Length - 1)])));
                }
            }
            else
            {
                Response.AppendInt32(0);
            }
           
            Client.SendResponse(Response);
        }

        private void ChangeName(Client Client, Request Request)
        {
            string Username = BrickEngine.CleanString(Request.PopFixedString());

            Boolean UpdatedName = false;

            if (!Client.GetUser().Username.ToLower().Equals(Username.ToLower()))
            {
                foreach (VirtualRoom Room in BrickEngine.GetRoomReactor().GetMe(Client.GetUser().HabboId))
                {
                    if (Room.InternalState.Equals(RoomRunningState.Alive))
                    {
                        Boolean IsMatch = false;

                        if (Regex.IsMatch(Room.Name.ToLower(), Client.GetUser().Username.ToLower()))
                        {
                            Room.Name = Room.Name.Replace(Client.GetUser().Username, Username);
                            IsMatch = true;
                        }
                        
                        if (Regex.IsMatch(Room.Description.ToLower(), Client.GetUser().Username.ToLower()))
                        {
                            Room.Description = Room.Description.Replace(Client.GetUser().Username, Username);
                            IsMatch = true;
                        }

                        if (IsMatch)
                        {
                            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                            {
                                Reactor.SetQuery("UPDATE private_rooms SET name = @name, description = @desc WHERE id = @roomid LIMIT 1");
                                Reactor.AddParam("name", Room.Name);
                                Reactor.AddParam("desc", Room.Description);
                                Reactor.AddParam("roomid", Room.Id);
                                Reactor.ExcuteQuery();
                            }

                            Response Data = new Response(454);
                            Data.AppendBoolean(true);
                            Room.GetNavigatorResponse(Data, false);
                            Client.SendRoomResponse(Data);
                        }
                    }
                }

                Client.GetUser().Username = Username;
                Client.GetUser().RefreshUser();

                Client.Notif("Successfully changed name, reload your room to see changes.", false);

                if (Client.GetUser().EnableShowOnline)
                {
                    lock (Client.GetUser().MessengerLocker)
                    {
                        BrickEngine.GetMessengerHandler().AlertStatusFriends(Client.GetUser(), true);
                    }
                }

                UpdatedName = true;
            }

            Client.GetUser().ProgressedNewbie = true;

            Response Response = new Response();
            Response.Initialize(571);
            Response.AppendBoolean(false);
            Response.AppendStringWithBreak(Username);
            Response.AppendBoolean(false);
            Response.Initialize(570);
            Response.AppendBoolean(false);
            Response.AppendStringWithBreak(Username);
            Response.AppendBoolean(false);
            Client.SendResponse(Response);

            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(Client, 4);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET changed_name = '1' WHERE id = @habboid");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }

            if (UpdatedName)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE users SET username = @username WHERE id = @habboid");
                    Reactor.AddParam("habboid", Client.GetUser().HabboId);
                    Reactor.AddParam("username", Username);
                    Reactor.ExcuteQuery();
                }
            }
        }

        private void GoFindNewFriends(Client Client, Request Request)
        {
            int CurrentRoom = Client.GetUser().RoomId;
            int RandomRoom = BrickEngine.GetRoomReactor().Random(CurrentRoom, Client.GetUser().HabboId);

            Response Response = new Response(831);
            Response.AppendBoolean(RandomRoom > 0);

            if (!Client.GetUser().SearchedForFriends || RandomRoom <= 0)
            {
                Client.SendResponse(Response);
            }

            if (RandomRoom > 0)
            {
                BeginLoadRoom(Client, RandomRoom, string.Empty);

                Client.GetUser().SearchedForFriends = true;
            }
        }
    }
}
