using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Messages;
using BrickEmulator.Storage;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void GetMessenger(Client Client, Request Request)
        {
            GetFriends(Client, null);
            GetRequests(Client, null);
        }

        private void GetFriends(Client Client, Request Request)
        {
            var Friends = BrickEngine.GetMessengerHandler().GetFriends(Client.GetUser().HabboId);

            Response Response = new Response(12);
            Response.AppendInt32(BrickEngine.GetMessengerHandler().GetMaxFriends(Client));
            Response.AppendInt32(BrickEngine.GetMessengerHandler().MAX_FRIENDS_DEFAULT);
            Response.AppendInt32(BrickEngine.GetMessengerHandler().MAX_FRIENDS_BASIC);
            Response.AppendInt32(BrickEngine.GetMessengerHandler().MAX_FRIENDS_VIP);

            var Groups = BrickEngine.GetMessengerHandler().GetGroups(Client.GetUser().HabboId);

            Response.AppendInt32(Groups.Count);

            foreach (BrickEmulator.HabboHotel.Users.Handlers.Messenger.Groups.FriendGroup Group in Groups)
            {
                Response.AppendInt32(Group.Id);
                Response.AppendStringWithBreak(Group.Name);
            }

            Response.AppendInt32(Friends.Count);

            foreach (BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend Friend in Friends)
            {
                int CategoryId = BrickEngine.GetMessengerHandler().GetCategoryForFriendId(Client.GetUser().HabboId, Friend.HabboId);

                Friend.GetSerializeResponse(CategoryId, Response);
            }

            Response.AppendInt32(BrickEngine.GetMessengerHandler().MAX_FRIENDS_PER_PAGE);

            Client.SendResponse(Response);
        }

        private void GetRequests(Client Client, Request Request)
        {
            var Requests = BrickEngine.GetMessengerHandler().GetRequests(Client.GetUser().HabboId);

            Response Response = new Response(314);
            Response.AppendInt32(Requests.Count);
            Response.AppendInt32(Requests.Count);

            foreach (Users.Handlers.Messenger.Request UserRequest in Requests)
            {
                UserRequest.GetResponse(Response);
            }

            Client.SendResponse(Response);
        }

        private void RequestUser(Client Client, Request Request)
        {
            string Username = Request.PopFixedString();

            int HabboId = BrickEngine.GetUserReactor().GetId(Username);

            if (!BrickEngine.GetUserReactor().GetEnableNewFriends(HabboId))
            {
                Response Response = new Response(260);
                Response.AppendInt32(39);
                Response.AppendInt32(3);
                Client.SendResponse(Response);
                return;
            }

            if (HabboId > 0)
            {
                BrickEngine.GetMessengerHandler().RequestUser(Client.GetUser().HabboId, HabboId);
            }
        }

        private void AcceptRequests(Client Client, Request Request)
        {
            int Amount = Request.PopWiredInt32();

            for (int i = 0; i < Amount; i++)
            {
                int UserId = Request.PopWiredInt32();

                BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend Friend = new BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend(UserId);

                if (Friend.IsAlive)
                {
                    // Send Response (come online) >> Friend
                    Friend.GetClient().SendResponse(BrickEngine.GetMessengerHandler().GetStatusMessage(Friend.HabboId, Client.GetUser(), true));

                    // Send Response (come online) >> MySELF
                    Client.SendResponse(BrickEngine.GetMessengerHandler().GetStatusMessage(Client.GetUser().HabboId, Friend.GetClient().GetUser(), true));
                }

                BrickEngine.GetMessengerHandler().AcceptRequest(UserId, Client.GetUser().HabboId);
            }
        }

        private void DenyRequests(Client Client, Request Request)
        {
            // Avoid Junk
            Request.PopWiredInt32();

            int Amount = Request.PopWiredInt32();

            for (int i = 0; i < Amount; i++)
            {
                int UserId = Request.PopWiredInt32();

                BrickEngine.GetMessengerHandler().DenyRequest(UserId, Client.GetUser().HabboId);
            }
        }

        private void SearchUsers(Client Client, Request Request)
        {
            string Query = BrickEngine.CleanString(Request.PopFixedString());

            Client.SendResponse(BrickEngine.GetMessengerHandler().SearchUsers(Client.GetUser().HabboId, Query));
        }

        private void DeleteFriends(Client Client, Request Request)
        {
            int FriendAmount = Request.PopWiredInt32();

            for (int i = 0; i < FriendAmount; i++)
            {
                int FriendId = Request.PopWiredInt32();

                if (!BrickEngine.GetMessengerHandler().HasFriend(Client.GetUser().HabboId, FriendId))
                {
                    continue;
                }

                BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend Friend = BrickEngine.GetMessengerHandler().GetFriend(Client.GetUser().HabboId, FriendId);

                if (Friend == null)
                {
                    continue;
                }

                if (Friend.IsAlive)
                {
                    Response Response = new Response(13);
                    Response.AppendBoolean(false);
                    Response.AppendBoolean(true);
                    Response.AppendInt32(-1);
                    Response.AppendInt32(Client.GetUser().HabboId);

                    Friend.GetClient().SendResponse(Response);
                }

                Response MyResponse = new Response(13);
                MyResponse.AppendBoolean(false);
                MyResponse.AppendBoolean(true);
                MyResponse.AppendInt32(-1);
                MyResponse.AppendInt32(FriendId);
                Client.SendResponse(MyResponse);

                BrickEngine.GetMessengerHandler().DeleteFriend(Client.GetUser().HabboId, FriendId);
            }
        }

        private void ChatWithFriend(Client Client, Request Request)
        {
            int FriendId = Request.PopWiredInt32();

            if (!BrickEngine.GetMessengerHandler().HasFriend(Client.GetUser().HabboId, FriendId))
            {
                return;
            }

            BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend Friend = BrickEngine.GetMessengerHandler().GetFriend(Client.GetUser().HabboId, FriendId);

            if (Friend == null)
            {
                Response Respose = new Response(261);
                Respose.AppendInt32(6);
                Respose.AppendInt32(FriendId);
                Client.SendResponse(Respose);
                return;
            }

            if (Client.GetUser().Muted)
            {
                Response Respose = new Response(261);
                Respose.AppendInt32(4);
                Respose.AppendInt32(FriendId);
                Client.SendResponse(Respose);
                return;
            }

            if (Friend.IsAlive)
            {
                if (Friend.GetClient().GetUser().Muted)
                {
                    Response Respose = new Response(261);
                    Respose.AppendInt32(3);
                    Respose.AppendInt32(FriendId);
                    Client.SendResponse(Respose);
                }

                Response Response = new Response(134);
                Response.AppendInt32(Client.GetUser().HabboId);
                Response.AppendStringWithBreak(BrickEngine.CleanString(Request.PopFixedString()));

                Friend.GetClient().SendResponse(Response);
            }
            else
            {
                Response Respose = new Response(261);
                Respose.AppendInt32(5);
                Respose.AppendInt32(FriendId);
                Client.SendResponse(Respose);
            }
        }

        private void InviteFriends(Client Client, Request Request)
        {
            int FriendAmount = Request.PopWiredInt32();

            var FriendIds = new List<int>();

            for (int i = 0; i < FriendAmount; i++)
            {
                int FriendId = Request.PopWiredInt32();

                FriendIds.Add(FriendId);
            }

            foreach (int FriendId in FriendIds)
            {
                if (!BrickEngine.GetMessengerHandler().HasFriend(Client.GetUser().HabboId, FriendId))
                {
                    continue;
                }

                BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend Friend = BrickEngine.GetMessengerHandler().GetFriend(Client.GetUser().HabboId, FriendId);

                if (Friend == null)
                {
                    continue;
                }

                if (Friend.IsAlive)
                {
                    Response Response = new Response(135);
                    Response.AppendInt32(Client.GetUser().HabboId);
                    Response.AppendStringWithBreak(BrickEngine.CleanString(Request.PopFixedString()));

                    Friend.GetClient().SendResponse(Response);
                }
            }
        }

        private void FollowBuddy(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int FriendId = Request.PopWiredInt32();

            if (!BrickEngine.GetMessengerHandler().HasFriend(Client.GetUser().HabboId, FriendId))
            {
                return;
            }

            if (!BrickEngine.GetUserReactor().IsOnline(FriendId))
            {
                return;
            }

            Client TriggeredClient = BrickEngine.GetSocketShield().GetSocketClientByHabboId(FriendId).GetClient();

            if (!TriggeredClient.GetUser().IsInRoom)
            {
                return;
            }

            if (!TriggeredClient.GetUser().EnableFollow)
            {
                return;
            }

            Response Response = new Response(286);
            Response.AppendBoolean(false);
            Response.AppendInt32(TriggeredClient.GetUser().RoomId);
            Client.SendResponse(Response);

            BeginLoadRoom(Client, TriggeredClient.GetUser().RoomId, "");
        }

        private void UpdateFriendStreaming(Client Client, Request Request)
        {
            Boolean EnabledFriendStream = (Request.PlainReadBytes(1)[0].ToString() == "65");
            Request.AdvancePointer(1);

            if (Client.GetUser().EnabledFriendStream != EnabledFriendStream)
            {
                Client.GetUser().EnabledFriendStream = EnabledFriendStream;

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE users SET activate_friendstream = @activate_friendstream WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("activate_friendstream", EnabledFriendStream ? 1 : 0);
                    Reactor.AddParam("habboid", Client.GetUser().HabboId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        private void GetFriendStreams(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetStreamHandler().GetResponse(Client));
        }
    }
}
