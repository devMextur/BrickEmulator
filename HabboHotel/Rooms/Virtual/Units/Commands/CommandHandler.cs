using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.HabboHotel.Users;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.Security;
using BrickEmulator.Network;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Pets;

namespace BrickEmulator.HabboHotel.Rooms.Virtual.Units.Commands
{
    class CommandHandler
    {
        #region Fields
        private Dictionary<KeyValuePair<string, string>, KeyValuePair<Handler, int>> Handlers;
        private delegate void Handler(Client Client, List<string> Params);
        #endregion

        #region Constructors
        public CommandHandler() { }
        #endregion

        #region Methods
        public void Prepare()
        {
            Handlers = new Dictionary<KeyValuePair<string, string>, KeyValuePair<Handler, int>>();

            Handlers[new KeyValuePair<string, string>("pickall", "Pick all items in room.")] = new KeyValuePair<Handler, int>(new Handler(PickAll), 1);
            Handlers[new KeyValuePair<string, string>("empty", "Empty your inventory + pets.")] = new KeyValuePair<Handler, int>(new Handler(Empty), 1);
            Handlers[new KeyValuePair<string, string>("whosonline", "Shows all users that are online.")] = new KeyValuePair<Handler, int>(new Handler(WhosOnline), 1);
            Handlers[new KeyValuePair<string, string>("whosinroom", "Shows all users that are in a room.")] = new KeyValuePair<Handler, int>(new Handler(WhosInRoom), 1);
            Handlers[new KeyValuePair<string, string>("whoshotelview", "Shows all users are at the hotelview.")] = new KeyValuePair<Handler, int>(new Handler(WhosHotelView), 1);
            Handlers[new KeyValuePair<string, string>("about", "Shows an messagebox with server info.")] = new KeyValuePair<Handler, int>(new Handler(AboutMessage), 1);
            Handlers[new KeyValuePair<string, string>("commands", "Shows an messagebox with a list of commands.")] = new KeyValuePair<Handler, int>(new Handler(Commands), 1);

            Handlers[new KeyValuePair<string, string>("mute", "<username> <seconds in time> Mutes specifique user.")] = new KeyValuePair<Handler, int>(new Handler(Mute), 2);
            Handlers[new KeyValuePair<string, string>("unmute", "<username> Unmutes specifique user.")] = new KeyValuePair<Handler, int>(new Handler(UnMute), 2);

            Handlers[new KeyValuePair<string, string>("muteroom", "<seconds in time> Mutes all room visitors.")] = new KeyValuePair<Handler, int>(new Handler(MuteRoom), 5);
            Handlers[new KeyValuePair<string, string>("unmuteroom", "Un-mutes all room visitors.")] = new KeyValuePair<Handler, int>(new Handler(UnMuteRoom), 5);

            Handlers[new KeyValuePair<string, string>("givepixels", "<username> <amount> Give specifique user pixels.")] = new KeyValuePair<Handler, int>(new Handler(GivePixels), 5);
            Handlers[new KeyValuePair<string, string>("givecredits", "<username> <amount> Give specifique user credits.")] = new KeyValuePair<Handler, int>(new Handler(GiveCredits), 5);

            Handlers[new KeyValuePair<string, string>("ha", "<message> - Broadcast notifcation.")] = new KeyValuePair<Handler, int>(new Handler(HotelAlert), 6);
            Handlers[new KeyValuePair<string, string>("givebadge", "<username> <badgecode> Give specifique user a badge.")] = new KeyValuePair<Handler, int>(new Handler(GiveBadge), 6);
            Handlers[new KeyValuePair<string, string>("ipban", "<username> <reason> IPban specifique user.")] = new KeyValuePair<Handler, int>(new Handler(IPBan), 6);

            Handlers[new KeyValuePair<string, string>("refresh_items", "Refreshes all base-items.")] = new KeyValuePair<Handler, int>(new Handler(RefreshItems), 7);
            Handlers[new KeyValuePair<string, string>("refresh_catalogue", "Refreshes all catalogue items.")] = new KeyValuePair<Handler, int>(new Handler(RefreshCatalogue), 7);
            Handlers[new KeyValuePair<string, string>("refresh_navigator", "Refreshes all Feactured items.")] = new KeyValuePair<Handler, int>(new Handler(RefreshNavigator), 7);
            Handlers[new KeyValuePair<string, string>("effect", "<effectid> Enables specifique effect.")] = new KeyValuePair<Handler, int>(new Handler(Effect), 7);
            Handlers[new KeyValuePair<string, string>("summon", "<username> Sends user to your room and in font of you.")] = new KeyValuePair<Handler, int>(new Handler(Summon), 7);

            Handlers[new KeyValuePair<string, string>("broadcastcredits", "<amount> Give every online user credits.")] = new KeyValuePair<Handler, int>(new Handler(BroadcastCredits), 7);
            Handlers[new KeyValuePair<string, string>("broadcastpixels", "<amount> Give every online user pixels.")] = new KeyValuePair<Handler, int>(new Handler(BroadcastPixels), 7);
            Handlers[new KeyValuePair<string, string>("broadcastbadge", "<badgecode> Give every online user a badge.")] = new KeyValuePair<Handler, int>(new Handler(BroadcastBadge), 7);

            Handlers[new KeyValuePair<string, string>("teleport", "You're lazy, you don't have to walk.")] = new KeyValuePair<Handler, int>(new Handler(Teleport), 7);
            Handlers[new KeyValuePair<string, string>("unloadroom", "<roomid> Unload current room or specifique room.")] = new KeyValuePair<Handler, int>(new Handler(UnloadRoom), 7);
            Handlers[new KeyValuePair<string, string>("singpets", "<chatmessage> Let every pet chat and dance.")] = new KeyValuePair<Handler, int>(new Handler(SingPets), 7);
            Handlers[new KeyValuePair<string, string>("clearmotto", "<username> Clear motto of a specifique user.")] = new KeyValuePair<Handler, int>(new Handler(ClearMotto), 7);
            Handlers[new KeyValuePair<string, string>("disconnect", "<username> Dicsonnects a specifique user.")] = new KeyValuePair<Handler, int>(new Handler(DisconnectUser), 7);
            Handlers[new KeyValuePair<string, string>("dropevents", "Drops every event that is running.")] = new KeyValuePair<Handler, int>(new Handler(DropEvents), 7);

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] UserCommand(s) cached.", Handlers.Count), IO.WriteType.Outgoing);
        }

