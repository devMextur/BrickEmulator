using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Storage;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users.Handlers.Membership;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using System.Threading;
using System.Text.RegularExpressions;

namespace BrickEmulator.HabboHotel.Users
{
    /// <summary>
    /// Stores the user information and handles the user his alive client.
    /// </summary>
    class UserCache
    {
        #region Fields

        #region MySQL Fields
        public readonly int HabboId;
        public string Username;
        public string Email;
        public string Motto;
        public string Look;
        public string Gender;
        public readonly int Rank;
        public int Credits;
        public int Pixels;
        public int HomeRoomId;
        public int AchievementScore;
        public int RespectGained;
        public int RespectGiven;
        public int RespectLeft;
        public int RespectLeftPets;
        public Boolean EnabledFriendStream;
        public Boolean EnableNewFriends;
        public int Warnings;
        public double MinutesOnline;
        public int MarketplaceTickets;

        public Boolean EnableWordfilter;
        public Boolean EnableShowOnline;
        public Boolean EnableFollow;
        public Boolean EnableTrade;
        public Boolean ActivatedEmail;

        public DateTime RegisteredDatetime;

        public int CurrentQuest = 0;
        public int CurrentQuestProgress = 0;

        public Boolean ProgressedNewbie = false; 

        public List<int> FavoriteRoomIds;
        public Dictionary<int, int> Achievements;
        public List<int> IgnoredUsers;
        public List<string> Tags;
        public Dictionary<int, int> Quests;
        #endregion

        #region C# Fields

        #region Collections
        public List<int> VotedRooms = new List<int>();
        public List<RoomVisit> VisitedRooms = new List<RoomVisit>();
        #endregion

        #region Mute
        public DateTime MutedStart = new DateTime(1, 1, 1);
        public int SecondsMuted = 0;

        public Boolean Muted
        {
            get
            {
                if (MutedStart.Year > 1)
                {
                    if ((DateTime.Now - MutedStart).TotalSeconds >= SecondsMuted)
                    {
                        MutedStart = new DateTime(1, 1, 1);
                        SecondsMuted = 0;

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        #endregion

        #region RoomHandling
        public int RoomId = -1;

        public Boolean IsInRoom
        {
            get
            {
                return (RoomId > -1);
            }
        }

        public int PreparingRoomId = -1;

        public Boolean IsLoadingRoom
        {
            get
            {
                return PreparingRoomId > -1;
            }
        }
        #endregion

        public readonly Object MessengerLocker = new Object();
        public Boolean HasFreeEffect = false;

        public readonly string Hash;
        public Boolean SearchedForFriends = false;

        #region Teleport

        #endregion

        #endregion

        #region Constructors

        public UserCache(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            this.HabboId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.Username = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            Counter.Skip();

            this.Email = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            this.Motto = BrickEngine.CleanString(BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]));
            this.Look = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            this.Gender = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            this.Rank = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.Credits = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.Pixels = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.HomeRoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.AchievementScore = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.RespectGained = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.RespectGiven = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.RespectLeft = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.RespectLeftPets = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.EnabledFriendStream = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.EnableNewFriends = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.Warnings = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            this.MinutesOnline = BrickEngine.GetConvertor().ObjectToDouble(Row[Counter.Next]);
            this.MarketplaceTickets = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            Counter.Update(2);

            this.EnableWordfilter = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.EnableShowOnline = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.EnableFollow = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.EnableTrade = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.ActivatedEmail = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            this.ProgressedNewbie = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);

            this.RegisteredDatetime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);

            BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.DarkCyan, IO.PaintType.ForeColor);
            BrickEngine.GetScreenWriter().ScretchLine("[" + HabboId + "] User " + Username + " has entered the building.", IO.WriteType.Incoming);

            Hash = BrickEngine.GetUserReactor().GenerateHash();
        }

        #endregion

        #endregion

        #region Methods

        #region Caching

