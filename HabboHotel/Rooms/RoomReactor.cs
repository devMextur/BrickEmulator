using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Rooms
{
    enum RoomRunningState
    {
        Dead,
        Alive,
        NeedsUnload
    }

    enum RoomEnterMessage
    {
        Accepted,
        LimitUsers,
        Banned,
        NoAnswer,
        WrongPassword
    }

    class RoomReactor
    {
        private Dictionary<string, RoomModel> RoomModels;
        private Dictionary<int, VirtualRoom> RunningRooms;

        private Security.SecurityCounter RoomIdCounter;

        public RoomReactor() { }

        public void Prepare()
        {
            RunningRooms = new Dictionary<int, VirtualRoom>();

            LoadRoomModels();

            // Cached id Generator
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT MAX(id) FROM private_rooms LIMIT 1");
                RoomIdCounter = new Security.SecurityCounter(Reactor.GetInt32());
            }
        }

        public void LoadRoomModels()
        {
            RoomModels = new Dictionary<string, RoomModel>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM room_models ORDER BY id ASC");
                Table = Reactor.GetTable();
            }

            if (!Table.Equals(null))
            {
                foreach (DataRow Row in Table.Rows)
                {
                    RoomModel Model = new RoomModel(Row);

                    RoomModels.Add(Model.Id, Model);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + RoomModels.Count + "] RoomModel(s) cached.", IO.WriteType.Outgoing);
        }

        public void DisposeRoom(int Id)
        {
            if (RunningRooms.ContainsKey(Id))
            {
                RunningRooms[Id].Dispose();

                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Green, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("[" + Id + "] Room disposed succesfully.", IO.WriteType.Outgoing);

                RunningRooms.Remove(Id);
            }
        }

        public Boolean RoomIsAlive(int Id)
        {
            return RunningRooms.ContainsKey(Id);
        }

        public VirtualRoom GetVirtualRoom(int Id, RoomRunningState State)
        {
            if (RunningRooms.ContainsKey(Id))
            {
                return RunningRooms[Id];
            }
            else
            {
                DataRow Row = null;

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT * FROM private_rooms WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    Row = Reactor.GetRow();
                }

                if (Row != null)
                {
                    VirtualRoom GeneratedRoom = new VirtualRoom(Row);

                    if (State.Equals(RoomRunningState.Alive))
                    {
                        RunningRooms.Add(GeneratedRoom.Id, GeneratedRoom);
                        GeneratedRoom.Activate();

                        BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Green, IO.PaintType.ForeColor);
                        BrickEngine.GetScreenWriter().ScretchLine("[" + GeneratedRoom.Id + "] Room cached.", IO.WriteType.Outgoing);
                    }

                    return GeneratedRoom;
                }

                return null;
            }
        }

        public int CreateRoom(Client Client, string RawName, string RawModel)
        {
            string FixedName = BrickEngine.CleanString(RawName);
            string FixedModel = BrickEngine.CleanString(RawModel);

            if (GetRoomModel(FixedModel) == null)
            {
                Client.Notif("RoomModel: " + FixedModel + " could not found in cache, try another.", false);
                return -1;
            }

            if (FixedName.Length < 3)
            {
                Client.Notif("The name you typed in was to short, try again.",false);
                return -1;
            }

            if (FixedName.Length > 25)
            {
                Client.Notif("The name you typed in was to long, try again.", false);
                return -1;
            }

            int GeneratedId = RoomIdCounter.Next;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO private_rooms (id, name, owner_id, model_param) VALUES (@roomid, @name, @ownerid, @model)");
                Reactor.AddParam("roomid", GeneratedId);
                Reactor.AddParam("name", FixedName);
                Reactor.AddParam("ownerid", Client.GetUser().HabboId);
                Reactor.AddParam("model", FixedModel);
                Reactor.ExcuteQuery();
            }

            return GeneratedId;
        }

        public int CreateRoom(Client Client, string RawName, string RawModel, string Description, string Wallpaper, string Floor)
        {
            string FixedName = BrickEngine.CleanString(RawName);
            string FixedModel = BrickEngine.CleanString(RawModel);

            if (GetRoomModel(FixedModel) == null)
            {
                Client.Notif("RoomModel: " + FixedModel + " could not found in cache, try another.", false);
                return -1;
            }

            if (FixedName.Length < 3)
            {
                Client.Notif("The name you typed in was to short, try again.", false);
                return -1;
            }

            int GeneratedId = RoomIdCounter.Next;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO private_rooms (id, name, description, owner_id, model_param, wallpaper, floor) VALUES (@roomid, @name, @desc, @ownerid, @model, @wallpaper, @floor)");
                Reactor.AddParam("roomid", GeneratedId);
                Reactor.AddParam("name", FixedName);
                Reactor.AddParam("desc", Description);
                Reactor.AddParam("ownerid", Client.GetUser().HabboId);
                Reactor.AddParam("model", FixedModel);
                Reactor.AddParam("wallpaper", Wallpaper);
                Reactor.AddParam("floor", Floor);
                Reactor.ExcuteQuery();
            }

            return GeneratedId;
        }

        public RoomModel GetRoomModel(string Param)
        {
            try { return RoomModels[Param]; }
            catch { return null; }
        }

        public int Random(int CurrentRoom, int HabboId)
        {
            var List = (from kvp in RunningRooms where !kvp.Value.OwnerId.Equals(HabboId) where !kvp.Key.Equals(CurrentRoom) where !kvp.Value.RoomUserAmount.Equals(0) select kvp.Key).ToList();

            if (List.Count <= 0)
            {
                return 0;
            }

            return List[BrickEngine.Random.Next(0, List.Count - 1)];
        }

        public List<VirtualRoom> GetMe(int HabboId)
        {
            var List = new List<VirtualRoom>();

            DataTable TableIds = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT id FROM private_rooms WHERE owner_id = @habboid ORDER BY id ASC");
                Reactor.AddParam("habboid", HabboId);
                TableIds = Reactor.GetTable();
            }

            if (TableIds != null)
            {
                foreach (DataRow Row in TableIds.Rows)
                {
                    int Id = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);

                    List.Add(GetVirtualRoom(Id, RoomRunningState.Dead));
                }
            }

            return List;
        }

        public List<VirtualRoom> GetHighestScore()
        {
            var List = new List<VirtualRoom>();

            DataTable TableIds = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT id FROM private_rooms WHERE rating > 0 ORDER BY rating DESC LIMIT 50");
                TableIds = Reactor.GetTable();
            }

            if (TableIds != null)
            {
                foreach (DataRow Row in TableIds.Rows)
                {
                    int Id = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);

                    List.Add(GetVirtualRoom(Id, RoomRunningState.Dead));
                }
            }

            return List;
        }

        public List<VirtualRoom> GetVirtualRooms()
        {
            var List = new List<VirtualRoom>();

            foreach (VirtualRoom Room in RunningRooms.Values.ToList())
            {
                if (Room.RoomUserAmount > 0)
                {
                    List.Add(Room);
                }
            }

            return List;
        }

        public List<VirtualRoom> GetVirtualRooms(int Category)
        {
            var FromCategory = new List<VirtualRoom>();

            if (Category > -1)
            {
                foreach (VirtualRoom Room in GetVirtualRooms())
                {
                    if (Room.RoomUserAmount > 0)
                    {
                        if (Room.CategoryId.Equals(Category))
                        {
                            FromCategory.Add(Room);
                        }
                    }
                }
            }
            else
            {
                FromCategory = GetVirtualRooms();
            }

            return (from room in FromCategory orderby room.RoomUserAmount descending select room).ToList();
        }

        public List<VirtualRoom> GetEventRooms(int Category)
        {
            var FromCategory = new List<VirtualRoom>();

            if (Category > -1)
            {
                foreach (VirtualRoom Room in GetVirtualRooms())
                {
                    if (Room.Event != null)
                    {
                        if (Room.Event.CategoryId == Category)
                        {
                            FromCategory.Add(Room);
                        }
                    }
                }
            }
            else
            {
                foreach (VirtualRoom Room in GetVirtualRooms())
                {
                    if (Room.Event != null)
                    {
                        FromCategory.Add(Room);
                    }
                }
            }

            return (from room in FromCategory orderby room.Id descending select room).ToList();
        }

        public List<string> GetPopulairTags()
        {
            var Tags = new List<string>();

            foreach (VirtualRoom Room in GetVirtualRooms())
            {
                if (Room.RoomUserAmount > 0)
                {
                    foreach (string Tag in Room.Tags)
                    {
                        if (!Tags.Contains(Tag.ToLower()) && Tags.Count < 50)
                        {
                            Tags.Add(Tag.ToLower());
                        }
                    }
                }
            }

            return Tags;
        }

        public List<VirtualRoom> GetFavoriteRooms(Client Client)
        {
            var List = new List<VirtualRoom>();

            foreach (int RoomId in Client.GetUser().FavoriteRoomIds)
            {
                List.Add(GetVirtualRoom(RoomId, RoomRunningState.Dead));
            }

            return List;
        }

        public List<VirtualRoom> GetLastVisited(Client Client)
        {
            var List = new List<VirtualRoom>();

            foreach (RoomVisit Visit in Client.GetUser().VisitedRooms)
            {
                List.Add(GetVirtualRoom(Visit.RoomId, RoomRunningState.Dead));
            }

            return List;
        }

        public List<VirtualRoom> GetSearchRooms(string Query)
        {
            var List = new Dictionary<int, VirtualRoom>();

            DataTable TableIds = null;

            if (Query != null)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                { //Done :D
                    Reactor.SetQuery("SELECT private_rooms.id FROM private_rooms INNER JOIN users ON users.id = private_rooms.owner_id WHERE private_rooms.name LIKE @query OR private_rooms.tags LIKE @query OR users.username LIKE @query ORDER BY private_rooms.id ASC LIMIT 50 ");
                    Reactor.AddParam("query","%" + Query + "%");
                    TableIds = Reactor.GetTable();
                }

                if (TableIds != null)
                {
                    foreach (DataRow Row in TableIds.Rows)
                    {
                        int Id = BrickEngine.GetConvertor().ObjectToInt32(Row[0]);

                        if (!List.ContainsKey(Id) && List.Count < 50)
                        {
                            List.Add(Id, GetVirtualRoom(Id, RoomRunningState.Dead));
                        }
                    }
                }
            }

            return List.Values.ToList();
        }

        public string GetRoomName(int Id)
        {
            if (RunningRooms.ContainsKey(Id))
            {
                return RunningRooms[Id].Name;
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT name FROM private_rooms WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetString();
                }
            }
        }


        public int GetRoomCategory(int Id)
        {
            if (RunningRooms.ContainsKey(Id))
            {
                return RunningRooms[Id].CategoryId;
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT category_id FROM private_rooms WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetInt32();
                }
            }
        }
    }
}