        public Boolean HandleCommand(string Command, Client Client, List<string> Params)
        {
            string Lowered = Command.ToLower();

            foreach (KeyValuePair<KeyValuePair<string, string>, KeyValuePair<Handler, int>> kvp in Handlers)
            {
                if (kvp.Key.Key.ToLower().Equals(Command))
                {
                    if (kvp.Value.Key != null && Client.GetUser().Rank >= kvp.Value.Value)
                    {
                        if (Client.GetUser().Rank >= kvp.Value.Value)
                        {
                            kvp.Value.Key.Invoke(Client, Params);
                            return true;
                        }
                        else
                        {
                            Client.Notif("You have no rights to do that command.", true);
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        private string GetParams(string Command)
        {
            foreach (KeyValuePair<KeyValuePair<string, string>, KeyValuePair<Handler, int>> kvp in Handlers)
            {
                if (kvp.Key.Key.ToLower().Equals(Command))
                {
                    return string.Format(":{0} : {1}", Command, kvp.Key.Value);
                }
            }

            return string.Format(":{0}", Command);
        }

        private void HotelAlert(Client Client, List<string> Params)
        {
            StringBuilder WholeAlert = new StringBuilder();

            int i = 0;

            foreach (string Param in Params)
            {
                if (i > 0 && i < Params.Count)
                {
                    WholeAlert.Append(' ');
                }

                WholeAlert.Append(Param);

                i++;
            }

            WholeAlert.AppendLine();

            WholeAlert.AppendLine("From: " + Client.GetUser().Username);

            Response Response = new Response(139);
            Response.AppendStringWithBreak(WholeAlert.ToString());
            BrickEngine.GetSocketShield().BroadcastResponse(Response);
        }

        private void AboutMessage(Client Client, List<string> Params)
        {
            Response Response = new Response(808);
            Response.AppendStringWithBreak("BrickEmulator [C#]");
            Response.AppendStringWithBreak("This hotel is proudly powered by BrickEmulator, the advanced Habbo Hotel emulator.");
            Response.AppendChar(10);
            Response.AppendChar(10);
            Response.AppendStringWithBreak("http://forum.ragezone.com/f331/brickemulator-c-pooling-sockets-scratch-770243/");
            Client.SendResponse(Response);
        }

        private void Commands(Client Client, List<string> Params)
        {
            StringBuilder Commands = new StringBuilder();

            Commands.AppendLine(string.Format("BrickEmulator [C#] ~ Commands : Rank [{0}]", Client.GetUser().Rank));

            foreach (KeyValuePair<KeyValuePair<string, string>, KeyValuePair<Handler, int>> kvp in Handlers)
            {
                if (Client.GetUser().Rank >= kvp.Value.Value)
                {
                    Commands.Append(string.Format(" {0}\r", GetParams(kvp.Key.Key)));
                }
            }

            Client.LongNotif(Commands);
        }

        private void RefreshCatalogue(Client Client, List<string> Params)
        {
            BrickEngine.GetSocketShield().BroadcastResponse(new Response(441));

            BrickEngine.GetShopReactor().LoadPages();
            BrickEngine.GetShopReactor().LoadItems();

            BrickEngine.GetShopReactor().LoadShopClubItems();
            BrickEngine.GetShopReactor().LoadShopGiftItems();

            BrickEngine.GetShopReactor().LoadPetRaces();

            Client.Notif("Catalogue refreshed sucessfully.", false);
        }

        private void RefreshNavigator(Client Client, List<string> Params)
        {
            BrickEngine.GetNavigatorManager().LoadFeacturedRooms();
            BrickEngine.GetNavigatorManager().LoadPrivateCategorys();

            Client.Notif("Navigator refreshed sucessfully.", false);
        }

        private void RefreshItems(Client Client, List<string> Params)
        {
            BrickEngine.GetFurniReactor().Prepare();

            Client.Notif("Items refreshed sucessfully.", false);
        }

        private void MuteRoom(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to mute room " + GetParams("muteroom"), false);
                return;
            }

            int SecondsMuted = BrickEngine.GetConvertor().ObjectToInt32(Params[0]);

            if (SecondsMuted > 3600)
            {
                Client.Notif("Limit of 1 hour " + GetParams("muteroom"), false);
                return;
            }

            foreach (VirtualRoomUser User in Client.GetUser().GetRoom().GetRoomEngine().GetUsers())
            {
                if (BrickEngine.GetUserReactor().IsOnline(User.HabboId))
                {
                    if (User.GetClient().GetUser().Rank == 1 && !Client.GetUser().GetRoom().GetRoomEngine().HasRights(User.HabboId, RightsType.Rights))
                    {
                        Mute(Client, new string[] { User.GetClient().GetUser().Username, SecondsMuted.ToString() }.ToList());
                    }
                }
            }

            Client.Notif("Room muted fawlessly.", true);
        }

        private void UnMuteRoom(Client Client, List<string> Params)
        {
            foreach (VirtualRoomUser User in Client.GetUser().GetRoom().GetRoomEngine().GetUsers())
            {
                if (BrickEngine.GetUserReactor().IsOnline(User.HabboId))
                {
                    if (User.GetClient().GetUser().Rank == 1 && !Client.GetUser().GetRoom().GetRoomEngine().HasRights(User.HabboId, RightsType.Rights))
                    {
                        UnMute(Client, new string[] { User.GetClient().GetUser().Username }.ToList());
                    }
                }
            }

            Client.Notif("Room un-muted fawlessly.", true);
        }

        private void Mute(Client Client, List<string> Params)
        {
            if (Params.Count != 2)
            {
                Client.Notif("Failed to mute a user " + GetParams("mute"), false);
                return;
            }

            string Username = Params[0];

            int SecondsMuted = BrickEngine.GetConvertor().ObjectToInt32(Params[1]);

            if (SecondsMuted > 3600)
            {
                Client.Notif("Limit of 1 hour " + GetParams("mute"), false);
                return;
            }

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client TargetClient = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

                TargetClient.GetUser().GetClient().GetUser().MutedStart = DateTime.Now;
                TargetClient.GetUser().GetClient().GetUser().SecondsMuted = SecondsMuted;

                Response Mute = new Response(27);
                Mute.AppendInt32(SecondsMuted);
                TargetClient.SendResponse(Mute);

                TargetClient.Notif(string.Format("You've been muted by: {0}", Client.GetUser().Username), true);
            }
        }

        private void UnMute(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to mute a user " + GetParams("unmute"), false);
                return;
            }

            string Username = Params[0];

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client TargetClient = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

                if (TargetClient.GetUser().Muted)
                {
                    TargetClient.Notif("You've been unmuted, reload the room.", false);

                    TargetClient.GetUser().GetClient().GetUser().MutedStart = new DateTime(1, 1, 1);
                    TargetClient.GetUser().GetClient().GetUser().SecondsMuted = 0;

                    Response Mute = new Response(27);
                    Mute.AppendInt32(0);
                    TargetClient.SendResponse(Mute);
                }
            }
        }

        private void Summon(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to summon user " + GetParams("summon"), false);
                return;
            }

            string Username = Params[0];

            if (Username.ToLower().Equals(Client.GetUser().Username))
            {
                Client.Notif("You cannot summon yourself.", false);
                return;
            }

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (HabboId <= 0)
            {
                Client.Notif(string.Format("User {0} not found in database/cache.", Username), false);
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("User {0} is offline.", Username), false);
                return;
            }