        public void LoadFavoriteRooms()
        {
            FavoriteRoomIds = new List<int>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT room_id FROM user_favorite_rooms WHERE user_id = @habboid");
                Reactor.AddParam("habboid", HabboId);
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    FavoriteRoomIds.Add(BrickEngine.GetConvertor().ObjectToInt32(Row[0]));
                }
            }
        }

        public void LoadIgnores()
        {
            IgnoredUsers = new List<int>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT ignore_id FROM user_ignores WHERE user_id = @habboid");
                Reactor.AddParam("habboid", HabboId);
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    IgnoredUsers.Add(BrickEngine.GetConvertor().ObjectToInt32(Row[0]));
                }
            }
        }

        public void LoadTags()
        {
            Tags = new List<string>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT tag FROM user_tags WHERE user_id = @habboid");
                Reactor.AddParam("habboid", HabboId);
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    Tags.Add(BrickEngine.GetConvertor().ObjectToString(Row[0]));
                }
            }
        }

        public void LoadAchievements()
        {
            Achievements = new Dictionary<int, int>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT achievement_id, current_level FROM user_achievements WHERE user_id = @habboid");
                Reactor.AddParam("habboid", HabboId);
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    int AchievementId = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);
                    int CurrentLevel = BrickEngine.GetConvertor().ObjectToInt32(Row[1]);

                    Achievements.Add(AchievementId, CurrentLevel);
                }
            }
        }

        public void LoadQuests()
        {
            Quests = new Dictionary<int, int>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT quest_category_id, current_level FROM user_quests WHERE user_id = @habboid");
                Reactor.AddParam("habboid", HabboId);
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    int QuestCategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);
                    int CurrentLevel = BrickEngine.GetConvertor().ObjectToInt32(Row[1]);

                    Quests.Add(QuestCategoryId, CurrentLevel);
                }
            }
        }

        #endregion

        #region Responses

        public Response GetLoginResponse()
        {
            LoadAchievements();
            LoadFavoriteRooms();
            LoadIgnores();
            LoadQuests();
            LoadTags();

            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(GetClient(), 1);

            Response Builder = new Response();
            BrickEngine.GetEffectsHandler().GetResponse(GetClient(), Builder);

            var FloorItems = BrickEngine.GetItemReactor().GetUnseenItems(HabboId, 1);
            var WallItems = BrickEngine.GetItemReactor().GetUnseenItems(HabboId, 2);
            var PetItems = BrickEngine.GetItemReactor().GetUnseenItems(HabboId, 3);
            var BadgeItems = BrickEngine.GetItemReactor().GetUnseenItems(HabboId, 4);

            Builder.Initialize(832);
            Builder.AppendInt32(FloorItems.Count + WallItems.Count + PetItems.Count + BadgeItems.Count);

            if (FloorItems.Count > 0)
            {
                Builder.AppendInt32(1);
                Builder.AppendInt32(FloorItems.Count);

                foreach (int ItemId in FloorItems)
                {
                    Builder.AppendInt32(ItemId);
                }
            }

            if (WallItems.Count > 0)
            {
                Builder.AppendInt32(2);
                Builder.AppendInt32(WallItems.Count);

                foreach (int ItemId in WallItems)
                {
                    Builder.AppendInt32(ItemId);
                }
            }

            if (PetItems.Count > 0)
            {
                Builder.AppendInt32(3);
                Builder.AppendInt32(PetItems.Count);

                foreach (int PetId in PetItems)
                {
                    Builder.AppendInt32(PetId);
                }
            }

            if (BadgeItems.Count > 0)
            {
                Builder.AppendInt32(4);
                Builder.AppendInt32(BadgeItems.Count);

                foreach (int BadgeId in BadgeItems)
                {
                    Builder.AppendInt32(BadgeId);
                }
            }

            AtLogin();

            Builder.Initialize(455);
            Builder.AppendInt32(HomeRoomId);

            Builder.Initialize(458);
            Builder.AppendInt32(30);
            Builder.AppendInt32(FavoriteRoomIds.Count);
            FavoriteRoomIds.ToList().ForEach(Builder.AppendInt32);

            Builder.Initialize(2);

            if (GetMembership() != null)
            {
                Builder.AppendInt32(GetMembership().MemberScale + 1);
            }
            else
            {
                Builder.AppendInt32(0);
            }

            Builder.Initialize(290);
            Builder.AppendBoolean(true);
            Builder.AppendBoolean(false);

            Builder.Initialize(517);
            Builder.AppendBoolean(true);

            Builder.Initialize(628);
            Builder.AppendBoolean(true);
            Builder.AppendBoolean(false);
            Builder.AppendInt32(Pixels);

            return Builder;
        }

        #endregion

        #region Balance

        public void UpdateCredits(bool InDatabase)
        {
            Response Response = new Response(6);
            Response.AppendStringWithBreak(Credits + ".0");
            GetClient().SendResponse(Response);

            if (InDatabase)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE users SET credits = @amount WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("amount", Credits);
                    Reactor.AddParam("habboid", HabboId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void UpdatePixels(bool InDatabase)
        {
            UpdatePixels(InDatabase, 0);
        }

        public void UpdatePixels(bool InDatabase, int Amount)
        {
            Response Response = new Response(438);
            Response.AppendInt32(Pixels);
            Response.AppendInt32(Amount);
            GetClient().SendResponse(Response);

            if (InDatabase)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE users SET pixels = @amount WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("amount", Pixels);
                    Reactor.AddParam("habboid", HabboId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        #endregion

        #region Events
        public void AtLogin()
        {
            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(GetClient(), 5);
            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(GetClient(), 7);
            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(GetClient(), 8);
            BrickEngine.GetAchievementReactor().UpdateUsersAchievement(GetClient(), 9);

            if (EnableShowOnline)
            {
                lock (MessengerLocker)
                {
                    BrickEngine.GetMessengerHandler().AlertStatusFriends(this, true);
                }
            }

            if (BrickEngine.GetRoomReactor().GetMe(HabboId).Count == 0)
            {
                if (MinutesOnline < 1)
                {
                    string RoomName = string.Format("{0}'s  room", Username);
                    string RoomDesc = string.Format("{0} has entered the building", Username);

                    int RoomId = BrickEngine.GetRoomReactor().CreateRoom(GetClient(), RoomName, "model_f", RoomDesc, "210","110");

                    if (RoomId > 0)
                    {
                        HomeRoomId = RoomId;

                        using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                        {
                            Reactor.SetQuery("UPDATE users SET home_room_id = @roomid WHERE id = @habboid LIMIT 1");
                            Reactor.AddParam("roomid", RoomId);
                            Reactor.AddParam("habboid", HabboId);
                            Reactor.ExcuteQuery();
                        }
                    }
                }
            }

            HandleStateParams(true);
        }

        public void AtEnterRoom(int RoomId)
        {
            VisitedRooms.Add(new RoomVisit(RoomId));

            if (!ProgressedNewbie && Regex.Replace(Email.Split('@')[0].ToLower(), @"\d", string.Empty).Equals(Regex.Replace(Username.ToLower(), @"\d", string.Empty)))
            {
                if (BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).OwnerId == HabboId)
                {
                    Response NewbieMenu = new Response(575);
                    NewbieMenu.AppendBoolean(true);
                    NewbieMenu.AppendBoolean(false);
                    NewbieMenu.AppendBoolean(false);
                    GetClient().SendResponse(NewbieMenu);
                }
            }

            if (EnableShowOnline)
            {
                lock (MessengerLocker)
                {
                    BrickEngine.GetMessengerHandler().AlertStatusFriends(this, true);
                }
            }

            if (Muted)
            {
                Response Mute = new Response(27);
                Mute.AppendInt32(Convert.ToInt32(SecondsMuted - (DateTime.Now - MutedStart).TotalSeconds));
                GetClient().SendResponse(Mute);
            }
        }

        public void AtLeaveRoom(int RoomId)
        {
            if (RoomId > -1)
            {
                RoomId = -1;
            }

            if (GetRoomVisit(RoomId) != null)
            {
                GetRoomVisit(RoomId).Updated = true;
                GetRoomVisit(RoomId).Leaved = DateTime.Now;
            }

            if (EnableShowOnline)
            {
                lock (MessengerLocker)
                {
                    BrickEngine.GetMessengerHandler().AlertStatusFriends(this, true);
                }
            }
        }

        public void AtDisconnect()
        {
            lock (this)
            {
                LeaveCurrentRoom();

                if (EnableShowOnline)
                {
                    lock (MessengerLocker)
                    {
                        BrickEngine.GetMessengerHandler().AlertStatusFriends(this, false);
                    }
                }

                HandleStateParams(false);
            }
        }

        #endregion

        #region Rooms

        private RoomVisit GetRoomVisit(int RoomId)
        {
            foreach (RoomVisit Visit in (from visit in VisitedRooms orderby visit.Entered ascending select visit))
            {
                if (Visit.RoomId == RoomId)
                {
                    return Visit;
                }
            }

            return null;
        }

        public void LeaveCurrentRoom()
        {
            if (IsInRoom)
            {
                GetRoom().GetRoomEngine().HandleLeaveUser(HabboId, false);
            }
        }

        public void RefreshUser()
        {
            Response Response = new Response(266);
            Response.AppendInt32(-1);
            Response.AppendStringWithBreak(Look);
            Response.AppendStringWithBreak(Gender.ToLower());
            Response.AppendStringWithBreak(Motto);
            Response.AppendInt32(AchievementScore);
            GetClient().SendResponse(Response);

            if (EnableShowOnline)
            {
                lock (MessengerLocker)
                {
                    BrickEngine.GetMessengerHandler().AlertStatusFriends(this, true);
                }
            }

            if (IsInRoom)
            {
                Response RoomResponse = new Response(266);
                RoomResponse.AppendInt32(GetRoomUser().VirtualId);
                RoomResponse.AppendStringWithBreak(Look);
                RoomResponse.AppendStringWithBreak(Gender.ToLower());
                RoomResponse.AppendStringWithBreak(Motto);
                RoomResponse.AppendInt32(AchievementScore);
                GetClient().SendResponse(Response);

                GetRoom().GetRoomEngine().BroadcastResponse(RoomResponse);
            }
        }

        #endregion

        #region Others

        public Boolean HasIgnoredUser(int HabboId)
        {
            return IgnoredUsers.Contains(HabboId);
        }

        private void HandleStateParams(bool Alive)
        {
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                StringBuilder QueryBuilder = new StringBuilder("");
                QueryBuilder.Append("UPDATE users SET status = @state");

                if (Alive)
                {
                    QueryBuilder.Append(", last_alive = @last, alive_ip = @ip");
                }

                QueryBuilder.Append(" WHERE id = @habboid LIMIT 1");


                Reactor.SetQuery(QueryBuilder.ToString());

                if (Alive)
                {
                    Reactor.AddParam("ip", GetClient().IPAddress);
                }

                Reactor.AddParam("last", DateTime.Now);
                Reactor.AddParam("habboid", HabboId);
                Reactor.AddParam("state", (Alive ? 1 : 0).ToString());
                Reactor.ExcuteQuery();
            }
        }

        #endregion

        #region Garbage

        public VirtualRoomUser GetRoomUser()
        {
            return GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);
        }

        public VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, BrickEmulator.HabboHotel.Rooms.RoomRunningState.Alive);
        }

        public Membership GetMembership()
        {
            return BrickEngine.GetMembershipHandler().GetCurrentMembership(HabboId);
        }

        public Client GetClient()
        {
            return BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Stores info about an Room visit.
    /// </summary>
    class RoomVisit
    {
        #region Fields

        public readonly int RoomId;
        public readonly DateTime Entered = DateTime.Now;

        public DateTime Leaved;

        public Boolean Updated = false;

        public RoomVisit(int RoomId)
        {
            this.RoomId = RoomId;
        }

        #endregion
    }
}
