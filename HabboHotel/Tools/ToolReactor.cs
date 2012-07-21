using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms.Chatlogs;
using BrickEmulator.Storage;
using BrickEmulator.Security;
using System.Data;
using System.Net;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;

namespace BrickEmulator.HabboHotel.Tools
{
    class ToolReactor
    {
        private List<BannedItem> BannedUsers;
        private List<string> DefaultUserAlerts;
        private List<string> DefaultRoomAlerts;

        public ToolReactor()
        {
            LoadBans();
            LoadAlerts();
        }

        public void LoadAlerts()
        {
            DefaultUserAlerts = new List<string>();
            DefaultRoomAlerts = new List<string>();

            DataTable UserTable = null;
            DataTable RoomTable = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT message FROM default_mod_alerts WHERE type = 'user'");
                UserTable = Reactor.GetTable();
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT message FROM default_mod_alerts WHERE type = 'room'");
                RoomTable = Reactor.GetTable();
            }

            foreach (DataRow Row in UserTable.Rows)
            {
                DefaultUserAlerts.Add(BrickEngine.GetConvertor().ObjectToString(Row[0]));
            }

            foreach (DataRow Row in RoomTable.Rows)
            {
                DefaultRoomAlerts.Add(BrickEngine.GetConvertor().ObjectToString(Row[0]));
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + DefaultUserAlerts.Count + DefaultRoomAlerts.Count + "] DefaultMessage(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadBans()
        {
            BannedUsers = new List<BannedItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_bans");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                BannedItem Item = new BannedItem(Row);

                BannedUsers.Add(Item);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + BannedUsers.Count + "] BannedUser(s) cached.", IO.WriteType.Outgoing);
        }

        public DateTime GetExpiredDateTime(int UserId)
        {
            foreach (BannedItem Item in BannedUsers)
            {
                if (Item.UserId == UserId)
                {
                    return Item.Ended;
                }
            }

            return new DateTime(1, 1, 1);
        }

        public Boolean IsBanned(int UserId, string IP)
        {
            foreach (BannedItem Item in BannedUsers)
            {
                if (Item.IPBan)
                {
                    if (Item.UserIP == IP)
                    {
                        return true;
                    }
                }

                if (Item.UserId == UserId)
                {
                    return true;
                }
            }

            return false;
        }

        public Response GetResponse(Client Client)
        {
            if (Client.GetUser().Rank < 5)
            {
                return null;
            }

            Response Response = new Response(531);
            Response.AppendInt32(-1);
            Response.AppendInt32(DefaultUserAlerts.Count);
            DefaultUserAlerts.ForEach(Response.AppendStringWithBreak);

            Response.AppendInt32(0);
            Response.AppendBoolean(true);
            Response.AppendInt32(1);
            Response.AppendInt32(1);
            Response.AppendInt32(1);
            Response.AppendInt32(1);
            Response.AppendInt32(1);
            Response.AppendInt32(1);
            Response.AppendInt32(DefaultRoomAlerts.Count);
            DefaultRoomAlerts.ForEach(Response.AppendStringWithBreak);
            return Response;
        }

        public Response GetRoomResponse(Client Client, int RoomId)
        {
            if (Client.GetUser().Rank < 5)
            {
                Client.Notif("You need rank '5' to do this action.", false);
                return null;
            }

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Dead);

            if (Room == null)
            {
                return null;
            }

            Response Response = new Response(538);
            Response.AppendInt32(RoomId);
            Response.AppendInt32(Room.RoomUserAmount);
            Response.AppendBoolean(BrickEngine.GetUserReactor().IsOnline(Room.OwnerId) && Room.GetRoomEngine().GetUserByHabboId(Room.OwnerId) != null);
            Response.AppendInt32(Room.OwnerId);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(Room.OwnerId));
            Response.AppendInt32(RoomId);
            Response.AppendStringWithBreak(Room.Name);
            Response.AppendStringWithBreak(Room.Description);
            Response.AppendInt32(Room.Tags.Count);
            Room.Tags.ForEach(Response.AppendStringWithBreak);