            int CurrentRoomId = Client.GetUser().RoomId;

            if (CurrentRoomId <= 0)
            {
                Client.Notif("You have to be in a room.", false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            if (Target.GetUser().Rank > Client.GetUser().Rank)
            {
                Client.Notif("You cannot summon someone with a higher rank.", false);
                return;
            }

            if (!Target.GetUser().IsInRoom)
            {
                return;
            }

            Target.Notif(string.Format("You have been summoned by {0}.", Client.GetUser().Username), true);

            if (!Target.GetUser().RoomId.Equals(CurrentRoomId))
            {
                BrickEngine.GetPacketHandler().BeginLoadRoom(Target, CurrentRoomId, string.Empty);
            }
            else
            {
                VirtualRoomUser TargetUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);
                VirtualRoomUser MyUser = Client.GetUser().GetRoomUser();

                if (TargetUser != null && MyUser != null)
                {
                    TargetUser.UnhandledGoalPoint = MyUser.FontPoint;
                }
            }
        }

        private void IPBan(Client Client, List<string> Params)
        {
            if (Params.Count != 2)
            {
                Client.Notif("Failed to ip-ban a user " + GetParams("ipban"), false);
                return;
            }

            int HabboId = BrickEngine.GetUserReactor().GetId(Params[0]);

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("{0} is not in the hotel.", Params[0]), false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            string IP = Target.IPAddress;

            BrickEngine.GetToolReactor().BanUser(Client, HabboId, Params[1], 100000, true);
        }

