using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.Messages;
using System.Data;
using BrickEmulator.Storage;
using System.Threading;
using BrickEmulator.HabboHotel.Furni.Items;
using System.Threading.Tasks;
using BrickEmulator.HabboHotel.Pets;

namespace BrickEmulator.HabboHotel.Rooms
{
    enum RightsType
    {
        Rights,
        Founder
    }

    /// <summary>
    /// Machine that runs the alive rooms.
    /// </summary>
    class VirtualRoomEngine : IDisposable
    {
        #region Fields

        private int SuppressAlive = 0;
        private Random Random = new Random();

        private readonly int RoomId;

        private double[,] HeightMap;
        private TileState[,] Matrix;

        private SecurityCounter VirtualIdCounter = new SecurityCounter(-1);
        private Dictionary<int, VirtualRoomUser> RoomUsers = new Dictionary<int, VirtualRoomUser>();
        private Dictionary<int, VirtualRoomUser> RoomPets = new Dictionary<int, VirtualRoomUser>();
        private Dictionary<int, DateTime> Bans = new Dictionary<int, DateTime>();

        private Dictionary<int, RightsType> Rights = new Dictionary<int, RightsType>();

        private Timer UserHandler;
        private Timer PetsHandler;

        public int UnitCount
        {
            get
            {
                return RoomUsers.Count + RoomPets.Count;
            }
        }

        public int RoomUserAmount
        {
            get
            {
                return RoomUsers.Count;
            }
        }

        public VirtualRoomEngine(int RoomId)
        {
            this.RoomId = RoomId;

            LoadRights();

            AddRights(GetRoom().OwnerId, RightsType.Founder);
        }

        #endregion

        #region Methods

        #region Startup
        public void Prepare()
        {
            UserHandler = new Timer(new TimerCallback(HandleUsers), UserHandler, 0, 500);
            PetsHandler = new Timer(new TimerCallback(HandlePets), PetsHandler, 100, 500);

            GenerateMatrix();

            foreach (PetInfo Info in BrickEngine.GetPetReactor().GetPetsForRoom(RoomId))
            {
                int Rot = Info.Rot;

                iPoint Place = new iPoint(Info.X, Info.Y);

                if (!Place.Compare(GetRoom().GetRoomModel().Door))
                {
                    if ((Place.X <= 0 && Place.Y <= 0) || Place.X >= GetRoom().GetRoomModel().XLength || Place.Y >= GetRoom().GetRoomModel().YLength)
                    {
                        Place = GetRoom().GetRoomModel().Door;
                    }

                    if (!Matrix[Place.X, Place.Y].Equals(TileState.Walkable) || !CanWalk(Place))
                    {
                        Place = GetRoom().GetRoomModel().Door;
                    }
                }

                if (Rot < 0)
                {
                    Rot = GetRoom().GetRoomModel().DoorRot;
                }

                GenerateRoomPet(Info.Id, Place, Rot);
            }
        }
        #endregion

        #region Users

        public List<VirtualRoomUser> GetUsers()
        {
            return RoomUsers.Values.ToList();
        }

        public List<VirtualRoomUser> GetPets()
        {
            return RoomPets.Values.ToList();
        }

        public VirtualRoomUser GenerateRoomPet(int PetId, iPoint Place, int Rot)
        {
            VirtualRoomUser User = new VirtualRoomUser(VirtualIdCounter.Next, PetId, RoomId, Rot);
            User.UpdatePoint(Place);

            User.Point.Z = GetTileHeight(Place);

            Response EnterMessage = new Response(28);
            EnterMessage.AppendInt32(1); // Amount, just 1
            User.GetResponse(EnterMessage);
            BroadcastResponse(EnterMessage);

            lock (RoomPets)
            {
                RoomPets.Add(User.VirtualId, User);
            }

            BrickEngine.GetPetReactor().GetPetInfo(PetId).X = Place.X;
            BrickEngine.GetPetReactor().GetPetInfo(PetId).Y = Place.Y;
            BrickEngine.GetPetReactor().GetPetInfo(PetId).Rot = Rot;

            return User;
        }

        public void RemovePet(int PetId)
        {
            VirtualRoomUser User = GetUserByPetId(PetId);

            if (User == null)
            {
                return;
            }

            int VirtualId = User.VirtualId;

            lock (RoomPets)
            {
                RoomPets.Remove(VirtualId);
            }

            Response LeaveMessage = new Response(29);
            LeaveMessage.AppendRawInt32(VirtualId);
            BroadcastResponse(LeaveMessage);

            BrickEngine.GetPetReactor().GetPetInfo(PetId).X = -1;
            BrickEngine.GetPetReactor().GetPetInfo(PetId).Y = -1;

            BrickEngine.GetProgressReactor().GetCollector().Finialize(User);
        }

