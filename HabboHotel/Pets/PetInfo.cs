using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Pets
{
    enum PetInterAction
    {
        Interactive,
        Action,
        Walking,
        Chatting,
        Playing,
        Progressing,
        Sleeping
    }

    class PetInfo
    {
        #region InfoFields
        public readonly int Id;
        public readonly int UserId;
        public int RoomId;
        public readonly string Name;
        public readonly int Type;
        public readonly int Race;
        public readonly string Color;
        #endregion

        #region Mood and Body status Fields
        public int Happiness;
        public int Expirience;
        public int Energy;
        #endregion

        #region Extra Fields
        public int Respect;
        public DateTime Created;

        public int X;
        public int Y;
        public int Rot;
        #endregion

        #region CacheFields
        public DateTime MutedDateTime = new DateTime(1,1,1);

        private Boolean Hungry = false;
        private Boolean Sad = false;
        private Boolean Happy = true;
        private Boolean EndPlaying = false;

        private Item PlayingItem = null;

        public Random Random = new Random();

        private PetInterAction Action = PetInterAction.Interactive;
        #endregion

        #region Properties

        public string GetLook
        {
            get
            {
                return string.Format("{0} {1} {2}", Type, Race, Color);
            }
        }

        public int DaysOld
        {
            get
            {
                return BrickEngine.GetConvertor().ObjectToInt32((DateTime.Now - Created).TotalDays);
            }
        }

        public Boolean IsInRoom
        {
            get
            {
                return GetRoom() != null;
            }
        }

        public int Level
        {
            get
            {
                int e = -1;

                for (int i = 0; i <= PetReactor.MAX_LEVEL; i++)
                {
                    var ExpirienceNeed = BrickEngine.GetPetReactor().ExpirienceLevels[i];

                    if (Expirience >= ExpirienceNeed && e <= PetReactor.MAX_LEVEL)
                    {
                        e++;
                    }
                }

                return e;
            }
        }

        public int ExpirienceGoal
        {
            get
            {
                try { return BrickEngine.GetPetReactor().ExpirienceLevels[Level + 1]; }
                catch { return BrickEngine.GetPetReactor().ExpirienceLevels[Level]; }
            }
        }

        public Boolean Sleeping
        {
            get
            {
                return Action.Equals(PetInterAction.Sleeping);
            }
        }

        public Boolean Muted
        {
            get
            {
                if (MutedDateTime.Year > 1)
                {
                    if ((DateTime.Now - MutedDateTime).TotalMinutes >= 1)
                    {
                        MutedDateTime = new DateTime(1, 1, 1);
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        #endregion

        #region Constructors

        public PetInfo(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            RoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Type = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Race = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Color = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            Happiness = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Expirience = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Energy = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            Respect = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Created = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);

            X = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Y = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Rot = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public PetInfo(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            RoomId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Type = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Race = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Color = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            Happiness = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Expirience = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Energy = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            Respect = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Created = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);

            X = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Y = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Rot = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        #endregion

        #region Events
        public void DoActions(PetInterAction Action, VirtualRoomUser Pet)
        {
            if (Pet.ContainsStatus("lay"))
            {
                Pet.RemoveStatus("lay");
            }

            if (Action.Equals(PetInterAction.Progressing))
            {
                this.Action = PetInterAction.Progressing;
            }

            if (Energy <= 15 && !this.Action.Equals(PetInterAction.Sleeping))
            {
                if (Happiness > 0)
                {
                    Happiness--;
                    UpdateHappiness();
                }

                Pet.AddStatus("lay", string.Empty);
                Pet.UpdateStatus(true);

                this.Action = PetInterAction.Sleeping;
            }
            else
            {
                if (this.Action.Equals(PetInterAction.Sleeping))
                {
                    if (Energy < PetReactor.MAX_ENERGY(Level) && Energy + 2 <= PetReactor.MAX_ENERGY(Level))
                    {
                        Energy += 2;
                        UpdateEnergy();
                    }
                    else
                    {
                        Energy = PetReactor.MAX_ENERGY(Level);
                    }

                    if (Energy >= PetReactor.MAX_ENERGY(Level))
                    {
                        Pet.Talk("Welcome back!", SpeechType.Talk, 0, string.Empty);
                        Pet.RemoveStatus("lay");

                        if (Happiness < PetReactor.MAX_HAPPINESS)
                        {
                            Happiness++;
                            UpdateHappiness();
                        }

                        this.Action = PetInterAction.Interactive;
                    }
                    else
                    {
                        Pet.AddStatus("lay", string.Empty);
                    }

                    Pet.UpdateStatus(true);

                    if (Action.Equals(PetInterAction.Chatting))
                    {
                        Pet.Talk("*Sleeping Zzz..*", SpeechType.Talk, 0, string.Empty);
                    }
                }
                else
                {
                    if (this.Action.Equals(PetInterAction.Progressing))
                    {
                        if (GetRoom().GetRoomEngine().GetPetToys().Count > 0)
                        {
                            foreach (Item Toy in GetRoom().GetRoomEngine().GetPetToys())
                            {
                                if (GetRoomUser().Point.Compare(Toy.Point))
                                {
                                    if (Toy.ExtraData == "0")
                                    {
                                        Pet.RotBody = 2;
                                        Pet.RotHead = 2;

                                        Pet.AddStatus("pla", string.Empty);
                                        Pet.UpdateStatus(true);

                                        Toy.ExtraData = "1";

                                        if (Happiness < PetReactor.MAX_HAPPINESS && Happiness + 3 <= PetReactor.MAX_HAPPINESS)
                                        {
                                            Happiness += 3;
                                            UpdateHappiness();
                                        }

                                        GetRoom().GetRoomEngine().BroadcastResponse(Toy.GetTriggerResponse());
                                    }
                                    else
                                    {
                                        if (!EndPlaying)
                                        {
                                            EndPlaying = true;
                                        }
                                        else
                                        {
                                            Toy.ExtraData = "0";
                                            GetRoom().GetRoomEngine().BroadcastResponse(Toy.GetTriggerResponse());
                                            EndPlaying = false;

                                            Pet.RemoveStatus("pla");
                                            Pet.UpdateStatus(true);

                                            this.Action = PetInterAction.Interactive;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Action.Equals(PetInterAction.Walking))
                        {
                            if (Energy > 0)
                            {
                                if (Pet.IsWalking)
                                {
                                    Pet.PathPoints = new List<Rooms.Pathfinding.iPoint>();
                                    Pet.UpdateStatus(true);
                                }

                                Pet.UnhandledGoalPoint = GetRoom().GetRoomEngine().GetRandom();

                                Energy--;
                                UpdateEnergy();

                                if (Happiness < PetReactor.MAX_HAPPINESS && Happiness + 3 <= PetReactor.MAX_HAPPINESS)
                                {
                                    Happiness += 3;
                                    UpdateHappiness();
                                }

                                this.Action = Action;
                            }
                        }
                        else if (Action.Equals(PetInterAction.Chatting) && !Muted)
                        {
                            var Speeches = BrickEngine.GetPetReactor().GetSpeechesForType(Type);

                            PetSpeech Speech = null;

                            if (Speeches.Count > 0)
                            {
                                Speech = Speeches[Random.Next(0, Speeches.Count - 1)];
                            }

                            if (Speech != null)
                            {
                                Pet.Talk(Speech.Speech, Speech.Shout ? SpeechType.Shout : SpeechType.Talk, 0, string.Empty);
                            }

                            if (Happiness < PetReactor.MAX_HAPPINESS && Happiness + 5 <= PetReactor.MAX_HAPPINESS)
                            {
                                Happiness += 5;
                                UpdateHappiness();
                            }

                            this.Action = Action;
                        }
                        else if (Action.Equals(PetInterAction.Action) && !GetRoomUser().IsWalking)
                        {
                            if (Pet.GetRoom().GetRoomEngine().GetPetToys().Count > 0 && Random.Next(0, 5) == 2)
                            {
                                Pet.AddStatus("gst", "plf");
                                Pet.UpdateStatus(true);

                                Pet.UnhandledGoalPoint = Pet.GetRoom().GetRoomEngine().GetPetToys()[Random.Next(0, Pet.GetRoom().GetRoomEngine().GetPetToys().Count - 1)].Point;
                                DoActions(PetInterAction.Progressing, Pet);
                            }
                            else
                            {
                                var Actions = BrickEngine.GetPetReactor().GetActionsForType(Type);

                                PetAction doAction = null;

                                if (Actions.Count > 0)
                                {
                                    doAction = Actions[Random.Next(0, Actions.Count - 1)];
                                }

                                if (doAction != null)
                                {
                                    Pet.AddStatus(doAction.Key, doAction.Value);
                                    Pet.UpdateStatus(true);
                                }

                                if (Energy > 0)
                                {
                                    Energy--;
                                    UpdateEnergy();
                                }

                                if (Happiness < PetReactor.MAX_HAPPINESS && Happiness + 5 <= PetReactor.MAX_HAPPINESS)
                                {
                                    Happiness += 5;
                                    UpdateHappiness();
                                }

                                this.Action = Action;
                            }
                        }
                        else
                        {
                            this.Action = PetInterAction.Interactive;
                        }
                    }
                }
            }
        }

        public void CollectStatus()
        {
            Happy = false;

            if (Energy < 20)
            {
                Hungry = true;
            }
            else if (Energy >= 20)
            {
                if (Hungry)
                {
                    Hungry = false;
                }
            }

            if (Happiness <= 30)
            {
                Sad = true;
            }
            else if (Happiness >= 90)
            {
                if (Sad)
                {
                    Sad = false;
                    Happy = true;
                }

                if (Happiness >= 75)
                {
                    Happy = true;
                }
            }
            else
            {
                if (Sad)
                {
                    Sad = false;
                }
            }

            if (!this.Action.Equals(PetInterAction.Playing) && !this.Action.Equals(PetInterAction.Progressing))
            {
                GetRoomUser().RemoveStatus();

                DeliverStatus();
            }
        }

        public void DeliverStatus()
        {
            if (Hungry)
            {
                GetRoomUser().AddStatus("gst", "hng");
            }
            else if (Sad)
            {
                GetRoomUser().AddStatus("gst", "sad");
            }
            else if (Happy)
            {
                GetRoomUser().AddStatus("gst", "sml");
            }
        }

        public void GoPlay()
        {

        }
        #endregion

        #region Methods
        public void GetInventoryResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(Name);
            Response.AppendInt32(Type);
            Response.AppendInt32(Race);
            Response.AppendStringWithBreak(Color);
        }

        public Response GetInfoResponse()
        {
            Response Response = new Response(601);
            Response.AppendInt32(Id);
            Response.AppendStringWithBreak(Name);
            Response.AppendInt32(Level); // Level
            Response.AppendInt32(PetReactor.MAX_LEVEL);
            Response.AppendInt32(Expirience);
            Response.AppendInt32(ExpirienceGoal);
            Response.AppendInt32(Energy);
            Response.AppendInt32(PetReactor.MAX_ENERGY(Level));
            Response.AppendInt32(Happiness);
            Response.AppendInt32(PetReactor.MAX_HAPPINESS);
            Response.AppendStringWithBreak(Color.ToLower());
            Response.AppendInt32(Respect);
            Response.AppendInt32(UserId);
            Response.AppendInt32(DaysOld);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(UserId));
            return Response;
        }

        public void UpdateEnergy()
        {
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_pets SET energy = @energy WHERE id = @petid LIMIT 1");
                Reactor.AddParam("energy", Energy);
                Reactor.AddParam("petid", Id);
                Reactor.ExcuteQuery();
            }
        }

        public void UpdateHappiness()
        {
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_pets SET happiness = @happiness WHERE id = @petid LIMIT 1");
                Reactor.AddParam("happiness", Happiness);
                Reactor.AddParam("petid", Id);
                Reactor.ExcuteQuery();
            }
        }

        public void GiveExpirience(int Amount)
        {
            if (Level == PetReactor.MAX_LEVEL)
            {
                Expirience = BrickEngine.GetPetReactor().ExpirienceLevels[PetReactor.MAX_LEVEL];
            }
            else
            {
                Expirience += Amount;

                if (IsInRoom)
                {
                    Response Response = new Response(609);
                    Response.AppendInt32(Id);
                    Response.AppendInt32(GetRoomUser().VirtualId);
                    Response.AppendInt32(Amount);
                    GetRoom().GetRoomEngine().BroadcastResponse(Response);
                }

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE user_pets SET expirience = @expirience WHERE id = @petid LIMIT 1");
                    Reactor.AddParam("expirience", Expirience);
                    Reactor.AddParam("petid", Id);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public void GiveRespect()
        {
            Respect++;

            if (IsInRoom)
            {
                Response Response = new Response(606);
                Response.AppendInt32(Respect);
                Response.AppendInt32(UserId);
                Response.AppendInt32(Id);
                Response.AppendStringWithBreak(Name);
                Response.AppendBoolean(false);
                Response.AppendInt32(10);
                Response.AppendBoolean(false);
                Response.AppendInt32(-2);
                Response.AppendBoolean(true);
                Response.AppendChar(2);
                GetRoom().GetRoomEngine().BroadcastResponse(Response);
            }

            GiveExpirience(10);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_pets SET respect = respect + 1 WHERE id = @petid LIMIT 1");
                Reactor.AddParam("petid", Id);
                Reactor.ExcuteQuery();
            }
        }

        public VirtualRoomUser GetRoomUser()
        {
            return GetRoom().GetRoomEngine().GetUserByPetId(Id);
        }

        public VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);
        }

        #endregion
    }
}