        private void Effect(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to run effect " + GetParams("effect"), false);
                return;
            }

            int FilteredEffect = -1;

            string RawEffect = Params[0];

            if (!int.TryParse(RawEffect.Trim(), out FilteredEffect))
            {
                Client.Notif("Failed to run effect " + GetParams("effect"), false);
                return;
            }
            else if (FilteredEffect < 0)
            {
                Client.Notif("Failed to run effect " + GetParams("effect"), false);
                return;
            }

            BrickEngine.GetEffectsHandler().RunFreeEffect(Client, FilteredEffect);
        }

        private void PickAll(Client Client, List<string> Params)
        {
            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, RightsType.Founder))
            {
                return;
            }

            lock (Client.GetUser().GetRoom().GetRoomEngine().GetItems())
            {
                foreach (Item Item in Client.GetUser().GetRoom().GetRoomEngine().GetItems())
                {
                    int OldRotation = Item.Rotation;

                    iPoint OldPlace = Item.Point;
                    iPoint NewPlace = new iPoint(-1, -1, 0.0);

                    int NewRotation = 0;

                    Item.RoomId = 0;
                    Item.Rotation = NewRotation;
                    Item.Point = NewPlace;

                    Client.GetUser().GetRoom().GetRoomEngine().HandleIncomingItemPickUp(Item, OldPlace, NewPlace, OldRotation, NewRotation, Client.GetUser().GetRoomUser());

                    Client.SendResponse(new Response(101));
                }
            }
        }

        private void GiveCredits(Client Client, List<string> Params)
        {
            if (Params.Count != 2)
            {
                Client.Notif("Failed to give credits " + GetParams("givecredits"), false);
                return;
            }

            int FilteredCredits = -1;

            string RawCredits = Params[1];

            if (!int.TryParse(RawCredits.Trim(), out FilteredCredits))
            {
                Client.Notif("Failed to give credits " + GetParams("givecredits"), false);
                return;
            }
            else if (FilteredCredits <= 0)
            {
                Client.Notif("Failed to give credits " + GetParams("givecredits"), false);
                return;
            }

            string Username = Params[0];

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (HabboId <= 0)
            {
                Client.Notif("Failed to give credits " + GetParams("givecredits"), false);
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("User {0} is offline.", Username), false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            Target.GetUser().Credits += FilteredCredits;
            Target.GetUser().UpdateCredits(true);

            Target.Notif(string.Format("{0} gave you {1} credits.", Client.GetUser().Username, FilteredCredits), true);
        }

        private void BroadcastCredits(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to give credits " + GetParams("broadcastcredits"), false);
                return;
            }

            int FilteredCredits = -1;

            string RawCredits = Params[0];

            if (!int.TryParse(RawCredits.Trim(), out FilteredCredits))
            {
                Client.Notif("Failed to give credits " + GetParams("givecredits"), false);
                return;
            }
            else if (FilteredCredits <= 0)
            {
                Client.Notif("Failed to give credits " + GetParams("givecredits"), false);
                return;
            }

            int i = 0;

            foreach (SocketClient sClient in BrickEngine.GetSocketShield().Sessions)
            {
               GiveCredits(Client, new string[] { sClient.GetClient().GetUser().Username, FilteredCredits.ToString() }.ToList());
               i += FilteredCredits;
            }

            Client.Notif(string.Format("Broadcasted total {0} credits(s) away.", i), true);
        }

        private void GivePixels(Client Client, List<string> Params)
        {
            if (Params.Count != 2)
            {
                Client.Notif("Failed to give pixels " + GetParams("givepixels"), false);
                return;
            }

            int FilteredPixels = -1;

            string RawPixels = Params[1];

            if (!int.TryParse(RawPixels.Trim(), out FilteredPixels))
            {
                Client.Notif("Failed to give pixels " + GetParams("givepixels"), false);
                return;
            }
            else if (FilteredPixels <= 0)
            {
                Client.Notif("Failed to give pixels " + GetParams("givepixels"), false);
                return;
            }

            string Username = Params[0];

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (HabboId <= 0)
            {
                Client.Notif("Failed to give pixels " + GetParams("givepixels"), false);
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("User {0} is offline.", Username), false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            Target.GetUser().Pixels += FilteredPixels;
            Target.GetUser().UpdatePixels(true);

            Target.Notif(string.Format("{0} gave you {1} pixels.", Client.GetUser().Username, FilteredPixels), true);
        }

        private void BroadcastPixels(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to give pixels " + GetParams("broadcastpixels"), false);
                return;
            }

            int FilteredPixels = -1;

            string RawPixels = Params[0];

            if (!int.TryParse(RawPixels.Trim(), out FilteredPixels))
            {
                Client.Notif("Failed to give pixels " + GetParams("givepixels"), false);
                return;
            }
            else if (FilteredPixels <= 0)
            {
                Client.Notif("Failed to give pixels " + GetParams("givepixels"), false);
                return;
            }

            int i = 0;

            foreach (SocketClient sClient in BrickEngine.GetSocketShield().Sessions)
            {
                GivePixels(Client, new string[] { sClient.GetClient().GetUser().Username, FilteredPixels.ToString() }.ToList());
                i += FilteredPixels;
            }

            Client.Notif(string.Format("Broadcasted total {0} pixel(s) away.", i), true);
        }

        private void GiveBadge(Client Client, List<string> Params)
        {
            if (Params.Count != 2)
            {
                Client.Notif("Failed to give badge " + GetParams("givebadge"), false);
                return;
            }

            string Username = Params[0];

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (HabboId <= 0)
            {
                Client.Notif("Failed to give badge " + GetParams("givebadge"), false);
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("User {0} is offline.", Username), false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            string BadgeCode = Params[1];

            BrickEngine.GetBadgeHandler().GiveBadge(Target, BadgeCode);
            BrickEngine.GetPacketHandler().GetBadges(Target, null);

            Target.Notif(string.Format("{0} gave you a badge: {1}", Client.GetUser().Username, BadgeCode), true);
        }

        private void BroadcastBadge(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to broadcast badge " + GetParams("broadcastbadge"), false);
                return;
            }

            int i = 0;

            string BadgeCode = Params[0];

            foreach (SocketClient sClient in BrickEngine.GetSocketShield().Sessions)
            {
                GiveBadge(Client, new string[] { sClient.GetClient().GetUser().Username,  BadgeCode }.ToList());
                i++;
            }

            Client.Notif(string.Format("Broadcasted total {0} badge(s) away.", i), true);
        }

        private void SingPets(Client Client, List<string> Params)
        {
            if (Params.Count == 0)
            {
                Client.Notif("Failed to sing pets " + GetParams("singpets"), false);
                return;
            }

            StringBuilder Message = new StringBuilder();

            int i = 0;

            foreach (string Param in Params)
            {
                if (i > 0 && i < Params.Count)
                {
                    Message.Append(' ');
                }

                Message.Append(Param);

                i++;
            }

            foreach (VirtualRoomUser Pet in Client.GetUser().GetRoom().GetRoomEngine().GetPets())
            {
                Pet.Talk(Message.ToString(), SpeechType.Shout, 0, string.Empty);

                Pet.AddStatus("dan", string.Empty);
                Pet.UpdateStatus(true);
            }
        }

        private void Teleport(Client Client, List<string> Params)
        {
            if (!Client.GetUser().GetRoomUser().TeleportingEnabled)
            {
                Client.GetUser().GetRoomUser().TeleportingEnabled = true;
            }
            else
            {
                Client.GetUser().GetRoomUser().TeleportingEnabled = false;
            }

            Client.Notif(string.Format("Teleporting is setted {0}", (Client.GetUser().GetRoomUser().TeleportingEnabled) ? "enabled" : "disabled"), false);
        }

        private void WhosOnline(Client Client, List<string> Params)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine(string.Format("[{0}] Users that are in the hotel.", BrickEngine.GetSocketShield().Sessions.Count));

            foreach (SocketClient sClient in BrickEngine.GetSocketShield().Sessions)
            {
                Builder.Append(string.Format("[{0}] {1}\r", sClient.GetClient().GetUser().Rank, sClient.GetClient().GetUser().Username));
            }

            Client.LongNotif(Builder);
        }

        private void WhosInRoom(Client Client, List<string> Params)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine(string.Format("[{0}] Users that are in the hotel.", BrickEngine.GetSocketShield().Sessions.Count));

            foreach (SocketClient sClient in BrickEngine.GetSocketShield().Sessions)
            {
                if (sClient.GetClient().GetUser().EnableShowOnline || Client.GetUser().Rank >= 7)
                {
                    if (sClient.GetClient().GetUser().IsInRoom && (sClient.GetClient().GetUser().EnableFollow || Client.GetUser().Rank >= 7))
                    {
                        Builder.Append(string.Format("[{0}] {1}\r", sClient.GetClient().GetUser().Rank, sClient.GetClient().GetUser().Username));
                    }
                }
            }

            Client.LongNotif(Builder);
        }

        private void WhosHotelView(Client Client, List<string> Params)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine(string.Format("[{0}] Users that are in the hotel.", BrickEngine.GetSocketShield().Sessions.Count));

            foreach (SocketClient sClient in BrickEngine.GetSocketShield().Sessions)
            {
                if (sClient.GetClient().GetUser().EnableShowOnline || Client.GetUser().Rank >= 7)
                {
                    if (!sClient.GetClient().GetUser().IsInRoom && (!sClient.GetClient().GetUser().EnableFollow || Client.GetUser().Rank >= 7))
                    {
                        Builder.Append(string.Format(" [{0}] {1}\r", sClient.GetClient().GetUser().Rank, sClient.GetClient().GetUser().Username));
                    }
                }
            }

            Client.LongNotif(Builder);
        }

        private void DropEvents(Client Client, List<string> Params)
        {
            int i = 0;

            foreach (VirtualRoom Room in BrickEngine.GetRoomReactor().GetEventRooms(-1))
            {
                Room.Event.Drop();
                i++;
            }

            Client.Notif(string.Format("Dropped {0} event(s).", i), true);
        }

        private void ClearMotto(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to clear motto " + GetParams("clearmotto"), false);
                return;
            }

            string Username = Params[0];

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (HabboId <= 0)
            {
                Client.Notif("Failed to clear motto " + GetParams("clearmotto"), false);
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("User {0} is offline.", Username), false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            Target.GetUser().Motto = string.Empty;

            Target.GetUser().RefreshUser();

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET motto = @motto WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("motto", string.Empty);
                Reactor.AddParam("habboid", HabboId);
                Reactor.ExcuteQuery();
            }

            BrickEngine.GetStreamHandler().AddStream(Target.GetUser().HabboId, Users.Handlers.Messenger.Streaming.StreamType.EditedMotto, string.Empty);

            Client.Notif(string.Format("Cleared motto of {0}", Username), true);
        }

        private void DisconnectUser(Client Client, List<string> Params)
        {
            if (Params.Count != 1)
            {
                Client.Notif("Failed to disconnect user " + GetParams("disconnect"), false);
                return;
            }

            string Username = Params[0];

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (HabboId <= 0)
            {
                Client.Notif("Failed to disconnect user " + GetParams("disconnect"), false);
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(HabboId))
            {
                Client.Notif(string.Format("User {0} is offline.", Username), false);
                return;
            }

            Client Target = BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();

            Target.Dispose();

            Client.Notif(string.Format("Disconnected {0} fawlessly.", Username), true);
        }

        private void Empty(Client Client, List<string> Params)
        {
            foreach (PetInfo Item in BrickEngine.GetPetReactor().GetPetsForInventory(Client.GetUser().HabboId))
            {
                BrickEngine.GetPetReactor().RemovePet(Item.Id);
            }

            foreach (Item Item in BrickEngine.GetItemReactor().GetItemsForUser(Client.GetUser().HabboId))
            {
                BrickEngine.GetItemReactor().RemoveItem(Item.Id);
            }

            Client.SendResponse(new Response(101));
            Client.Notif("Inventory Emptied.", true);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM items WHERE user_id = @habboid AND room_id <= 0");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM user_pets WHERE user_id = @habboid AND room_id <= 0");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void UnloadRoom(Client Client, List<string> Params)
        {
            int RoomId = Client.GetUser().RoomId;

            if (Params.Count == 1)
            {
                int.TryParse(Params[0], out RoomId);
            }

            BrickEngine.GetRoomReactor().DisposeRoom(RoomId);

            Client.Notif(string.Format("Disposed room {0} fawlessly.", RoomId), false);
        }
        #endregion
    }
}
