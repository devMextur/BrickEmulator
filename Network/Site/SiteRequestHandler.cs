using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.Messages;
using System.Text.RegularExpressions;

namespace BrickEmulator.Network.Site
{
    class SiteRequestHandler
    {
        #region Fields
        private Dictionary<int, Interaction> Interactions;
        private List<int> MayOffline;

        private delegate void Interaction(Client Client, SiteRequest Request);
        #endregion

        #region Constructors
        public SiteRequestHandler()
        {
            Register();
        }
        #endregion

        #region Methods
        private void Register()
        {
            Interactions = new Dictionary<int, Interaction>();
            MayOffline = new List<int>();

            MayOffline.Add(9);
            MayOffline.Add(10);
            MayOffline.Add(11);
            MayOffline.Add(12);
            MayOffline.Add(13);
            MayOffline.Add(15);
            MayOffline.Add(16);

            Interactions[0] = new Interaction(UpdateMotto);
            Interactions[1] = new Interaction(UpdateEnableFriends);
            Interactions[2] = new Interaction(UpdateEnableWordFilter);
            Interactions[3] = new Interaction(UpdateEnableShowOnline);
            Interactions[4] = new Interaction(UpdateEnableFollow);
            Interactions[5] = new Interaction(SignoutUser);
            Interactions[6] = new Interaction(AddUserTag);
            Interactions[7] = new Interaction(RemoveUserTag);
            Interactions[8] = new Interaction(UpdateEnableTrade);
            Interactions[9] = new Interaction(AddFriend);
            Interactions[10] = new Interaction(DeleteFriend);
            Interactions[11] = new Interaction(CreateUserGroup);
            Interactions[12] = new Interaction(EditUserGroup);
            Interactions[13] = new Interaction(DeleteUserGroup);
            Interactions[14] = new Interaction(AddUserToGroup);
            Interactions[15] = new Interaction(RemoveUserFromGroup);
            Interactions[16] = new Interaction(UpdateCredits);
        }

        public Boolean NeedsOnline(int RequestId)
        {
            return !MayOffline.Contains(RequestId);
        }

        public void HandleRequest(SiteRequest Request)
        {
            if (!Interactions.ContainsKey(Request.GetHeader()))
            {
                return;
            }

            if (NeedsOnline(Request.GetHeader()))
            {
                if (GainUserClient(Request) == null)
                {
                    return;
                }
            }

            Interactions[Request.GetHeader()].Invoke(GainUserClient(Request), Request);
        }

        private Client GainUserClient(SiteRequest Request)
        {
            if (!BrickEngine.GetUserReactor().IsOnline(Request.GetUserId()))
            {
                return null;
            }

            SocketClient Client = BrickEngine.GetSocketShield().GetSocketClientByHabboId(Request.GetUserId());

            if (Client == null || !Client.GetClient().IsValidUser)
            {
                return null;
            }

            return Client.GetClient();
        }
        #endregion

        #region Interactions
        private void UpdateMotto(Client Client, SiteRequest Request)
        {
            string Motto = BrickEngine.CleanString(Request.PopStringToEnd());

            if (Client.GetUser().Motto.Equals(Motto))
            {
                return;
            }

            Client.GetUser().Motto = Motto;
            Client.GetUser().RefreshUser();

            BrickEngine.GetStreamHandler().AddStream(Request.GetUserId(), HabboHotel.Users.Handlers.Messenger.Streaming.StreamType.EditedMotto, Motto);
        }

        private void UpdateEnableFriends(Client Client, SiteRequest Request)
        {
            Boolean EnableFriends = Request.PopBoolean();

            if (Client.GetUser().EnableNewFriends.Equals(EnableFriends))
            {
                return;
            }

            Client.GetUser().EnableNewFriends = EnableFriends;
        }

        private void UpdateEnableWordFilter(Client Client, SiteRequest Request)
        {
            Boolean EnableWordFilter = Request.PopBoolean();

            if (Client.GetUser().EnableWordfilter.Equals(EnableWordFilter))
            {
                return;
            }

            Client.GetUser().EnableWordfilter = EnableWordFilter;
        }

        private void UpdateEnableShowOnline(Client Client, SiteRequest Request)
        {
            Boolean EnableShowOnline = Request.PopBoolean();

            if (Client.GetUser().EnableShowOnline.Equals(EnableShowOnline))
            {
                return;
            }

            if (EnableShowOnline)
            {
                lock (Client.GetUser().MessengerLocker)
                {
                    BrickEngine.GetMessengerHandler().AlertStatusFriends(Client.GetUser(), true);
                }
            }
            else
            {
                lock (Client.GetUser().MessengerLocker)
                {
                    BrickEngine.GetMessengerHandler().AlertStatusFriends(Client.GetUser(), false);
                }
            }

            Client.GetUser().EnableShowOnline = EnableShowOnline;
        }

        private void UpdateEnableFollow(Client Client, SiteRequest Request)
        {
            Boolean EnableFollow = Request.PopBoolean();

            if (Client.GetUser().EnableFollow.Equals(EnableFollow))
            {
                return;
            }

            Client.GetUser().EnableFollow = EnableFollow;

            lock (Client.GetUser().MessengerLocker)
            {
                BrickEngine.GetMessengerHandler().AlertStatusFriends(Client.GetUser(), true);
            }
        }

