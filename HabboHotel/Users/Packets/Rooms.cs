using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Rooms.Navigator.Items;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.HabboHotel.Pets;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        public void ClearLoading(Client Client, Boolean Reset)
        {
            if (Client.GetUser().IsInRoom)
            {
                Client.GetUser().RoomId = -1;
            }

            if (Client.GetUser().IsLoadingRoom)
            {
                Client.GetUser().PreparingRoomId = -1;
            }

            if (Reset)
            {
                Client.SendResponse(new Response(18));
            }
        }

        public void OpenFeacturedRoom(Client Client, Request Request)
        {
            int RoomId = Request.PopWiredInt32();
            string Password = Request.PopFixedString();

            if (Client.GetUser().RoomId == RoomId)
            {
                return;
            }

            BeginLoadRoom(Client, RoomId, Password);
        }

        public void OpenPrivateRoom(Client Client, Request Request)
        {
            int RoomId = Request.PopWiredInt32();
            string Password = Request.PopFixedString();

            BeginLoadRoom(Client, RoomId, Password);
        }

        public void BeginLoadRoom(Client Client, int RoomId, string Password)
        {
            if (Client.GetUser().PreparingRoomId == RoomId)
            {
                return;
            }

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, Rooms.RoomRunningState.Alive);

            if (Room == null)
            {
                ClearLoading(Client, true);
                return;
            }

            if (BrickEngine.GetRoomReactor().GetRoomModel(Room.ModelParam) == null)
            {
                Client.Notif("The RoomModel of this room is missing.", false);
                ClearLoading(Client, true);
                return;
            }

            if (!Room.BeginEnterRoom(Client, Password))
            {
                Client.GetUser().LeaveCurrentRoom();
                ClearLoading(Client, false);
            }
            else
            {
                Client.GetUser().LeaveCurrentRoom();
                Client.GetUser().PreparingRoomId = RoomId;
                ContinueLoading(Client, null);
            }
        }

        private void ContinueLoading(Client Client, Request Request)
        {
            if (!Client.GetUser().IsLoadingRoom)
            {
                return;
            }

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(Client.GetUser().PreparingRoomId, Rooms.RoomRunningState.Alive);

            if (Room == null)
            {
                ClearLoading(Client, true);
                return;
            }

            Room.GetSecondairResponse(Client);
        }

        private void GetMapParams(Client Client, Request Request)
        {
            if (!Client.GetUser().IsLoadingRoom)
            {
                return;
            }

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(Client.GetUser().PreparingRoomId, Rooms.RoomRunningState.Alive);

            if (Room == null)
            {
                ClearLoading(Client, true);
                return;
            }

            Room.GetMapsResponse(Client);
        }

        private void ActivateLoading(Client Client, Request Request)
        {
            Response Response = new Response(297);
            Response.AppendBoolean(false);
            Client.SendResponse(Response);
        }

        private void EndLoadRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsLoadingRoom)
            {
                return;
            }

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(Client.GetUser().PreparingRoomId, Rooms.RoomRunningState.Alive);

            if (Room == null)
            {
                ClearLoading(Client, true);
                return;
            }

            Room.GetRestResponse(Client);

            ClearLoading(Client, false);
            Client.GetUser().RoomId = Room.Id;
        }

        private void GetEventMenu(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            Response Response = new Response(367);
            Response.AppendBoolean(true);
            Response.AppendBoolean(false);
            Client.SendResponse(Response);
        }

        private void UpdateEvent(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            int CategoryId = Request.PopWiredInt32();
            string Name = BrickEngine.CleanString(Request.PopFixedString());
            string Description = BrickEngine.CleanString(Request.PopFixedString());
            int TagCount = Request.PopWiredInt32();

            List<string> Tags = new List<string>();

            for (int i = 0; i < TagCount; i++)
            {
                Tags.Add(BrickEngine.CleanString(Request.PopFixedString()));
            }

            RoomEvent Event = null;

            if (Client.GetUser().GetRoom().Event == null)
            {
                Event = new RoomEvent(Client.GetUser().RoomId, Name, Description, CategoryId, Tags);

                Client.GetUser().GetRoom().Event = Event;

                // Friends alert
                BrickEngine.GetMessengerHandler().AlertFriends(Client.GetUser().HabboId, BrickEngine.GetMessengerHandler().GetAchievedResponse(Client.GetUser().HabboId, false, Name));
            }
            else
            {
                Event = Client.GetUser().GetRoom().Event;

                Event.Name = Name;
                Event.Description = Description;
                Event.Tags = Tags;
            }

            Response Response = new Response(370);
            Event.GetResponse(Response);

            Client.SendRoomResponse(Response);
        }

        private void EndEvent(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            if (Client.GetUser().GetRoom().Event != null)
            {
                Client.GetUser().GetRoom().Event.Drop();
            }
        }

        private void BeginEditRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int RoomId = Request.PopWiredInt32();

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);

            if (Room == null)
            {
                return;
            }

            if (!Room.GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            Response Response = new Response(465);
            Response.AppendInt32(RoomId);
            Response.AppendStringWithBreak(Room.Name);
            Response.AppendStringWithBreak(Room.Description);
            Response.AppendInt32(Room.DoorState);
            Response.AppendInt32(Room.CategoryId);
            Response.AppendInt32(Room.LimitUsers);
            Response.AppendInt32(Room.GetRoomModel().LimitUsers);
            Response.AppendInt32(Room.Tags.Count);
            Room.Tags.ForEach(Response.AppendStringWithBreak);

            Response.AppendInt32(Room.GetRoomEngine().GetUsersWithRights().Count);
            
            foreach (int UserId in Room.GetRoomEngine().GetUsersWithRights())
            {
                Response.AppendInt32(UserId);
                Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(UserId));
            }

            Response.AppendInt32(Room.GetRoomEngine().GetUsersWithRights().Count);

            Response.AppendBoolean(Room.AllowPets);
            Response.AppendBoolean(Room.AllowPetsEat);
            Response.AppendBoolean(Room.AllowWalkthough);
            Response.AppendBoolean(Room.AllowHideWall);
            Response.AppendInt32(Room.WallThick);
            Response.AppendInt32(Room.FloorThick);
            Client.SendResponse(Response);
        }

        private void EndEditRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int RoomId = Request.PopWiredInt32();

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);

            if (Room == null)
            {
                return;
            }

            if (!Room.GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            string Name = BrickEngine.CleanString(Request.PopFixedString());
            string Description = BrickEngine.CleanString(Request.PopFixedString());
            int DoorState = Request.PopWiredInt32();
            string Password = BrickEngine.CleanString(Request.PopFixedString());
            int LimitUsers = Request.PopWiredInt32();

            int CategoryId = Request.PopWiredInt32();

            PrivateCategory Category = BrickEngine.GetNavigatorManager().GetPrivateCategory(CategoryId);

            if (Category == null)
            {
                CategoryId = 0;
            }

            if (Client.GetUser().Rank < Category.RankAllowed)
            {
                Client.Notif("You're not allowed to use this category.", false);
                CategoryId = 0;
            }

            int TagAmount = Request.PopWiredInt32();

            List<string> Tags = new List<string>();

            for (int i = 0; i < TagAmount; i++)
            {
                string Tag = BrickEngine.CleanString(Request.PopFixedString()).Trim().ToLower();

                if (Tag.Length > 32)
                {
                    Tag = Tag.Substring(0, 32);
                }

                if (Tag.Length > 0 && !Tags.Contains(Tag))
                {
                    Tags.Add(Tag);
                }
            }

            Boolean AllowPets = (Request.PlainReadBytes(1)[0].ToString() == "65");
            Request.AdvancePointer(1);

            Boolean AllowPetsEat = (Request.PlainReadBytes(1)[0].ToString() == "65");
            Request.AdvancePointer(1);

            Boolean AllowWalkthough = (Request.PlainReadBytes(1)[0].ToString() == "65");
            Request.AdvancePointer(1);

            Boolean AllowHideWall = (Request.PlainReadBytes(1)[0].ToString() == "65");
            Request.AdvancePointer(1);

            int WallThick = Request.PopWiredInt32();

            if (WallThick < -2 || WallThick > 1)
            {
                WallThick = 0;
            }

            int FloorThick = Request.PopWiredInt32();

            if (FloorThick < -2 || FloorThick > 1)
            {
                FloorThick = 0;
            }

            if (Name.Length > 60)
            {
                Name = Name.Substring(0, 60);
            }

            if (Description.Length > 128)
            {
                Description = Description.Substring(0, 128);
            }

            if (Password.Length > 64)
            {
                Password = Password.Substring(0, 64);
            }

            if (LimitUsers > Client.GetUser().GetRoom().GetRoomModel().LimitUsers)
            {
                LimitUsers = Client.GetUser().GetRoom().GetRoomModel().LimitUsers;
            }

            if (DoorState == 2 && Password.Length <= 0)
            {
                DoorState = 0;
            }

            Dictionary<string, Object> Params = new Dictionary<string, object>();

            List<string> Commands = new List<string>();

            if (!Room.Name.Equals(Name))
            {
                Commands.Add("name = @name");
                Params.Add("name", Name);
                Room.Name = Name;
            }

            if (!Room.Description.Equals(Description))
            {
                Commands.Add("description = @desc");
                Params.Add("desc", Description);
                Room.Description = Description;
            }

            if (!Room.DoorState.Equals(DoorState))
            {
                Commands.Add("door_state = @door");
                Params.Add("door", DoorState);
                Room.DoorState = DoorState;
            }

            if (!Room.Password.Equals(Password))
            {
                Commands.Add("password = @pw");
                Params.Add("pw", Password);
                Room.Password = Password;
            }

            if (!Room.LimitUsers.Equals(LimitUsers))
            {
                Commands.Add("limit_users = @limit");
                Params.Add("limit", LimitUsers);
                Room.LimitUsers = LimitUsers;
            }

            if (!Room.CategoryId.Equals(CategoryId))
            {
                Commands.Add("category_id = @catid");
                Params.Add("catid", CategoryId);
                Room.CategoryId = CategoryId;
            }

            if (!Room.Tags.Equals(Tags))
            {
                string SplittedTags = string.Empty;

                int x = 0;

                foreach (string Tag in Tags)
                {
                    if (x > 0)
                    {
                        SplittedTags += ',';
                    }

                    SplittedTags += Tag.ToLower();

                    x++;
                }

                Commands.Add("tags = @tags");
                Params.Add("tags", SplittedTags.ToString());
                Room.Tags = Tags;
            }

            if (!Room.AllowPets.Equals(AllowPets))
            {
                Commands.Add("allow_pets = @allow_pets");
                Params.Add("allow_pets", AllowPets ? 1 : 0);
                Room.AllowPets = AllowPets;
            }

            if (!Room.AllowPetsEat.Equals(AllowPetsEat))
            {
                Commands.Add("allow_pets_eat = @allow_pets_eat");
                Params.Add("allow_pets_eat", AllowPetsEat ? 1 : 0);
                Room.AllowPetsEat = AllowPetsEat;
            }

            if (!Room.AllowWalkthough.Equals(AllowWalkthough))
            {
                Commands.Add("allow_walkthough = @allow_walkthough");
                Params.Add("allow_walkthough", AllowWalkthough ? 1 : 0);
                Room.AllowWalkthough = AllowWalkthough;
            }

            if (!Room.AllowHideWall.Equals(AllowHideWall))
            {
                Commands.Add("allow_hidewall = @allow_hidewall");
                Params.Add("allow_hidewall", AllowHideWall ? 1 : 0);
                Room.AllowHideWall = AllowHideWall;
            }

            if (!Room.WallThick.Equals(WallThick))
            {
                Commands.Add("walls_thick = @walls_thick");
                Params.Add("walls_thick", WallThick);
                Room.WallThick = WallThick;
            }

            if (!Room.FloorThick.Equals(FloorThick))
            {
                Commands.Add("floors_thick = @floors_thick");
                Params.Add("floors_thick", FloorThick);
                Room.FloorThick = FloorThick;
            }

            Response Data = new Response(454);
            Data.AppendBoolean(true);
            Room.GetNavigatorResponse(Data, false);
            Client.SendRoomResponse(Data);

            Response Response = new Response();
            Response.Initialize(467);
            Response.AppendInt32(Client.GetUser().RoomId);
            Response.Initialize(456);
            Response.AppendInt32(Client.GetUser().RoomId);
            Client.SendResponse(Response);

            Response RoomStruct = new Response(472);
            RoomStruct.AppendBoolean(AllowHideWall);
            RoomStruct.AppendInt32(WallThick);
            RoomStruct.AppendInt32(FloorThick);
            Client.SendRoomResponse(RoomStruct);

            if (Commands.Count > 0)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    StringBuilder Builder = new StringBuilder();
                    Builder.Append("UPDATE private_rooms SET ");

                    int i = 0;

                    foreach (string Command in Commands)
                    {
                        i++;
                        Builder.Append(Command);

                        if (i < Commands.Count)
                        {
                            Builder.Append(", ");
                        }
                    }

                    Builder.Append(" WHERE id = @roomid LIMIT 1");

                    Reactor.SetQuery(Builder.ToString());

                    foreach (KeyValuePair<string, Object> kvp in Params)
                    {
                        Reactor.AddParam(kvp.Key, kvp.Value);
                    }

                    Reactor.AddParam("roomid", Room.Id);
                    
                    Reactor.ExcuteQuery();
                }
            }
        }

        private void UpdateRoomIcon(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int Q = Request.PopWiredInt32();

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            var Icons = new Dictionary<int, int>();

            int BackgroundIcon = Request.PopWiredInt32();

            if (BackgroundIcon < 1 || BackgroundIcon > 24)
            {
                BackgroundIcon = 1;
            }

            int ForegroundIcon = Request.PopWiredInt32();

            if (ForegroundIcon < 0 || ForegroundIcon > 11)
            {
                ForegroundIcon = 0;
            }

            int IconsAmount = Request.PopWiredInt32();

            for (int i = 0; i < IconsAmount; i++)
            {
                int SlotId = Request.PopWiredInt32();
                int Icon = Request.PopWiredInt32();

                if (SlotId < 0 || SlotId > 10 || Icon < 1 || Icon > 27)
                {
                    continue;
                }

                if (!Icons.ContainsKey(SlotId))
                {
                    Icons.Add(SlotId, Icon);
                }
            }

            int x = 0;

            StringBuilder Query = new StringBuilder();

            foreach (KeyValuePair<int, int> kvp in Icons)
            {
                if (x < Icons.Count)
                {
                    Query.Append(',');
                }

                Query.Append(kvp.Key);
                Query.Append('.');
                Query.Append(kvp.Value);

                x++;
            }

            Client.GetUser().GetRoom().Icon = new RoomIcon(Client.GetUser().GetRoom().Id, BackgroundIcon, ForegroundIcon, Query.ToString());

            Response Response = new Response();
            Response.Initialize(457);
            Response.AppendInt32(Client.GetUser().RoomId);
            Response.AppendBoolean(true);
            Response.Initialize(456);
            Response.AppendInt32(Client.GetUser().RoomId);
            Client.SendResponse(Response);

            Response Data = new Response(454);
            Data.AppendBoolean(false);
            Client.GetUser().GetRoom().GetNavigatorResponse(Data, false);
            Client.SendRoomResponse(Data);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE private_rooms SET nav_icon_bg = @bg, nav_icon_fg = @fg, nav_icons = @icons WHERE id = @roomid LIMIT 1");
                Reactor.AddParam("bg", BackgroundIcon);
                Reactor.AddParam("fg", ForegroundIcon);
                Reactor.AddParam("icons", Query.ToString());
                Reactor.AddParam("roomid", Client.GetUser().RoomId);
                Reactor.ExcuteQuery();
            }
        }

        private void DestroyRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int RoomId = Request.PopWiredInt32();

            VirtualRoom Room = BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);

            if (Room == null)
            {
                return;
            }

            if (!Room.GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                Client.Notif("You has not the rights to do this.", false);
                return;
            }

            BrickEngine.GetRoomReactor().DisposeRoom(Room.Id);

            // Cleanup Data
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM private_rooms WHERE id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.ExcuteQuery();
            }

            // Cleanup HomeRooms
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET home_room_id = '0' WHERE home_room_id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.ExcuteQuery();
            }

            // Cleanup Rights
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM private_rooms_rights WHERE room_id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.ExcuteQuery();
            }

            // Cleanup Rights
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM user_favorite_rooms WHERE room_id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.ExcuteQuery();
            }

            // Cleanup Items
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET room_id = '0' WHERE room_id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.ExcuteQuery();
            }
        }

        private void GiveRights(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            int HabboId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            if (Client.GetUser().GetRoom().GetRoomEngine().HasRights(HabboId, Rooms.RightsType.Rights))
            {
                return;
            }

            Client.GetUser().GetRoom().GetRoomEngine().AddRights(HabboId, RightsType.Rights);

            Response Response = new Response(510);
            Response.AppendInt32(Client.GetUser().RoomId);
            Response.AppendInt32(HabboId);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(HabboId));
            Client.SendResponse(Response);

            TriggeredUser.GetClient().SendResponse(new Response(42));

            TriggeredUser.AddStatus("flatctrl", "");
            TriggeredUser.UpdateStatus(true);

            // Doing Querys
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO private_rooms_rights (user_id, room_id) VALUES (@habboid, @roomid)");
                Reactor.AddParam("habboid", HabboId);
                Reactor.AddParam("roomid", Client.GetUser().RoomId);
                Reactor.ExcuteQuery();
            }
        }

        private void TakeRights(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            int RemoveAmount = Request.PopWiredInt32();

            for (int i = 0; i < RemoveAmount; i++)
            {
                int HabboId = Request.PopWiredInt32();

                VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

                if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(HabboId, Rooms.RightsType.Rights))
                {
                   return;
                }

                Client.GetUser().GetRoom().GetRoomEngine().RemoveRights(HabboId);

                Response Response = new Response(511);
                Response.AppendInt32(Client.GetUser().RoomId);
                Response.AppendInt32(HabboId);
                Client.SendResponse(Response);


                if (TriggeredUser != null)
                {
                    TriggeredUser.GetClient().SendResponse(new Response(43));

                    TriggeredUser.RemoveStatus("flatctrl");
                    TriggeredUser.UpdateStatus(true);
                }

                // Doing Querys
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("DELETE FROM private_rooms_rights WHERE user_id = @habboid AND room_id = @roomid LIMIT 1");
                    Reactor.AddParam("habboid", HabboId);
                    Reactor.AddParam("roomid", Client.GetUser().RoomId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        private void TakeAllRights(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                return;
            }

            foreach (int HabboId in Client.GetUser().GetRoom().GetRoomEngine().GetUsersWithRights())
            {
                VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

                if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(HabboId, Rooms.RightsType.Rights))
                {
                    return;
                }

                Client.GetUser().GetRoom().GetRoomEngine().RemoveRights(HabboId);

                Response Response = new Response(511);
                Response.AppendInt32(Client.GetUser().RoomId);
                Response.AppendInt32(HabboId);
                Client.SendResponse(Response);

                if (TriggeredUser != null)
                {
                    TriggeredUser.GetClient().SendResponse(new Response(43));

                    TriggeredUser.RemoveStatus("flatctrl");
                    TriggeredUser.UpdateStatus(true);
                }

                // Doing Querys
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("DELETE FROM private_rooms_rights WHERE user_id = @habboid AND room_id = @roomid LIMIT 1");
                    Reactor.AddParam("habboid", HabboId);
                    Reactor.AddParam("roomid", Client.GetUser().RoomId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        private void KickUser(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Rights))
            {
                return;
            }

            int HabboId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            if (Client.GetUser().GetRoom().GetRoomEngine().HasRights(HabboId, Rooms.RightsType.Founder))
            {
                Client.Notif("You can't kick the room owner.", false);
                return;
            }

            if (TriggeredUser.GetClient().GetUser().Rank > 1)
            {
                Client.Notif("You can't kick staff.", false);
                return;
            }

            Response Kick = new Response(33);
            Kick.AppendInt32(4008);
            TriggeredUser.GetClient().SendResponse(Kick);

            TriggeredUser.WalkFreezed = true;
            TriggeredUser.UnhandledGoalPoint = Client.GetUser().GetRoom().GetRoomModel().Door;
        }

        private void BanUser(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Rights))
            {
                return;
            }

            int HabboId = Request.PopWiredInt32();

            VirtualRoomUser TriggeredUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByHabboId(HabboId);

            if (TriggeredUser == null)
            {
                return;
            }

            if (Client.GetUser().GetRoom().GetRoomEngine().HasRights(HabboId, Rooms.RightsType.Founder))
            {
                Client.Notif("You can't ban the room owner.", false);
                return;
            }

            if (TriggeredUser.GetClient().GetUser().Rank > 1)
            {
                Client.Notif("You can't ban staff.", false);
                return;
            }

            Response Kick = new Response(33);
            Kick.AppendInt32(4008);
            TriggeredUser.GetClient().SendResponse(Kick);

            Client.GetUser().GetRoom().GetRoomEngine().HandleNewBan(HabboId);

            TriggeredUser.WalkFreezed = true;
            TriggeredUser.UnhandledGoalPoint = Client.GetUser().GetRoom().GetRoomModel().Door;
        }

        private void RateRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room voting FTW
                return;
            }

            if (Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                // Can't rate your own room?
                return;
            }

            if (Client.GetUser().VotedRooms.Contains(Client.GetUser().GetRoom().Id))
            {
                // Aleady rated.
                return;
            }

            Client.GetUser().VotedRooms.Add(Client.GetUser().GetRoom().Id);

            Client.GetUser().GetRoom().Rating++;

            Response Response = new Response(345);
            Response.AppendInt32(Client.GetUser().GetRoom().Rating);
            Client.SendResponse(Response);

            // First Response than Querys to avoid laggs.
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE private_rooms SET rating = rating + 1 WHERE id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", Client.GetUser().RoomId);
                Reactor.ExcuteQuery();
            }

            BrickEngine.GetStreamHandler().AddStream(Client.GetUser().HabboId, Users.Handlers.Messenger.Streaming.StreamType.RatedRoom, Client.GetUser().GetRoom().Id, Client.GetUser().GetRoom().Name);
        }

        private void MoveItemToRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                // Only Place in your own room.
                return;
            }

            int LimitX = Client.GetUser().GetRoom().GetRoomModel().XLength;
            int LimitY = Client.GetUser().GetRoom().GetRoomModel().YLength;

            int RoomId = Client.GetUser().RoomId;

            if (RoomId <= 0)
            {
                return;
            }

            // Get Point + Item Info.
            string PointInfo = Request.PopFixedString();

            // Split them in a array.
            string[] InfoSplit = PointInfo.Split(' ');

            // Gain ItemId & Verify
            int ItemId = BrickEngine.GetConvertor().ObjectToInt32(InfoSplit[0]);

            if (ItemId <= 0)
            {
                return;
            }

            VirtualRoomEngine RoomEngine = BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive).GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            if (InfoSplit[1].Contains(':'))
            {
                string Verify = RoomEngine.VerifyWallPosition(PointInfo.Substring(Array.IndexOf(PointInfo.ToCharArray(), ':')));

                if (string.IsNullOrEmpty(Verify) || !Verify.StartsWith(":"))
                {
                    return;
                }

                // Gain Item & Verify
                Item Item = null;

                if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
                {
                    return;
                }

                Item.RoomId = RoomId;
                Item.WallPoint = Verify;

                RoomEngine.HandleIncomingItem(Item, Verify, Client.GetUser().GetRoomUser());
            }
            else
            {
                // Gain Point X & Verify
                int PointX = BrickEngine.GetConvertor().ObjectToInt32(InfoSplit[1]);

                if (PointX < 0 || PointX >= LimitX)
                {
                    return;
                }

                // Gain Point Y & Verify
                int PointY = BrickEngine.GetConvertor().ObjectToInt32(InfoSplit[2]);

                if (PointY < 0 || PointY >= LimitY)
                {
                    return;
                }

                // Gain Item Rot & Verify
                int ItemRot = BrickEngine.GetConvertor().ObjectToInt32(InfoSplit[3]);

                if (ItemRot < 0 || ItemRot > 6)
                {
                    return;
                }

                // Gain Item & Verify
                Item Item = null;

                if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
                {
                    return;
                }

                // Already in a room
                if (Item.Place.Equals(ItemPlace.Room))
                {
                    return;
                }

                iPoint NewPlace = new iPoint(PointX, PointY);

                if (RoomEngine.LinkedPoint(Item, NewPlace, ItemRot))
                {
                    return;
                }

                Double PointZ = RoomEngine.GetTileHeight(NewPlace, Item.GetBaseItem().LengthZ, ItemId);

                if (PointZ < 0.0)
                {
                    PointZ = RoomEngine.GetTileHeight(NewPlace);
                }

                NewPlace.Z = PointZ;

                // Update Info & Cache
                Item.Point = NewPlace;
                Item.Rotation = ItemRot;
                Item.RoomId = RoomId;

                RoomEngine.HandleIncomingItem(Item, NewPlace, ItemRot, Client.GetUser().GetRoomUser());
            }

            Response Response = new Response(99);
            Response.AppendInt32(ItemId);
            Client.SendResponse(Response);
        }

        private void UpdateWallItem(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Rights))
            {
                // Only Update with rights.
                return;
            }

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            // Gain Item & Verify
            Item Item = null;

            if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
            {
                return;
            }

            // Still in inventory!?
            if (Item.Place.Equals(ItemPlace.Inventory))
            {
                return;
            }

            string WallPos = Request.PopFixedString();

            VirtualRoomEngine RoomEngine = Client.GetUser().GetRoom().GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            string Verify = RoomEngine.VerifyWallPosition(WallPos);

            if (string.IsNullOrEmpty(Verify) || !Verify.StartsWith(":"))
            {
                return;
            }

            Item.WallPoint = Verify;

            RoomEngine.HandleIncomingItemUpdate(Item, Verify, Client.GetUser().GetRoomUser());
        }

        private void UpdateFloorItem(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Rights))
            {
                // Only Update with rights.
                return;
            }

            int LimitX = Client.GetUser().GetRoom().GetRoomModel().XLength;
            int LimitY = Client.GetUser().GetRoom().GetRoomModel().YLength;

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            int PointX = Request.PopWiredInt32();

            if (PointX < 0 || PointX >= LimitX)
            {
                return;
            }

            int PointY = Request.PopWiredInt32();

            if (PointY < 0 || PointY >= LimitY)
            {
                return;
            }

            int ItemRot = Request.PopWiredInt32();

            if (ItemRot < 0 || ItemRot > 6)
            {
                return;
            }

            // Gain Item & Verify
            Item Item = null;

            if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
            {
                return;
            }

            // Still in inventory!?
            if (Item.Place.Equals(ItemPlace.Inventory))
            {
                return;
            }

            VirtualRoomEngine RoomEngine = Client.GetUser().GetRoom().GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            int OldRotation = Item.Rotation;

            iPoint OldPlace = Item.Point;
            iPoint NewPlace = new iPoint(PointX, PointY);

            if (RoomEngine.LinkedPoint(Item, NewPlace, ItemRot))
            {
                return;
            }

            Double PointZ = RoomEngine.GetTileHeight(NewPlace, Item.GetBaseItem().LengthZ, ItemId);

            if (PointZ < 0.0)
            {
                PointZ = RoomEngine.GetTileHeight(NewPlace);
            }

            NewPlace.Z = PointZ;

            // Update Info & Cache
            Item.Point = NewPlace;
            Item.Rotation = ItemRot;

            RoomEngine.HandleIncomingItemUpdate(Item, OldPlace, NewPlace, OldRotation, ItemRot, Client.GetUser().GetRoomUser());
        }

        private void PickUpItem(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                // Only Pickup as owner.
                return;
            }

            // Avoid junk
            Request.PopWiredInt32();

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            // Gain Item & Verify
            Item Item = null;

            if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
            {
                return;
            }

            // Already moved =)
            if (Item.Place.Equals(ItemPlace.Inventory))
            {
                return;
            }

            VirtualRoomEngine RoomEngine = Client.GetUser().GetRoom().GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            Item.RoomId = 0;

            if (Item.GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                int OldRotation = Item.Rotation;

                iPoint OldPlace = Item.Point;
                iPoint NewPlace = new iPoint(-1, -1, 0.0);

                int ItemRot = 0;

                Item.Rotation = ItemRot;
                Item.Point = NewPlace;

                RoomEngine.HandleIncomingItemPickUp(Item, OldPlace, NewPlace, OldRotation, ItemRot, Client.GetUser().GetRoomUser());
            }
            else
            {
                RoomEngine.HandleIncomingItemPickUp(Item, Client.GetUser().GetRoomUser());
            }

            Client.SendResponse(new Response(101));
        }

        private void TriggerSelectedItem(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room triggering FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Rights))
            {
                // Only Trigger with rights
                return;
            }

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            // Gain Item & Verify
            Item Item = null;

            if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
            {
                return;
            }

            // Already moved =)
            if (Item.Place.Equals(ItemPlace.Inventory))
            {
                return;
            }

            VirtualRoomEngine RoomEngine = Client.GetUser().GetRoom().GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            RoomEngine.HandleIncomingTrigger(Client.GetUser().GetRoomUser(), Item, Request.PopWiredInt32());
        }

        private void UpdateRoomLayout(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Rights))
            {
                return;
            }

            int ItemId = Request.PopWiredInt32();

            if (ItemId <= 0)
            {
                return;
            }

            // Gain Item & Verify
            Item Item = null;

            if ((Item = BrickEngine.GetItemReactor().GetItem(ItemId)) == null)
            {
                return;
            }

            VirtualRoomEngine RoomEngine = Client.GetUser().GetRoom().GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            RoomEngine.UpdateLayout(Client, Item);
        }

        #region Pets        
        private void MovePetToRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                // Only Place in your own room.
                return;
            }

            int PetId = Request.PopWiredInt32();

            if (PetId <= 0)
            {
                return;
            }

            PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(PetId);

            if (Info == null)
            {
                return;
            }

            if (Info.UserId != Client.GetUser().HabboId)
            {
                return;
            }

            if (Client.GetUser().GetRoom().GetRoomEngine().GetPets().Count >= PetReactor.MAX_PETS_PER_ROOM)
            {
                Client.Notif(string.Format("A room can only contains {0} pets.",PetReactor.MAX_PETS_PER_ROOM), false);
                return;
            }

            int LimitX = Client.GetUser().GetRoom().GetRoomModel().XLength;
            int LimitY = Client.GetUser().GetRoom().GetRoomModel().YLength;

            int X = Request.PopWiredInt32();

            if (X < 0 || X >= LimitX)
            {
                return;
            }

            int Y = Request.PopWiredInt32();

            if (Y < 0 || Y >= LimitY)
            {
                return;
            }

            iPoint Place = new iPoint(X, Y);

            VirtualRoomEngine RoomEngine = BrickEngine.GetRoomReactor().GetVirtualRoom(Client.GetUser().RoomId, RoomRunningState.Alive).GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            Info.RoomId = Client.GetUser().RoomId;

            int Rot = Rotation.Calculate(Place, Client.GetUser().GetRoomUser().Point);

            RoomEngine.GenerateRoomPet(PetId, Place, Rot);

            Response RemoveMessage = new Response(604);
            RemoveMessage.AppendInt32(PetId);
            Client.SendResponse(RemoveMessage);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_pets SET room_id = @roomid WHERE id = @petid LIMIT 1");
                Reactor.AddParam("roomid", Client.GetUser().RoomId);
                Reactor.AddParam("petid", PetId);
                Reactor.ExcuteQuery();
            }
        }

        private void MovePetToInventory(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                // Only Place in your own room.
                return;
            }

            int PetId = Request.PopWiredInt32();

            if (PetId <= 0)
            {
                return;
            }

            PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(PetId);

            if (Info == null)
            {
                return;
            }

            if (Info.UserId != Client.GetUser().HabboId)
            {
                return;
            }

            VirtualRoomEngine RoomEngine = BrickEngine.GetRoomReactor().GetVirtualRoom(Client.GetUser().RoomId, RoomRunningState.Alive).GetRoomEngine();

            if (RoomEngine == null)
            {
                return;
            }

            Info.RoomId = -1;

            RoomEngine.RemovePet(PetId);

            Response AddMessage = new Response(603);
            Info.GetInventoryResponse(AddMessage);
            Client.SendResponse(AddMessage);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_pets SET room_id = @roomid WHERE id = @petid LIMIT 1");
                Reactor.AddParam("roomid", -1);
                Reactor.AddParam("petid", PetId);
                Reactor.ExcuteQuery();
            }
        }

        private void GetPetInfo(Client Client, Request Request) // 3001
        {
            if (!Client.GetUser().IsInRoom)
            {
                // Outside room placing FTW.
                return;
            }

            int PetId = Request.PopWiredInt32();

            if (PetId <= 0)
            {
                return;
            }

            PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(PetId);

            if (Info == null)
            {
                return;
            }

            Client.SendResponse(Info.GetInfoResponse());
        }
        #endregion
    }
}