        public VirtualRoomUser GenerateRoomUser(Client Client)
        {
            if (!Client.IsValidUser)
            {
                return null;
            }

            VirtualRoomUser User = new VirtualRoomUser(VirtualIdCounter.Next, Client.GetUser().HabboId, RoomId, GetRoom().GetRoomModel().DoorRot);
            User.UpdatePoint(GetRoom().GetRoomModel().Door);

            Response EnterMessage = new Response(28);
            EnterMessage.AppendInt32(1); // Amount, just 1
            User.GetResponse(EnterMessage);
            BroadcastResponse(EnterMessage);

            User.GetClient().GetUser().AtEnterRoom(RoomId);

            lock (RoomUsers)
            {
                RoomUsers.Add(User.VirtualId, User);
            }

            return User;
        }

        public void RemoveUser(int HabboId)
        {
            VirtualRoomUser User = GetUserByHabboId(HabboId);

            if (User == null)
            {
                return;
            }

            int VirtualId = User.VirtualId;

            lock (RoomUsers)
            {
                RoomUsers.Remove(VirtualId);
            }

            Response LeaveMessage = new Response(29);
            LeaveMessage.AppendRawInt32(VirtualId);
            BroadcastResponse(LeaveMessage);

            BrickEngine.GetProgressReactor().GetCollector().Finialize(User);
        }

        public void HandleLeaveUser(int HabboId, bool Notify)
        {
            VirtualRoomUser User = GetUserByHabboId(HabboId);

            if (User == null)
            {
                return;
            }

            RemoveUser(HabboId);

            try
            {
                User.GetClient().GetUser().AtLeaveRoom(RoomId);
            }
            catch { }

            if (Notify)
            {
                Response Response = new Response(700);
                Response.AppendBoolean(false);

                try
                {
                    User.GetClient().SendResponse(Response);
                }
                catch { }
            }

            try
            {
                BrickEngine.GetPacketHandler().ClearLoading(User.GetClient(), Notify);
            }
            catch { }
        }

        public Boolean CheckPetCommand(Client Client, string Message)
        {
            if (Message.Contains(' '))
            {
                string PetName = Message.Split(' ')[0];
                string Command = Message.Substring(PetName.Length + 1);

                VirtualRoomUser Pet = GetUserByPetName(PetName);

                if (Pet == null)
                {
                    return false;
                }

                PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(Pet.HabboId);

                if (Info == null)
                {
                    return false;
                }

                if (Info.UserId != Client.GetUser().HabboId)
                {
                    return false;
                }

                BrickEngine.GetPetReactor().CommandHandler.Interact(Client, Info, Command.ToLower());

                return true;
            }

            return false;
        }

        public VirtualRoomUser GetUserByPetName(string PetName)
        {
            foreach (VirtualRoomUser User in GetPets())
            {
                if (BrickEngine.GetPetReactor().GetPetInfo(User.HabboId).Name.ToLower().Equals(PetName.ToLower()))
                {
                    return User;
                }
            }

            return null;
        }

        public VirtualRoomUser GetUserByPetId(int Id)
        {
            foreach (VirtualRoomUser User in GetPets())
            {
                if (User.HabboId == Id)
                {
                    return User;
                }
            }

            return null;
        }

        public VirtualRoomUser GetUserByHabboId(int Id)
        {
            foreach (VirtualRoomUser User in GetUsers())
            {
                if (User.HabboId == Id)
                {
                    return User;
                }
            }

            return null;
        }

        public void UpdateUsersOnPoint(iPoint Point)
        {
            foreach (VirtualRoomUser User in GetUsers())
            {
                if (User.Point.Compare(Point))
                {
                    User.UpdateStatus(true);
                }
            }

            foreach (VirtualRoomUser Pet in GetPets())
            {
                if (Pet.Point.Compare(Point))
                {
                    Pet.UpdateStatus(true);
                }
            }
        }

        #endregion

        #region Bans