        private void UpdateEnableTrade(Client Client, SiteRequest Request)
        {
            Boolean EnableTrade = Request.PopBoolean();

            if (Client.GetUser().EnableTrade.Equals(EnableTrade))
            {
                return;
            }

            Client.GetUser().EnableTrade = EnableTrade;
        }

        private void AddFriend(Client Client, SiteRequest Request)
        {
            int FriendId = Request.PopInt32();

            if (FriendId <= 0 || FriendId == Request.GetUserId())
            {
                return;
            }

            BrickEngine.GetMessengerHandler().RequestUser(Request.GetUserId(), FriendId);
        }

        private void DeleteFriend(Client Client, SiteRequest Request)
        {
            int FriendId = Request.PopInt32();

            if (FriendId <= 0 || FriendId == Request.GetUserId())
            {
                return;
            }

            if (!BrickEngine.GetMessengerHandler().HasFriend(Request.GetUserId(), FriendId))
            {
                return;
            }

            BrickEmulator.HabboHotel.Users.Handlers.Messenger.Friend Friend = BrickEngine.GetMessengerHandler().GetFriend(Request.GetUserId(), FriendId);

            if (Friend == null)
            {
                return;
            }

            if (Friend.IsAlive)
            {
                Response Response = new Response(13);
                Response.AppendBoolean(false);
                Response.AppendBoolean(true);
                Response.AppendInt32(-1);
                Response.AppendInt32(Request.GetUserId());

                Friend.GetClient().SendResponse(Response);
            }

            if (Client != null)
            {
                Response MyResponse = new Response(13);
                MyResponse.AppendBoolean(false);
                MyResponse.AppendBoolean(true);
                MyResponse.AppendInt32(-1);
                MyResponse.AppendInt32(FriendId);
                Client.SendResponse(MyResponse);
            }

            BrickEngine.GetMessengerHandler().DeleteFriend(Request.GetUserId(), FriendId);
        }

        private void CreateUserGroup(Client Client, SiteRequest Request)
        {
            int GroupId = Request.PopInt32();

            if (GroupId <= 0)
            {
                return;
            }

            string Name = BrickEngine.CleanString(Request.PopString());

            if (string.IsNullOrEmpty(Name))
            {
                return;
            }

            if (!Regex.IsMatch(Name, @"^[a-zA-Z]+$"))
            {
                return;
            }

            if (Name.Length > 32)
            {
                Name = Name.Substring(32);
            }

            BrickEngine.GetMessengerHandler().CreateGroup(GroupId, Request.GetUserId(), Name);
        }

        private void EditUserGroup(Client Client, SiteRequest Request)
        {
            int GroupId = Request.PopInt32();

            if (GroupId <= 0)
            {
                return;
            }

            string Name = BrickEngine.CleanString(Request.PopString());

            if (string.IsNullOrEmpty(Name))
            {
                return;
            }

            if (!Regex.IsMatch(Name, @"^[a-zA-Z]+$"))
            {
                return;
            }

            if (Name.Length > 32)
            {
                Name = Name.Substring(32);
            }

            BrickEngine.GetMessengerHandler().RenameGroup(GroupId, Request.GetUserId(), Name);
        }

        private void DeleteUserGroup(Client Client, SiteRequest Request)
        {
            int GroupId = Request.PopInt32();

            if (GroupId <= 0)
            {
                return;
            }

            BrickEngine.GetMessengerHandler().DeleteGroup(GroupId, Request.GetUserId());
        }

        private void AddUserToGroup(Client Client, SiteRequest Request)
        {
            int ItemId = Request.PopInt32();

            if (ItemId <= 0)
            {
                return;
            }

            int FriendId = Request.PopInt32();

            if (FriendId <= 0)
            {
                return;
            }

            int CategoryId = Request.PopInt32();

            if (CategoryId <= 0)
            {
                return;
            }

            BrickEngine.GetMessengerHandler().AddUserToGroup(ItemId, Request.GetUserId(), FriendId, CategoryId);
        }

        private void RemoveUserFromGroup(Client Client, SiteRequest Request)
        {
            int ItemId = Request.PopInt32();

            if (ItemId <= 0)
            {
                return;
            }

            BrickEngine.GetMessengerHandler().RemoveUserFromGroup(ItemId, Request.GetUserId());
        }

        private void UpdateCredits(Client Client, SiteRequest Request)
        {
            int Credits = Request.PopInt32();

            if (Credits <= 0)
            {
                Credits = 0;
            }

            if (Client.GetUser().Credits.Equals(Credits))
            {
                return;
            }

            Client.GetUser().Credits = Credits;
            Client.GetUser().UpdateCredits(false);
        }

        private void SignoutUser(Client Client, SiteRequest Request)
        {
            Client.Dispose();
        }

        private void AddUserTag(Client Client, SiteRequest Request)
        {
            string Tag = Request.PopString();

            if (!Client.GetUser().Tags.Contains(Tag))
            {
                Client.GetUser().Tags.Add(Tag);
            }
        }

        private void RemoveUserTag(Client Client, SiteRequest Request)
        {
            string Tag = Request.PopString();

            if (Client.GetUser().Tags.Contains(Tag))
            {
                Client.GetUser().Tags.Remove(Tag);
            }
        }
        #endregion
    }
}
