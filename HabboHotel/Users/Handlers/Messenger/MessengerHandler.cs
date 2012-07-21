using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Users.Handlers.Membership;
using BrickEmulator.Messages;
using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Users.Handlers.Messenger.Groups;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger
{
    class MessengerHandler
    {
        public readonly int MAX_FRIENDS_DEFAULT;
        public readonly int MAX_FRIENDS_BASIC;
        public readonly int MAX_FRIENDS_VIP;
        public readonly int MAX_FRIENDS_PER_PAGE;

        private List<Friendship> Friendships;

        private Dictionary<int, FriendGroup> FriendGroups;
        private Dictionary<int, FriendGroupItem> FriendGroupItems;
             
        public MessengerHandler()
        {
            MAX_FRIENDS_DEFAULT = BrickEngine.GetConfigureFile().CallIntKey("max.friends.default");
            MAX_FRIENDS_BASIC = BrickEngine.GetConfigureFile().CallIntKey("max.friends.basic");
            MAX_FRIENDS_VIP = BrickEngine.GetConfigureFile().CallIntKey("max.friends.vip");
            MAX_FRIENDS_PER_PAGE = BrickEngine.GetConfigureFile().CallIntKey("max.friends.vip");

            LoadFriendShips();
            LoadFriendGroups();
            LoadFriendGroupItems();
        }

        public void LoadFriendShips()
        {
            Friendships = new List<Friendship>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_friendships");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    Friendship Friendship = new Friendship(Row);

                    Friendships.Add(Friendship);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Friendships.Count + "] Friendship(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadFriendGroups()
        {
            FriendGroups = new Dictionary<int, FriendGroup>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_friends_groups");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    FriendGroup Group = new FriendGroup(Row);

                    FriendGroups.Add(Group.Id, Group);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + FriendGroups.Count + "] FriendGroup(s) cached.", IO.WriteType.Outgoing);
        }

        public FriendGroup GetGroup(int Id)
        {
            try { return FriendGroups[Id]; }
            catch { return null; }
        }

        public void LoadFriendGroupItems()
        {
            FriendGroupItems = new Dictionary<int, FriendGroupItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_friends_groups_items");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    FriendGroupItem Item = new FriendGroupItem(Row);

                    FriendGroupItems.Add(Item.Id, Item);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + FriendGroupItems.Count + "] FriendGroupItem(s) cached.", IO.WriteType.Outgoing);
        }

        public int GetMaxFriends(Client Client)
        {
            Membership.Membership Membership = Client.GetUser().GetMembership();

            if (Membership != null)
            {
                if (Membership.MemberScale == 0)
                {
                    return MAX_FRIENDS_BASIC;
                }
                else if (Membership.MemberScale == 1)
                {
                    return MAX_FRIENDS_VIP;
                }
            }

            return MAX_FRIENDS_DEFAULT;
        }

        public List<FriendGroup> GetGroups(int HabboId)
        {
            var List = new List<FriendGroup>();

            foreach (FriendGroup Group in FriendGroups.Values)
            {
                if (Group.UserId == HabboId)
                {
                    List.Add(Group);
                }
            }

            return List;
        }

        public int GetCategoryForFriendId(int UserId, int FriendId)
        {
            foreach (FriendGroupItem Item in FriendGroupItems.Values)
            {
                if (Item.UserId == UserId)
                {
                    if (Item.FriendId == FriendId)
                    {
                        return Item.CategoryId;
                    }
                }
            }

            return 0;
        }

        public List<Request> GetRequests(int HabboId)
        {
            var List = new List<Request>();

            foreach (Friendship Friendship in Friendships)
            {
                if (!Friendship.Pending)
                {
                    continue;
                }

                if (Friendship.FriendId == HabboId)
                {
                    List.Add(new Request(Friendship.UserId));
                }
            }

            return List;
        }

        public List<Friend> GetFriends(int HabboId)
        {
            var List = new List<Friend>();

            foreach (Friendship Friendship in Friendships)
            {
                if (Friendship.Pending)
                {
                    continue;
                }

                if (Friendship.UserId == HabboId)
                {
                    List.Add(new Friend(Friendship.FriendId));
                }
                else if (Friendship.FriendId == HabboId)
                {
                    List.Add(new Friend(Friendship.UserId));
                }
            }

            return List;
        }

        public List<Friend> GetOnlineFriends(int HabboId)
        {
            var List = new List<Friend>();

            foreach (Friend Friend in GetFriends(HabboId))
            {
                if (Friend.IsAlive)
                {
                    List.Add(Friend);
                }
            }

            return List;
        }

        public Friendship GetFriendship(int UserId, int FriendId)
        {
            foreach (Friendship Friendship in Friendships)
            {
                if (Friendship.UserId == UserId && Friendship.FriendId == FriendId)
                {
                    return Friendship;
                }
            }

            return null;
        }

        public Friend GetFriend(int UserId, int FriendId)
        {
            var Friends = GetFriends(UserId);

            foreach (Friend Friend in Friends)
            {
                if (Friend.HabboId == FriendId)
                {
                    return Friend;
                }
            }

            return null;
        }

        public Response GetStatusMessage(int TargetId, UserCache Cache, Boolean CameOnline)
        {
            Response Response = new Response(13);

            var Groups = GetGroups(TargetId);

            Response.AppendInt32(Groups.Count);

            foreach (FriendGroup Group in Groups)
            {
                Response.AppendInt32(Group.Id);
                Response.AppendStringWithBreak(Group.Name);
            }

            Response.AppendInt32(1);
            Response.AppendBoolean(false);

            Response.AppendInt32(Cache.HabboId);
            Response.AppendStringWithBreak(Cache.Username);
            Response.AppendBoolean(true);
            Response.AppendBoolean(CameOnline);
            Response.AppendBoolean(CameOnline ? Cache.IsInRoom && Cache.EnableFollow : false);
            Response.AppendStringWithBreak(CameOnline ? Cache.Look : string.Empty);
            Response.AppendInt32(GetCategoryForFriendId(TargetId, Cache.HabboId));
            Response.AppendStringWithBreak(CameOnline ? BrickEngine.GetUserReactor().GetMotto(Cache.HabboId) : string.Empty);
            Response.AppendStringWithBreak(CameOnline ? string.Empty : BrickEngine.GetUserReactor().GetLastVisit(Cache.HabboId));
            Response.AppendChar(2);
            Response.AppendChar(2);

            return Response;
        }

        public void GetSearchForHabboId(int HabboId, Response Response, int SearcherId)
        {
            if (HasFriend(SearcherId, HabboId))
            {
                GetFriend(SearcherId, HabboId).GetSearchResponse(Response);
            }
            else
            {
                Boolean Online = BrickEngine.GetUserReactor().IsOnline(HabboId);

                Response.AppendInt32(HabboId);
                Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(HabboId));
                Response.AppendStringWithBreak(Online ? BrickEngine.GetUserReactor().GetMotto(HabboId) : string.Empty);
                Response.AppendBoolean(Online ? BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient().GetUser().IsInRoom : false);
                Response.AppendBoolean(Online);
                Response.AppendChar(2);
                Response.AppendBoolean(false);
                Response.AppendStringWithBreak(Online ? BrickEngine.GetUserReactor().GetLook(HabboId) : string.Empty);
                Response.AppendStringWithBreak(Online ? string.Empty : BrickEngine.GetUserReactor().GetLastVisit(HabboId));
                Response.AppendChar(2);
            }
        }

        public Response SearchUsers(int HabboId, string Query)
        {
            DataTable Table = null;

            var Friends = new List<int>();
            var Annonymous = new List<int>();

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT id FROM users WHERE username LIKE @query ORDER by username ASC");
                Reactor.AddParam("query", "%" + Query + "%");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    int UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);

                    if (HasFriend(HabboId,UserId))
                    {
                        Friends.Add(UserId);
                    }
                    else
                    {
                        Annonymous.Add(UserId);
                    }
                }
            }

            Response Response = new Response(435);

            Response.AppendInt32(Friends.Count);

            foreach (int UserId in Friends)
            {
                GetSearchForHabboId(UserId, Response, HabboId);
            }

            Response.AppendInt32(Annonymous.Count);

            foreach (int UserId in Annonymous)
            {
                GetSearchForHabboId(UserId, Response, HabboId);
            }

            return Response;
        }

        public Boolean HasFriend(int UserId, int FriendId)
        {
            return GetFriend(UserId, FriendId) != null || GetFriend(FriendId, UserId) != null;
        }

        public Boolean HasRequest(int UserId, int FriendId)
        {
            return (GetRequests(UserId).Contains(new Request(FriendId)) || GetRequests(FriendId).Contains(new Request(UserId)));
        }

        public Response GetAchievedResponse(int HabboId, Boolean Achievement, string Info)
        {
            Response Response = new Response(833);
            Response.AppendRawInt32(HabboId);
            Response.AppendChar(2);
            Response.AppendBoolean(Achievement);
            Response.AppendStringWithBreak(Info);
            return Response;
        }

        public void AlertFriends(int HabboId, Response Response)
        {
            var Friends = GetFriends(HabboId);

            foreach (Friend Friend in GetOnlineFriends(HabboId))
            {
                Friend.GetClient().SendResponse(Response);
            }
        }

        public void AlertStatusFriends(UserCache Cache, Boolean CameOnline)
        {
            var Friends = GetFriends(Cache.HabboId);

            foreach (Friend Friend in GetOnlineFriends(Cache.HabboId))
            {
                Response Response = GetStatusMessage(Friend.HabboId, Cache, CameOnline);

                Friend.GetClient().SendResponse(Response);
            }
        }

        public void RequestUser(int UserId, int FriendId)
        {
            if (HasFriend(UserId, FriendId) || HasRequest(UserId, FriendId))
            {
                return;
            }

            Dictionary<int, Object> Row = new Dictionary<int,object>();

            Row[1] = UserId;
            Row[2] = FriendId;
            Row[3] = true;

            Friendships.Add(new Friendship(Row));

            if (BrickEngine.GetUserReactor().IsOnline(FriendId))
            {
                Response Response = new Response(132);
                new Request(UserId).GetResponse(Response);

                BrickEngine.GetSocketShield().GetSocketClientByHabboId(FriendId).GetClient().SendResponse(Response);
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO user_friendships (user_id, friend_id) VALUES (@userid, @friendid)");
                Reactor.AddParam("userid", UserId);
                Reactor.AddParam("friendid", FriendId);
                Reactor.ExcuteQuery();
            }
        }

        public Boolean AcceptRequest(int UserId, int FriendId)
        {
            Friendship Friendship = GetFriendship(UserId, FriendId);

            if (Friendship == null)
            {
                return false;
            }

            if (!Friendship.Pending)
            {
                return false;
            }

            if (HasFriend(UserId, FriendId) || HasFriend(FriendId, UserId))
            {
                return false;
            }

            Friendship.Pending = false;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_friendships SET pending = '0' WHERE user_id = @userid AND friend_id = @friendid LIMIT 1");
                Reactor.AddParam("userid", UserId);
                Reactor.AddParam("friendid", FriendId);
                Reactor.ExcuteQuery();
            }

            BrickEngine.GetStreamHandler().AddStream(UserId, Streaming.StreamType.MadeFriends, FriendId);
            BrickEngine.GetStreamHandler().AddStream(FriendId, Streaming.StreamType.MadeFriends, UserId);

            return true;
        }

        public void DenyRequest(int UserId, int FriendId)
        {
            Friendship Friendship = GetFriendship(UserId, FriendId);

            if (Friendship == null)
            {
                return;
            }

            if (!Friendship.Pending)
            {
                return;
            }

            if (HasFriend(UserId, FriendId) || HasFriend(FriendId, UserId))
            {
                return;
            }

            Friendships.Remove(Friendship);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM user_friendships WHERE user_id = @userid AND friend_id = @friendid LIMIT 1");
                Reactor.AddParam("userid", UserId);
                Reactor.AddParam("friendid", FriendId);
                Reactor.ExcuteQuery();
            }
        }

        public void DeleteFriend(int UserId, int FriendId)
        {
            Friendship FriendShipA = GetFriendship(UserId, FriendId);
            Friendship FriendShipB = GetFriendship(FriendId, UserId);

            if (FriendShipA != null)
            {
                Friendships.Remove(FriendShipA);

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("DELETE FROM user_friendships WHERE user_id = @userid AND friend_id = @friendid LIMIT 1");
                    Reactor.AddParam("userid", UserId);
                    Reactor.AddParam("friendid", FriendId);
                    Reactor.ExcuteQuery();
                }
            }
            else if (FriendShipB != null)
            {
                Friendships.Remove(FriendShipB);

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("DELETE FROM user_friendships WHERE user_id = @userid AND friend_id = @friendid LIMIT 1");
                    Reactor.AddParam("userid", FriendId);
                    Reactor.AddParam("friendid", UserId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void AddUserToGroup(int Id, int UserId, int FriendId, int CategoryId)
        {
            if (!HasFriend(UserId, FriendId))
            {
                return;
            }

            Dictionary<int, Object> Row = new Dictionary<int, object>();

            Row[0] = Id;
            Row[1] = UserId;
            Row[2] = FriendId;
            Row[3] = CategoryId;

            FriendGroupItem Item = new FriendGroupItem(Row);

            FriendGroupItems.Add(Item.Id, Item);
        }

        public void RemoveUserFromGroup(int Id, int UserId)
        {
            if (!FriendGroupItems.ContainsKey(Id))
            {
                return;
            }

            if (FriendGroupItems[Id].UserId != UserId)
            {
                return;
            }

            FriendGroupItems.Remove(Id);
        }

        public void RenameGroup(int Id, int UserId, string Name)
        {
            FriendGroup Group = GetGroup(Id);

            if (Group == null)
            {
                return;
            }

            if (Group.UserId != UserId)
            {
                return;
            }

            if (!Group.Name.Equals(Name))
            {
                Group.Name = Name;
            }
        }

        public void CreateGroup(int Id, int UserId, string Name)
        {
            Dictionary<int, Object> Row = new Dictionary<int, object>();

            Row[0] = Id;
            Row[1] = UserId;
            Row[2] = Name;

            FriendGroup Group = new FriendGroup(Row);

            FriendGroups.Add(Group.Id, Group);
        }

        public void DeleteGroup(int Id, int UserId)
        {
            FriendGroup Group = GetGroup(Id);

            if (Group == null)
            {
                return;
            }

            if (Group.UserId != UserId)
            {
                return;
            }

            FriendGroups.Remove(Id);

            var ToRemove = new List<int>();

            foreach (FriendGroupItem Item in FriendGroupItems.Values)
            {
                if (Item.CategoryId == Id)
                {
                    ToRemove.Add(Item.Id);
                }
            }

            foreach (int ItemId in ToRemove)
            {
                FriendGroupItems.Remove(ItemId);
            }
        }
    }
}