        public Boolean CheckBanned(int HabboId)
        {
            if (Bans.ContainsKey(HabboId))
            {
                if ((DateTime.Now - Bans[HabboId]).TotalMinutes >= 5)
                {
                    Bans.Remove(HabboId);

                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public void HandleNewBan(int HabboId)
        {
            if (Bans.ContainsKey(HabboId))
            {
                Bans.Remove(HabboId);
            }

            Bans.Add(HabboId, DateTime.Now);
        }

        #endregion

        #region Pathfinding

        public Boolean CanWalk(iPoint Point)
        {
            if (!GetRoom().AllowWalkthough)
            {
                foreach (VirtualRoomUser User in GetUsers())
                {
                    if (User.Point.Compare(Point))
                    {
                        return false;
                    }
                }

                foreach (VirtualRoomUser Pet in GetPets())
                {
                    if (Pet.Point.Compare(Point))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public iPoint GetRandom()
        {
            var PointList = new List<iPoint>();

            for (short y = 0; y < GetRoom().GetRoomModel().YLength; y++)
            {
                for (short x = 0; x < GetRoom().GetRoomModel().XLength; x++)
                {
                    TileState State = Matrix[x, y];

                    if (new iPoint(x, y).Compare(GetRoom().GetRoomModel().Door))
                    {
                        continue;
                    }

                    if (!State.Equals(TileState.Blocked) && CanWalk(new iPoint(x,y)))
                    {
                        PointList.Add(new iPoint(x, y));
                    }
                }
            }

            return PointList[Random.Next(0, PointList.Count - 1)];
        }

        public void GenerateMatrix()
        {
            this.Matrix = new TileState[GetRoom().GetRoomModel().XLength, GetRoom().GetRoomModel().YLength];
            this.HeightMap = new double[GetRoom().GetRoomModel().XLength, GetRoom().GetRoomModel().YLength];

            for (short y = 0; y < GetRoom().GetRoomModel().YLength; y++)
            {
                for (short x = 0; x < GetRoom().GetRoomModel().XLength; x++)
                {
                    HeightMap[x, y] = GetRoom().GetRoomModel().DefaultHeightMap[x, y];

                    iPoint Point = new iPoint(x, y);

                    if (GetFloorItemsOnTile(Point).Count > 0)
                    {
                        foreach (Item Item in GetFloorItemsOnTile(Point))
                        {
                            if (!Item.GetBaseItem().EnableSit)
                            {
                                HeightMap[x, y] += Item.GetBaseItem().LengthZ;
                            }

                            if (Item.GetBaseItem().EnableSit)
                            {
                                Matrix[x, y] = TileState.Walkable_laststep;
                            }
                            else if (Item.GetBaseItem().EnableWalk)
                            {
                                Matrix[x, y] = TileState.Walkable;
                            }
                            else
                            {
                                Matrix[x, y] = TileState.Blocked;
                            }
                        }
                    }
                    else
                    {
                        Matrix[x, y] = GetRoom().GetRoomModel().DefaultTiles[x, y];
                    }
                }
            }
        }

        private void HandlePets(Object Obj)
        {
            foreach (VirtualRoomUser Pet in GetPets())
            {
                PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(Pet.HabboId);

                Info.CollectStatus();

                PetInterAction Action = PetInterAction.Interactive;

                int Rand = Random.Next(1, 30);

                if (Rand == 2)
                {
                    Action = PetInterAction.Walking;
                }
                else if (Rand == 25)
                {
                    Action = PetInterAction.Chatting;
                }
                else if (Rand == 4)
                {
                    Action = PetInterAction.Playing;
                }
                else if (Rand == 5)
                {
                    Action = PetInterAction.Action;
                }

                Info.DoActions(Action, Pet);

                if (Pet.NeedsWalking)
                {
                    using (Pathfinder Pathfinder = new Pathfinder(RoomId, Matrix, HeightMap, GetRoom().GetRoomModel()))
                    {
                        var Route = Pathfinder.GeneratePath(Pet.Point, Pet.UnhandledGoalPoint);

                        Pet.UnhandledGoalPoint = new iPoint(-1, -1);

                        Pet.RemoveStatus("mv");
                        Pet.RemoveStatus("sit");
                        Pet.RemoveStatus("lay");

                        if (Route.Count > 0)
                        {
                            Pet.PathPoints = Route;
                        }
                        else
                        {
                            Pet.PathPoints = new List<iPoint>();
                            Pet.UpdateStatus(true);
                        }
                    }
                }

                if (Pet.NeedsLastWalk)
                {
                    Pet.UpdateStatus(true);
                    Pet.NeedsLastWalk = false;

                    if (Pet.Point.Compare(GetRoom().GetRoomModel().Door))
                    {
                        HandleLeaveUser(Pet.HabboId, true);
                    }
                }

                if (Pet.IsWalking)
                {
                    Pet.RemoveStatus("sit");
                    Pet.RemoveStatus("lay");

                    iPoint Next = Pet.PathPoints[0];

                    StringBuilder MoveBuilder = new StringBuilder();

                    MoveBuilder.Append(Next.X + ",");
                    MoveBuilder.Append(Next.Y + ",");
                    MoveBuilder.Append(Next.Z.ToString().Replace(",", "."));

                    Pet.AddStatus("mv", MoveBuilder.ToString());

                    Pet.PathPoints.Remove(Next);

                    int NewRot = Rotation.Calculate(Pet.Point.X, Pet.Point.Y, Next.X, Next.Y);

                    Pet.RotHead = NewRot;
                    Pet.RotBody = NewRot;

                    Pet.UpdateStatus(false);

                    Pet.UpdatePoint(Next);

                    if (Pet.PathPoints.Count <= 0)
                    {
                        Pet.UnhandledGoalPoint = new iPoint(-1, -1);
                        Pet.RemoveStatus("mv");
                        Pet.NeedsLastWalk = true;
                    }
                }
            }

            if (GetPetToys().Count > 0)
            {
                foreach (Item Toy in GetPetToys())
                {
                    if (Toy.ExtraData.Equals("1"))
                    {
                        int i = 0;

                        foreach (VirtualRoomUser Pet in GetPets())
                        {
                            if (Pet.Point.Compare(Toy.Point))
                            {
                                i++;
                            }
                        }

                        if (i == 0)
                        {
                            Toy.ExtraData = "0";
                            BroadcastResponse(Toy.GetTriggerResponse());
                        }
                    }
                }
            }
        }

        private void HandleUsers(Object Obj)
        {
            foreach (VirtualRoomUser User in GetUsers())
            {
                try
                {
                    User.GetClient();
                }
                catch { RemoveUser(User.HabboId); continue; }

                User.SuppressAlive++;

                if (User.NeedsWalking)
                {
                    User.UnSuppress();

                    if (User.Point.Compare(GetRoom().GetRoomModel().Door) && User.UnhandledGoalPoint.Compare(GetRoom().GetRoomModel().Door))
                    {
                        HandleLeaveUser(User.HabboId, true);
                        continue;
                    }

                    using (Pathfinder Pathfinder = new Pathfinder(RoomId, Matrix, HeightMap, GetRoom().GetRoomModel()))
                    {
                        var Route = Pathfinder.GeneratePath(User.Point, User.UnhandledGoalPoint);

                        User.RemoveStatus("mv");
                        User.RemoveStatus("sit");
                        User.RemoveStatus("lay");

                        if (Route.Count > 0)
                        {
                            User.PathPoints = Route;
                        }
                        else
                        {
                            User.PathPoints = new List<iPoint>();
                            User.UpdateStatus(true);
                        }
                    }
                }

                if (User.WalkFreezed && !User.PlayingGame)
                {
                    if (!User.IsWalking)
                    {
                        HandleLeaveUser(User.HabboId, true);
                        continue;
                    }
                }

                if (User.NeedsLastWalk)
                {
                    User.UpdateStatus(true);
                    User.NeedsLastWalk = false;

                    if (User.Point.Compare(GetRoom().GetRoomModel().Door))
                    {
                        HandleLeaveUser(User.HabboId, true);
                    }
                }

                if (User.IsWalking)
                {
                    User.RemoveStatus("sit");
                    User.RemoveStatus("lay");

                    iPoint Next = User.PathPoints[0];

                    StringBuilder MoveBuilder = new StringBuilder();

                    MoveBuilder.Append(Next.X + ",");
                    MoveBuilder.Append(Next.Y + ",");
                    MoveBuilder.Append(Next.Z.ToString().Replace(",", "."));

                    User.AddStatus("mv", MoveBuilder.ToString());

                    User.PathPoints.Remove(Next);

                    int NewRot = Rotation.Calculate(User.Point.X, User.Point.Y, Next.X, Next.Y);

                    User.RotHead = NewRot;
                    User.RotBody = NewRot;

                    User.UpdateStatus(false);

                    User.UpdatePoint(Next);

                    if (User.PathPoints.Count <= 0)
                    {
                        User.UnhandledGoalPoint = new iPoint(-1, -1);
                        User.RemoveStatus("mv");
                        User.NeedsLastWalk = true;
                    }
                }

                if (User.SuppressAlive >= 600 && !User.Suppressed)
                {
                    Response SuppressedResponse = new Response(486);
                    SuppressedResponse.AppendInt32(User.VirtualId);
                    SuppressedResponse.AppendBoolean(true);
                    BroadcastResponse(SuppressedResponse);

                    User.Suppressed = true;
                }

                if (User.SuppressAlive >= 1800 && User.Suppressed)
                {
                    HandleLeaveUser(User.HabboId, true);
                }
            }

            if (GetUsers().Count <= 0)
            {
                SuppressAlive++;
            }
            else
            {
                SuppressAlive = 0;
            }

            if (SuppressAlive >= 60)
            {
                BrickEngine.GetRoomReactor().DisposeRoom(RoomId);
            }
        }

        public double GetTileHeight(iPoint Point)
        {
            return HeightMap[Point.X, Point.Y];
        }

        public double GetTileHeight(iPoint Point, double Min, int ItemId)
        {
            if (GetFloorItemsOnTile(Point).Count <= 0)
            {
                Min = 0.0;
            }
            else
            {
                int i = 0;

                foreach (Item Item in GetFloorItemsOnTile(Point))
                {
                    if (Item.Id == ItemId)
                    {
                        i++;
                    }
                }

                if (i <= 0)
                {
                    Min = 0.0;
                }
            }

            return HeightMap[Point.X, Point.Y] - Min;
        }

        #endregion

        #region Rights

        public void LoadRights()
        {
            Rights = new Dictionary<int, RightsType>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM private_rooms_rights WHERE room_id = @roomid");
                Reactor.AddParam("roomid", RoomId);
                Table = Reactor.GetTable();
            }

            if (!Table.Equals(null))
            {
                foreach (DataRow Row in Table.Rows)
                {
                    int UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[1]);

                    if (!Rights.ContainsKey(UserId))
                    {
                        Rights.Add(UserId, RightsType.Rights);
                    }
                }
            }
        }

        public Boolean HasRights(int HabboId, RightsType Type)
        {
            if (Rights.ContainsKey(HabboId))
            {
                if (Type.Equals(RightsType.Founder))
                {
                    return Rights[HabboId].Equals(RightsType.Founder);
                }
                if (Type.Equals(RightsType.Rights))
                {
                    return Rights[HabboId].Equals(RightsType.Rights) || Rights[HabboId].Equals(RightsType.Founder);
                }
            }

            return false;
        }

        public void AddRights(int HabboId, RightsType Type)
        {
            Rights.Add(HabboId, Type);
        }

        public void RemoveRights(int HabboId)
        {
            Rights.Remove(HabboId);
        }

        public List<int> GetRights()
        {
            var List = new List<int>();

            foreach (KeyValuePair<int, RightsType> kvp in Rights)
            {
                if (kvp.Value.Equals(RightsType.Rights))
                {
                    if (!List.Contains(kvp.Key))
                    {
                        List.Add(kvp.Key);
                    }
                }
            }

            return List;
        }

        public List<int> GetUsersWithRights()
        {
            var List = new List<int>();

            foreach (KeyValuePair<int, RightsType> kvp in Rights)
            {
                if (kvp.Value.Equals(RightsType.Rights))
                {
                    List.Add(kvp.Key);
                }
            }

            return List;
        }

        #endregion

        #region Responses

        public void BroadcastResponse(Response Response)
        {
            foreach (VirtualRoomUser User in GetUsers())
            {
                if (User != null && BrickEngine.GetUserReactor().IsOnline(User.HabboId))
                {
                    if (User.Alive)
                    {
                        User.GetClient().SendResponse(Response);
                    }
                }
            }
        }

        public void BroadcastChatResponse(int SendHabboId, int VirtualId, int HeaderId, int Emoticon, string Message)
        {
            foreach (VirtualRoomUser User in GetUsers())
            {
                if (User.GetClient().GetUser().HasIgnoredUser(SendHabboId))
                {
                    continue;
                }

                if (User != null && BrickEngine.GetUserReactor().IsOnline(User.HabboId))
                {
                    if (User.Alive)
                    {
                        Response Response = new Response(HeaderId);
                        Response.AppendInt32(VirtualId);
                        Response.AppendStringWithBreak(BrickEngine.GetWordFilterHandler().FilterMessage(User.GetClient(), Message));
                        Response.AppendInt32(Emoticon);
                        Response.AppendBoolean(false);

                        User.GetClient().SendResponse(Response);
                    }
                }
            }
        }

        #endregion

        #region Items

        public List<Item> GetFoodItems()
        {
            var List = new List<Item>();

            foreach (Item Item in GetFloorItems())
            {
                if (Item.GetBaseItem().InternalName.ToLower().Contains("a0 pet") || Item.GetBaseItem().InternalName.ToLower().Contains("petfood"))
                {
                    int i = 0;

                    if (!int.TryParse(Item.ExtraData, out i))
                    {
                        continue;
                    }

                    if (i < 4)
                    {
                        List.Add(Item);
                    }
                }
            }

            return List;
        }

        public List<Item> GetPetNests()
        {
            var List = new List<Item>();

            foreach (Item Item in GetFloorItems())
            {
                if (Item.GetBaseItem().InternalName.ToLower().Contains("nest"))
                {
                    List.Add(Item);
                }
            }

            return List;
        }

        public List<Item> GetPetToys()
        {
            var List = new List<Item>();

            foreach (Item Item in GetFloorItems())
            {
                if (Item.GetBaseItem().InternalName.ToLower().Contains("ashtree") || Item.GetBaseItem().InternalName.ToLower().Contains("toy"))
                {
                    List.Add(Item);
                }
            }

            return List;
        }

        public Boolean LinkedPoint(Item Furni, iPoint NextPoint, int Rotation)
        {
            foreach (Item Item in GetFloorItemsOnTile(NextPoint))
            {
                if (Item.Id.Equals(Furni.Id))
                {
                    continue;
                }

                if (!Item.GetBaseItem().EnableStack)
                {
                    return true;
                }
            }

            foreach (iPoint Point in BrickEngine.GetItemReactor().GetAffectedPoints(NextPoint, Furni.GetBaseItem().LengthY, Furni.GetBaseItem().LengthX, Rotation))
            {
                foreach (Item Item in GetFloorItemsOnTile(Point))
                {
                    if (Item.Id.Equals(Furni.Id))
                    {
                        continue;
                    }

                    if (!Item.GetBaseItem().EnableStack)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #region Itemhandling

        public void HandleIncomingItem(Item Item, iPoint NewPlace, int Rotation, VirtualRoomUser User)  // Already Updated @cache
        {
            // Handling Messages (FIRST, to avoid laggys)
            Response Response = new Response(93);
            Item.GetRoomResponse(Response);
            BroadcastResponse(Response);

            GenerateMatrix();

            UpdateUsersOnPoint(NewPlace);

            foreach (iPoint Point in BrickEngine.GetItemReactor().GetAffectedPoints(NewPlace, Item.GetBaseItem().LengthY, Item.GetBaseItem().LengthX, Rotation))
            {
                UpdateUsersOnPoint(Point);
            }

            Item.GetTrigger().OnPlace(Item, User);

            // Update Info & MySQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET room_id = @roomid, point_x = @x, point_y = @y, point_z = @z, rotation = @rot WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.AddParam("itemid", Item.Id);
                Reactor.AddParam("x", NewPlace.X);
                Reactor.AddParam("y", NewPlace.Y);
                Reactor.AddParam("z", NewPlace.Z.ToString().Replace(',', '.'));
                Reactor.AddParam("rot", Rotation);
                Reactor.ExcuteQuery();
            }
        }

        public void HandleIncomingItem(Item Item, string WallPoint, VirtualRoomUser User)  // Already Updated @cache
        {
            // Handling Messages (FIRST, to avoid laggys)
            Response Response = new Response(83);
            Item.GetRoomResponse(Response);
            BroadcastResponse(Response);

            Item.GetTrigger().OnPlace(Item, User);

            // Update Info & MySQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET room_id = @roomid, point_wall = @point_wall WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.AddParam("itemid", Item.Id);
                Reactor.AddParam("point_wall", WallPoint);
                Reactor.ExcuteQuery();
            }
        }

        public void HandleIncomingItemUpdate(Item Item, iPoint OldPlace, iPoint NewPlace, int OldRotation, int Rotation, VirtualRoomUser User)  // Already Updated @cache
        {
            // Handling Messages (FIRST, to avoid laggys)
            Response Response = new Response(95);
            Item.GetRoomResponse(Response);
            BroadcastResponse(Response);

            GenerateMatrix();

            UpdateUsersOnPoint(OldPlace);

            foreach (iPoint Point in BrickEngine.GetItemReactor().GetAffectedPoints(OldPlace, Item.GetBaseItem().LengthY, Item.GetBaseItem().LengthX, OldRotation))
            {
                UpdateUsersOnPoint(Point);
            }

            UpdateUsersOnPoint(NewPlace);

            foreach (iPoint Point in BrickEngine.GetItemReactor().GetAffectedPoints(NewPlace, Item.GetBaseItem().LengthY, Item.GetBaseItem().LengthX, Rotation))
            {
                UpdateUsersOnPoint(Point);
            }

            Item.GetTrigger().OnUpdate(Item, User);

            // Update Info & MySQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET point_x = @x, point_y = @y, point_z = @z, rotation = @rot WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("itemid", Item.Id);
                Reactor.AddParam("x", NewPlace.X);
                Reactor.AddParam("y", NewPlace.Y);
                Reactor.AddParam("z", NewPlace.Z.ToString().Replace(',', '.'));
                Reactor.AddParam("rot", Rotation);
                Reactor.ExcuteQuery();
            }
        }

        public void HandleIncomingItemUpdate(Item Item, string WallPos, VirtualRoomUser User) // Already Updated @cache
        {
            // Handling Messages (FIRST, to avoid laggys)
            Response Response = new Response(85);
            Item.GetRoomResponse(Response);
            BroadcastResponse(Response);

            Item.GetTrigger().OnUpdate(Item, User);

            // Update Info & MySQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET point_wall = @point_wall WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("itemid", Item.Id);
                Reactor.AddParam("point_wall", WallPos);
                Reactor.ExcuteQuery();
            }
        }

        public void HandleIncomingItemPickUp(Item Item, iPoint OldPlace, iPoint NewPlace, int OldRotation, int Rotation, VirtualRoomUser User)
        {
            // Handling Messages (FIRST, to avoid laggys)
            Response Response = new Response();

            if (Item.GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.Initialize(94);
            }
            else
            {
                Response.Initialize(84);
            }

            Response.AppendRawInt32(Item.Id);
            Response.AppendChar(2);
            Response.AppendBoolean(false);

            BroadcastResponse(Response);

            GenerateMatrix();

            UpdateUsersOnPoint(OldPlace);

            foreach (iPoint Point in BrickEngine.GetItemReactor().GetAffectedPoints(OldPlace, Item.GetBaseItem().LengthY, Item.GetBaseItem().LengthX, OldRotation))
            {
                UpdateUsersOnPoint(Point);
            }

            Item.GetTrigger().OnRemove(Item, User);

            if (Item.GetBaseItem().ExternalType.ToLower().Equals("default"))
            {
                Item.ExtraData = "0";

                // Update Info & MySQL
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE items SET room_id = '0', extra_data = '0', point_x = @x, point_y = @y, point_z = @z, rotation = @rot WHERE id = @itemid LIMIT 1");
                    Reactor.AddParam("itemid", Item.Id);
                    Reactor.AddParam("x", NewPlace.X);
                    Reactor.AddParam("y", NewPlace.Y);
                    Reactor.AddParam("z", NewPlace.Z.ToString().Replace(',', '.'));
                    Reactor.AddParam("rot", Rotation);
                    Reactor.ExcuteQuery();
                }
            }
            else
            {
                // Update Info & MySQL
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE items SET room_id = '0', point_x = @x, point_y = @y, point_z = @z, rotation = @rot WHERE id = @itemid LIMIT 1");
                    Reactor.AddParam("itemid", Item.Id);
                    Reactor.AddParam("x", NewPlace.X);
                    Reactor.AddParam("y", NewPlace.Y);
                    Reactor.AddParam("z", NewPlace.Z.ToString().Replace(',', '.'));
                    Reactor.AddParam("rot", Rotation);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void HandleIncomingItemPickUp(Item Item, VirtualRoomUser User)
        {
            // Handling Messages (FIRST, to avoid laggys)
            Response Response = new Response();

            if (Item.GetBaseItem().InternalType.ToLower().Equals("s"))
            {
                Response.Initialize(94);
            }
            else
            {
                Response.Initialize(84);
            }

            Response.AppendRawInt32(Item.Id);
            Response.AppendChar(2);
            Response.AppendBoolean(false);

            BroadcastResponse(Response);

            Item.GetTrigger().OnRemove(Item, User);

            if (Item.GetBaseItem().ExternalType.ToLower().Equals("default"))
            {
                Item.ExtraData = "0";

                // Update Info & MySQL
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE items SET room_id = '0', extra_data = '0',point_wall = '' WHERE id = @itemid LIMIT 1");
                    Reactor.AddParam("itemid", Item.Id);
                    Reactor.ExcuteQuery();
                }
            }
            else
            {
                // Update Info & MySQL
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE items SET room_id = '0', point_wall = '' WHERE id = @itemid LIMIT 1");
                    Reactor.AddParam("itemid", Item.Id);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void HandleIncomingTrigger(VirtualRoomUser User, Item Item, int TriggerId)
        {
            Item.GetTrigger().OnTrigger(TriggerId, Item, User);
        }

        #endregion

        public string VerifyWallPosition(string Position)
        {
            if (string.IsNullOrEmpty(Position))
            {
                return string.Empty;
            }

            if (Position.Contains(Convert.ToChar(13)) || Position.Contains(Convert.ToChar(9)))
            {
                return string.Empty;
            }

            string Filtered = Position.Replace(":", string.Empty).ToLower();

            string[] Split = Filtered.Split(' ');

            if (Split.Length != 3)
            {
                return string.Empty;
            }

            string[] WidthRaw = Split[0].Replace("w=", string.Empty).Split(',');

            if (WidthRaw.Length != 2)
            {
                return string.Empty;
            }

            int[] WidthInts = new int[2];

            foreach (string raw in WidthRaw)
            {
                int Out = -1;

                if (!int.TryParse(raw, out Out))
                {
                    return string.Empty;
                }

                if (Out < 0 || Out > 200)
                {
                    return string.Empty;
                }

                WidthInts[Array.IndexOf(WidthRaw, raw)] = Out;
            }

            string[] LenghtRaw = Split[1].Replace("l=", string.Empty).Split(',');

            if (LenghtRaw.Length != 2)
            {
                return string.Empty;
            }

            int[] LenghtInts = new int[2];

            foreach (string raw in LenghtRaw)
            {
                int Out = -1;

                if (!int.TryParse(raw, out Out))
                {
                    return string.Empty;
                }

                if (Out < 0 || Out > 200)
                {
                    return string.Empty;
                }

                LenghtInts[Array.IndexOf(LenghtRaw, raw)] = Out;
            }

            char WallCharacter = Split[2][0];

            if (!WallCharacter.Equals('l') && !WallCharacter.Equals('r'))
            {
                return string.Empty;
            }

            return string.Format(":{0} {1} {2}", string.Format("{0}={1},{2}", 'w', WidthInts[0], WidthInts[1]), string.Format("{0}={1},{2}", 'l', LenghtInts[0], LenghtInts[1]), WallCharacter);
        }

        public void UpdateLayout(Client Client, Item Item)
        {
            string Key = Item.GetBaseItem().InternalName.ToLower();
            string Value = Item.ExtraData;

            if (!Key.Equals("wallpaper") && !Key.Equals("floor") && !Key.Equals("landscape"))
            {
                return;
            }

            if (Key.Equals("landscape") && !Value.Contains('.'))
            {
                return;
            }

            if ((Key.Equals("wallpaper") || Key.Equals("floor")) && (Value.Length < 3 || Value.Length > 4))
            {
                return;
            }

            // Deliver update to users @ room
            Response Response = new Response(46);
            Response.AppendStringWithBreak(Key);
            Response.AppendStringWithBreak(Value);
            BroadcastResponse(Response);

            // Update @cache
            if (Key.Equals("wallpaper"))
            {
                GetRoom().Wallpaper = Value;
            }
            else if (Key.Equals("floor"))
            {
                GetRoom().Floor = Value;
            }
            else if (Key.Equals("landscape"))
            {
                GetRoom().Landscape = Value;
            }

            Client.SendResponse(BrickEngine.GetItemReactor().RemoveItem(Item.Id));

            // Update Info & MySQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE private_rooms SET " + Key + " = @value WHERE id = @roomid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.AddParam("value", Value);
                Reactor.ExcuteQuery();
            }

            // Delete item
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM items WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("itemid", Item.Id);
                Reactor.ExcuteQuery();
            }
        }

        public List<Item> GetFloorItemsOnTile(iPoint Point)
        {
            var List = new List<Item>();

            foreach (Item Item in GetFloorItems())
            {
                if (Item.Point.Compare(Point))
                {
                    List.Add(Item);
                    continue;
                }

                foreach (iPoint AffectedPoint in BrickEngine.GetItemReactor().GetAffectedPoints(Item.Point, Item.GetBaseItem().LengthY, Item.GetBaseItem().LengthX, Item.Rotation))
                {
                    if (AffectedPoint.Compare(Point) && !List.Contains(Item))
                    {
                        List.Add(Item);
                    }
                }
            }

            return List;
        }

        public List<Item> GetFloorItems()
        {
            var List = new List<Item>();

            foreach (Item Item in GetItems())
            {
                if (Item.GetBaseItem().InternalType.ToLower().Equals("s"))
                {
                    List.Add(Item);
                }
            }

            return List;
        }

        public List<Item> GetWallItems()
        {
            var List = new List<Item>();

            foreach (Item Item in GetItems())
            {
                if (Item.GetBaseItem().InternalType.ToLower().Equals("i"))
                {
                    List.Add(Item);
                }
            }

            return List;
        }

        public List<Item> GetItems()
        {
            return BrickEngine.GetItemReactor().GetItemsForRoom(RoomId);
        }

        #endregion

        #region ClassDistructors

        public void Dispose()
        {
            foreach (VirtualRoomUser User in RoomUsers.Values.ToList())
            {
                HandleLeaveUser(User.HabboId, true);
            }

            foreach(VirtualRoomUser Pet in RoomPets.Values.ToList())
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE user_pets SET x = @x, y = @y, rot = @rot WHERE id = @petid LIMIT 1");
                    Reactor.AddParam("x", Pet.Point.X);
                    Reactor.AddParam("y", Pet.Point.Y);
                    Reactor.AddParam("rot", Pet.RotBody);
                    Reactor.AddParam("petid", Pet.HabboId);
                    Reactor.ExcuteQuery();
                }

                BrickEngine.GetPetReactor().GetPetInfo(Pet.HabboId).X = Pet.Point.X;
                BrickEngine.GetPetReactor().GetPetInfo(Pet.HabboId).Y = Pet.Point.Y;
            }

            BrickEngine.GetProgressReactor().GetCollector().Finialize(UserHandler);
            BrickEngine.GetProgressReactor().GetCollector().Finialize(PetsHandler);

            UserHandler.Dispose();
            UserHandler = null;

            PetsHandler.Dispose();
            PetsHandler = null;

            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        public VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);
        }

        #endregion

        #endregion
    }
}