            Response.AppendBoolean(Room.Event != null);

            if (Room.Event != null)
            {
                Response.AppendStringWithBreak(Room.Event.Name);
                Response.AppendStringWithBreak(Room.Event.Description);
                Response.AppendInt32(Room.Event.Tags.Count);
                Room.Event.Tags.ForEach(Response.AppendStringWithBreak);
            }

            return Response;
        }

        public Response GetUserInfo(Client Client, int UserId)
        {
            if (Client.GetUser().Rank < 5)
            {
                Client.Notif("You need rank '5' to do this action.", false);
                return null;
            }

            Response Response = new Response(533);
            Response.AppendInt32(UserId);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(UserId));

            Response.AppendInt32(Convert.ToInt32((DateTime.Now - DateTime.Parse(BrickEngine.GetUserReactor().GetRegistered(UserId))).TotalMinutes)); // REGISTER_
            Response.AppendInt32(Convert.ToInt32((DateTime.Now - DateTime.Parse(BrickEngine.GetUserReactor().GetLastVisit(UserId))).TotalMinutes)); // LAST_ONLINE_

            Response.AppendBoolean(BrickEngine.GetUserReactor().IsOnline(UserId));

            Response.AppendInt32(0); // CALLS_FOR_HELP_AMOUNT
            Response.AppendInt32(0); // CALLS_FOR_HELP_USELESS_AMOUNT
            Response.AppendInt32(BrickEngine.GetUserReactor().GetWarnings(UserId)); // WARNINGS_AMOUNT
            Response.AppendInt32(0); // BANS_AMOUNT

            return Response;
        }

        public Response GetUserRoomVisits(Client Client, int UserId)
        {
            if (Client.GetUser().Rank < 5)
            {
                Client.Notif("You need rank '5' to do this action.", false);
                return null;
            }

            Response Response = new Response(537);
            Response.AppendInt32(UserId);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(UserId));

            if (BrickEngine.GetUserReactor().IsOnline(UserId))
            {            
                var Visits = BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().GetUser().VisitedRooms;

                var Sorted = (from kvp in Visits orderby kvp.Entered ascending select kvp);

                Response.AppendInt32(Sorted.Count());

                foreach (RoomVisit Visit in Sorted)
                {
                    Response.AppendBoolean(false);
                    Response.AppendInt32(Visit.RoomId);
                    Response.AppendStringWithBreak(BrickEngine.GetRoomReactor().GetRoomName(Visit.RoomId));
                    Response.AppendInt32(Visit.Entered.Hour);
                    Response.AppendInt32(Visit.Entered.Minute);
                }
            }
            else
            {
                Response.AppendInt32(0);
            }

            return Response;
        }

        public Response GetUserChatlogs(Client Client, int UserId)
        {
            if (Client.GetUser().Rank < 5)
            {
                Client.Notif("You need rank '5' to do this action.", false);
                return null;
            }

            var Chatlogs = BrickEngine.GetChatlogHandler().GetChatlogsForUserId(UserId);

            var RoomIdList = new List<int>();

            foreach (Chatlog Chatlog in Chatlogs)
            {
                if (!RoomIdList.Contains(Chatlog.RoomId))
                {
                    RoomIdList.Add(Chatlog.RoomId);
                }
            }

            Response Response = new Response(536);
            Response.AppendInt32(UserId);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(UserId));

            if (BrickEngine.GetUserReactor().IsOnline(UserId))
            {
                var Visits = BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().GetUser().VisitedRooms;
                var Sorted = (from kvp in Visits orderby kvp.Entered ascending select kvp);

                Response.AppendInt32(Sorted.Count());

                foreach (RoomVisit Visit in Visits)
                {
                    Response.AppendBoolean(false);
                    Response.AppendInt32(Visit.RoomId);
                    Response.AppendStringWithBreak(BrickEngine.GetRoomReactor().GetRoomName(Visit.RoomId));

                    var Logs = BrickEngine.GetChatlogHandler().GetChatlogsForRoomId(Visit);

                    Response.AppendInt32(Logs.Count);

                    foreach (Chatlog Chatlog in Logs)
                    {
                        Response.AppendInt32(Chatlog.Time.Hour);
                        Response.AppendInt32(Chatlog.Time.Minute);
                        Response.AppendInt32(Chatlog.UserId);
                        Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(Chatlog.UserId));
                        Response.AppendStringWithBreak(Chatlog.Message);
                    }
                }
            }
            else
            {
                Response.AppendInt32(RoomIdList.Count);

                foreach (int RoomId in RoomIdList)
                {
                    Response.AppendBoolean(false);
                    Response.AppendInt32(RoomId);
                    Response.AppendStringWithBreak(BrickEngine.GetRoomReactor().GetRoomName(RoomId));
                }
            }

            return Response;
        }

        public Response GetRoomChatlogs(Client Client, int RoomId)
        {
            if (Client.GetUser().Rank < 5)
            {
                Client.Notif("You need rank '5' to do this action.", false);
                return null;
            }

            Response Response = new Response(535);
            Response.AppendBoolean(false);
            Response.AppendInt32(RoomId);
            Response.AppendStringWithBreak(BrickEngine.GetRoomReactor().GetRoomName(RoomId));

            Response.AppendInt32(BrickEngine.GetChatlogHandler().GetChatlogsForRoomId(RoomId).Count);

            foreach (Chatlog Chatlog in BrickEngine.GetChatlogHandler().GetChatlogsForRoomId(RoomId))
            {
                Response.AppendInt32(Chatlog.Time.Hour);
                Response.AppendInt32(Chatlog.Time.Minute);
                Response.AppendInt32(Chatlog.UserId);
                Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(Chatlog.UserId));
                Response.AppendStringWithBreak(Chatlog.Message);
            }

            return Response;
        }

        public void AlertUser(Client Client, int UserId, string Message, Boolean Warning, Boolean Kick, Boolean RoomAlert)
        {
            if (Client.GetUser().Rank < 6)
            {
                Client.Notif("You need rank '6' to do this action.", false);
                return;
            }

            if (Client.GetUser().HabboId.Equals(UserId) && !RoomAlert)
            {
                Client.Notif("You can't message yourself.", false);
                return;
            }

            if (BrickEngine.GetUserReactor().IsOnline(UserId))
            {
                if (Kick)
                {
                    if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().GetUser().IsInRoom)
                    {
                        BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().GetUser().GetRoom().GetRoomEngine().HandleLeaveUser(UserId, true);
                    }
                }

                if (Message.Length > 0)
                {
                    BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().Notif(Message, true);
                }
            }

            if (Warning)
            {
                if (BrickEngine.GetUserReactor().IsOnline(UserId))
                {
                    BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().GetUser().Warnings++;
                }

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE users SET warnings = warnings + 1 WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("habboid", UserId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void AlertRoom(Client Client, int RoomId, string Message, Boolean Warning)
        {
            if (Client.GetUser().Rank < 7)
            {
                Client.Notif("You need rank '7' to do this action.", false);
                return;
            }

            if (BrickEngine.GetRoomReactor().RoomIsAlive(RoomId))
            {
                if (BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).RoomUserAmount > 0)
                {
                    foreach (VirtualRoomUser User in BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).GetRoomEngine().GetUsers())
                    {
                        if (User.GetClient().GetUser().Rank > 1)
                        {
                            AlertUser(Client, User.HabboId, Message, false, false, true);
                        }
                        else
                        {
                            AlertUser(Client, User.HabboId, Message, Warning, false, true);
                        }
                    }
                }
            }
        }

        public void HandleRoom(Client Client, int RoomId, Boolean SetDoorBell, Boolean SetRoomName, Boolean KickUsers)
        {
            if (Client.GetUser().Rank < 7)
            {
                Client.Notif("You need rank '7' to do this action.", false);
                return;
            }

            // KickUsers
            if (BrickEngine.GetRoomReactor().RoomIsAlive(RoomId))
            {
                if (KickUsers)
                {
                    if (BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).RoomUserAmount > 0)
                    {
                        foreach (VirtualRoomUser User in BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).GetRoomEngine().GetUsers())
                        {
                            if (User.GetClient().GetUser().Rank <= 2)
                            {
                                AlertUser(Client, User.HabboId, string.Empty, false, true, false);
                            }
                        }
                    }

                    if (BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).Event != null)
                    {
                        BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).Event.Drop();
                    }
                }
            }

            if (SetDoorBell)
            {
                if (BrickEngine.GetRoomReactor().RoomIsAlive(RoomId))
                {
                    BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).DoorState = 1;
                }

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE private_rooms SET door_state = '1' WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", RoomId);
                    Reactor.ExcuteQuery();
                }
            }

            if (SetRoomName)
            {
                if (BrickEngine.GetRoomReactor().RoomIsAlive(RoomId))
                {
                    BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive).Name = "Inappropriate to hotel management";
                }

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE private_rooms SET name = 'Inappropriate to hotel management' WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", RoomId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void BanUser(Client Client, int UserId, string Reason, int HoursLength, Boolean IPBan)
        {
            if (Client.GetUser().Rank < 7)
            {
                Client.Notif("You need rank '7' to do this action.", false);
                return;
            }

            if (Client.GetUser().HabboId.Equals(UserId))
            {
                Client.Notif("You can't ban yourself.", false);
                return;
            }

            if (BrickEngine.GetUserReactor().GetUserRank(UserId) >= Client.GetUser().Rank)
            {
                Client.Notif("You can't ban someone with the same / higher rank!", false);
                return;
            }

            // Generating Cache for Querys
            DateTime Started = DateTime.Now;
            DateTime Expire = DateTime.Now.AddHours(HoursLength);

            int SenderId = Client.GetUser().HabboId;

            string FixedReason = BrickEngine.CleanString(Reason);

            string UserIP = IPAddress.Any.ToString();

            if (BrickEngine.GetUserReactor().IsOnline(UserId))
            {
                UserIP = BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().IPAddress;

                // Disconnect User
                BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().Notif("You've been banned for " + HoursLength + " hours, with reason: " + Reason, true);
                BrickEngine.GetSocketShield().GetSocketClientByHabboId(UserId).GetClient().Dispose();
            }

            // Doing Querys + data
            BannedUsers.Add(new BannedItem(UserId, UserIP, IPBan, Started, Expire, SenderId, FixedReason));

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO user_bans (user_id, user_ip, ip_ban, given_datetime, end_datetime, given_mod_id, reason) VALUES (@habboid, @habboip, @ipban, @givendt, @enddt, @givenhabboid, @reason)");
                Reactor.AddParam("habboid", UserId);
                Reactor.AddParam("habboip", UserIP);
                Reactor.AddParam("ipban", IPBan ? 1 : 0);
                Reactor.AddParam("givendt", Started);
                Reactor.AddParam("enddt", Expire);
                Reactor.AddParam("givenhabboid", SenderId);
                Reactor.AddParam("reason", FixedReason);
                Reactor.ExcuteQuery();
            }

            // Write Warning @ Screen
            BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Blue, IO.PaintType.ForeColor);
            BrickEngine.GetScreenWriter().ScretchLine("User " + BrickEngine.GetUserReactor().GetUsername(UserId) + " has been banned for " + HoursLength + " hours.", IO.WriteType.Incoming);
        }
    }
}
