using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Rooms.Virtual
{
    class VirtualRoom : IDisposable
    {
        #region Fields
        public readonly int Id;
        public string Name;
        public string Description = string.Empty;
        public int OwnerId;

        public int DoorState;
        public int CategoryId;
        public int LimitUsers;

        public string Password = string.Empty;
        public readonly string ModelParam;

        public List<string> Tags = new List<string>();

        public RoomIcon Icon;
        public RoomEvent Event;

        public int Rating;

        public Boolean AllowPets;
        public Boolean AllowPetsEat;
        public Boolean AllowWalkthough;
        public Boolean AllowHideWall;

        public int WallThick;
        public int FloorThick;

        public string Wallpaper;
        public string Floor;
        public string Landscape;

        private RoomRunningState RunningState = RoomRunningState.Dead;
        private VirtualRoomEngine RunningEngine = null;
        #endregion

        #region Properties

        public RoomRunningState InternalState
        {
            get
            {
                if (RunningEngine != null)
                {
                    if (RunningState.Equals(RoomRunningState.Alive))
                    {
                        if (RunningEngine.RoomUserAmount <= 0)
                        {
                            return RoomRunningState.NeedsUnload;
                        }
                    }
                }

                return RunningState;
            }
        }

        public int RoomUserAmount
        {
            get
            {
                if (InternalState.Equals(RoomRunningState.Alive))
                {
                    return GetRoomEngine().RoomUserAmount;
                }

                return 0;
            }
        }
        #endregion

        #region Constructors

        public VirtualRoom(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Description = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            OwnerId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            LimitUsers = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Password = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            ModelParam = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            string TagsParams = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            foreach (string Tag in TagsParams.Split(','))
            {
                if (Tag.Length > 0)
                {
                    Tags.Add(Tag);
                }
            }

            DoorState = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            int IconBg = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            int IconFg = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            string ItemsRaw = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            Icon = new RoomIcon(Id, IconBg, IconFg, ItemsRaw);

            Rating = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            AllowPets = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            AllowPetsEat = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            AllowWalkthough = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
            AllowHideWall = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);

            WallThick = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            FloorThick = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            Wallpaper = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Floor = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Landscape = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }
        #endregion

        #region Methods
        public void Activate()
        {
            RunningState = RoomRunningState.Alive;

            RunningEngine = new VirtualRoomEngine(Id);
            RunningEngine.Prepare();
        }

        public RoomEnterMessage TryEnterRoom(Client Client, string Password)
        {
            if (RoomUserAmount >= LimitUsers)
            {
                return RoomEnterMessage.LimitUsers;
            }

            else if (GetRoomEngine().HasRights(Client.GetUser().HabboId, RightsType.Founder))
            {
                return RoomEnterMessage.Accepted;
            }

            else if (GetRoomEngine().CheckBanned(Client.GetUser().HabboId))
            {
                return RoomEnterMessage.Banned;
            }

            else if (DoorState.Equals(1) && RoomUserAmount <= 0)
            {
                return RoomEnterMessage.NoAnswer;
            }

            else if (DoorState.Equals(2) && !this.Password.Equals(Password))
            {
                return RoomEnterMessage.WrongPassword;
            }

            return RoomEnterMessage.Accepted;
        }

        public void GetNavigatorResponse(Response Response, Boolean AllowEvents)
        {
            Response.AppendInt32(Id);
            Response.AppendBoolean(AllowEvents);
            Response.AppendStringWithBreak((AllowEvents) ? Event.Name : Name);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(OwnerId));
            Response.AppendInt32(DoorState);
            Response.AppendInt32(RoomUserAmount);
            Response.AppendInt32(LimitUsers);
            Response.AppendStringWithBreak((AllowEvents) ? Event.Description : Description);
            Response.AppendBoolean(false);
            Response.AppendBoolean(BrickEngine.GetNavigatorManager().GetPrivateCategory(CategoryId).EnableTrading);
            Response.AppendInt32(Rating);
            Response.AppendInt32((AllowEvents) ? Event.CategoryId : CategoryId);

            if (AllowEvents)
            {
                Response.AppendString(Event.Started.ToShortTimeString());
            }

            Response.AppendChar(2);
            Response.AppendInt32((AllowEvents) ? Event.Tags.Count : Tags.Count);

            if (AllowEvents)
            {
                Event.Tags.ToList().ForEach(Response.AppendStringWithBreak);
            }
            else
            {
                Tags.ToList().ForEach(Response.AppendStringWithBreak);
            }

            Icon.GetResponse(Response);

            Response.AppendBoolean(true);
            Response.AppendBoolean(false);
        }

        public Boolean BeginEnterRoom(Client Client, string Password)
        {
            RoomEnterMessage EnterMessage = TryEnterRoom(Client,Password);

            if (!EnterMessage.Equals(RoomEnterMessage.Accepted))
            {
                Response ErrorResponse = new Response();

                if (EnterMessage.Equals(RoomEnterMessage.Banned))
                {
                    ErrorResponse.Initialize(224);
                    ErrorResponse.AppendInt32(4);
                    ErrorResponse.Initialize(18);
                    Client.SendResponse(ErrorResponse);
                    return false;
                }
                else if (EnterMessage.Equals(RoomEnterMessage.LimitUsers))
                {
                    ErrorResponse.Initialize(224);
                    ErrorResponse.AppendInt32(1);
                    ErrorResponse.Initialize(18);
                    Client.SendResponse(ErrorResponse);
                    return false;
                }
                else if (EnterMessage.Equals(RoomEnterMessage.NoAnswer))
                {
                    Client.SendResponse(new Response(131));
                    return false;
                }
                else if (EnterMessage.Equals(RoomEnterMessage.WrongPassword))
                {
                    ErrorResponse.Initialize(33);
                    ErrorResponse.AppendInt32(-100002);
                    ErrorResponse.Initialize(18);
                    Client.SendResponse(ErrorResponse);
                    return false;
                }
            }
            else
            {
                Client.SendResponse(new Response(19));
                return true;
            }

            return false;
        }

        public void GetSecondairResponse(Client Client)
        {
            Response PasteLink = new Response(166);
            PasteLink.AppendStringWithBreak("/client/private/" + Id + "/id");
            Client.SendResponse(PasteLink);

            Response ModelParams = new Response(69);
            ModelParams.AppendStringWithBreak(ModelParam.ToLower());
            ModelParams.AppendInt32(Id);
            Client.SendResponse(ModelParams);

            Response Environment = new Response();

            if (!Wallpaper.Equals("0.0"))
            {
                Environment.Initialize(46);
                Environment.AppendStringWithBreak("wallpaper");
                Environment.AppendStringWithBreak(Wallpaper);
            }

            if (!Floor.Equals("0.0"))
            {
                Environment.Initialize(46);
                Environment.AppendStringWithBreak("floor");
                Environment.AppendStringWithBreak(Floor);
            }

            Environment.Initialize(46);
            Environment.AppendStringWithBreak("landscape");
            Environment.AppendStringWithBreak(Landscape);

            Environment.Initialize(345);

            if (GetRoomEngine().HasRights(Client.GetUser().HabboId, RightsType.Founder) || Client.GetUser().VotedRooms.Contains(Id))
            {
                Environment.AppendInt32(Rating);
            }
            else
            {
                Environment.AppendInt32(-1);
            }

            Environment.Initialize(370);

            if (Event != null)
            {
                Event.GetResponse(Environment);
            }
            else
            {
                Environment.AppendRawInt32(-1);
                Environment.AppendChar(2);
            }

            Client.SendResponse(Environment);
        }

        public void GetMapsResponse(Client Client)
        {
            Response Premair = new Response(31);
            Premair.AppendStringWithBreak(GetRoomModel().GetPremairParams());
            Client.SendResponse(Premair);

            Response Secondair = new Response(470);
            Secondair.AppendStringWithBreak(GetRoomModel().GetSecondairParams());
            Client.SendResponse(Secondair);
        }

        public void GetRestResponse(Client Client)
        {
            Response UsersWithoutMe = new Response(28); // Users without me (before me)
            UsersWithoutMe.AppendInt32(GetRoomEngine().UnitCount); // UsersCount

            foreach (VirtualRoomUser User in GetRoomEngine().GetUsers())
            {
                User.GetResponse(UsersWithoutMe);
            }

            foreach (VirtualRoomUser Pet in GetRoomEngine().GetPets())
            {
                Pet.GetResponse(UsersWithoutMe);
            }

            Client.SendResponse(UsersWithoutMe);

            Response StaticFurni = new Response(30);
            StaticFurni.AppendBoolean(false); // Habbo leaves Public Rooms?
            Client.SendResponse(StaticFurni);

            Response FloorItems = new Response(32);
            FloorItems.AppendInt32(GetRoomEngine().GetFloorItems().Count); // ItemCount

            foreach (Item FloorItem in GetRoomEngine().GetFloorItems())
            {
                FloorItem.GetRoomResponse(FloorItems);
            }

            Client.SendResponse(FloorItems);

            Response WallItems = new Response(45);
            WallItems.AppendInt32(GetRoomEngine().GetWallItems().Count); // ItemCount

            foreach (Item WallItem in GetRoomEngine().GetWallItems())
            {
                WallItem.GetRoomResponse(WallItems);
            }

            Client.SendResponse(WallItems);

            if (GetRoomEngine().GenerateRoomUser(Client) != null)
            {
                Response UsersWithMe = new Response(28); // Users with me (after me)
                UsersWithMe.AppendInt32(GetRoomEngine().UnitCount); // UsersCount

                foreach (VirtualRoomUser User in GetRoomEngine().GetUsers())
                {
                    User.GetResponse(UsersWithMe);

                    if (User.DanceId > 0)
                    {
                        Response Dance = new Response(480);
                        Dance.AppendInt32(User.VirtualId);
                        Dance.AppendInt32(User.DanceId);
                        Client.SendResponse(Dance);
                    }

                    if (User.Suppressed)
                    {
                        Response Suppressed = new Response(486);
                        Suppressed.AppendInt32(User.VirtualId);
                        Suppressed.AppendBoolean(true);
                        Client.SendResponse(Suppressed);
                    }

                    if (BrickEngine.GetEffectsHandler().UserHasRunningEffect(User.HabboId))
                    {
                        Response Response = new Response(485);
                        Response.AppendInt32(User.VirtualId);
                        Response.AppendInt32(BrickEngine.GetEffectsHandler().GetRunningEffect(User.HabboId).EffectId);
                        Client.SendResponse(Response);
                    }
                }

                foreach (VirtualRoomUser Pet in GetRoomEngine().GetPets())
                {
                    Pet.GetResponse(UsersWithMe);
                }

                Client.SendResponse(UsersWithMe);
            }
            else
            {
                BrickEngine.GetPacketHandler().ClearLoading(Client, true);
                return;
            }

            if (BrickEngine.GetEffectsHandler().UserHasRunningEffect(Client.GetUser().HabboId))
            {
                Response Response = new Response(485);
                Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);
                Response.AppendInt32(BrickEngine.GetEffectsHandler().GetRunningEffect(Client.GetUser().HabboId).EffectId);
                Client.SendResponse(Response);
            }

            Response Additionals = new Response(472);
            Additionals.AppendBoolean(AllowHideWall);
            Additionals.AppendInt32(WallThick);
            Additionals.AppendInt32(FloorThick);
            Client.SendResponse(Additionals);

            int HabboId = Client.GetUser().HabboId;

            if (GetRoomEngine().HasRights(Client.GetUser().HabboId, RightsType.Founder))
            {
                Client.SendResponse(new Response(47));
                Client.SendResponse(new Response(42));

                VirtualRoomUser myUser = GetRoomEngine().GetUserByHabboId(Client.GetUser().HabboId);
                myUser.AddStatus("flatctrl useradmin", "");
            }
            else if (GetRoomEngine().HasRights(Client.GetUser().HabboId, RightsType.Rights))
            {
                Client.SendResponse(new Response(42));

                VirtualRoomUser myUser = GetRoomEngine().GetUserByHabboId(Client.GetUser().HabboId);
                myUser.AddStatus("flatctrl", "");
            }

            Response Extra = new Response(471);
            Extra.AppendBoolean(true);
            Extra.AppendInt32(Id);
            Extra.AppendBoolean(GetRoomEngine().HasRights(Client.GetUser().HabboId,RightsType.Founder)); // Enable Editing
            Client.SendResponse(Extra);

            Response Statusses = new Response(34);
            Statusses.AppendInt32(GetRoomEngine().UnitCount); // UsersCount

            foreach (VirtualRoomUser User in GetRoomEngine().GetUsers())
            {
                User.GetStatusResponse(Statusses);
            }

            foreach (VirtualRoomUser Pet in GetRoomEngine().GetPets())
            {
                Pet.GetStatusResponse(Statusses);
            }  

            Client.SendResponse(Statusses);

            Client.SendResponse(new Response(208));

            Response RoomCache = new Response(454);
            RoomCache.AppendBoolean(true);
            GetNavigatorResponse(RoomCache, false);
            Client.SendResponse(RoomCache);
        }

        public double GetTileHeight(iPoint Point)
        {
            try { return GetRoomModel().DefaultHeightMap[Point.X, Point.Y]; }
            catch { return 0.0; }
        }

        public void Dispose()
        {
            GetRoomEngine().Dispose();

            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        public RoomModel GetRoomModel()
        {
            return BrickEngine.GetRoomReactor().GetRoomModel(ModelParam);
        }

        public VirtualRoomEngine GetRoomEngine()
        {
            return RunningEngine;
        }
        #endregion
    }
}
