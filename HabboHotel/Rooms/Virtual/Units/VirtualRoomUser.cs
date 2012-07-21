using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Pets;

namespace BrickEmulator.HabboHotel.Rooms.Virtual.Units
{
    enum SpeechType
    {
        Whisper,
        Talk,
        Shout
    }

    class VirtualRoomUser
    {
        public readonly int VirtualId;
        public readonly int HabboId;
        public readonly int RoomId;

        public int DanceId = 0;
        public int SittingId = 0;

        private int MessageAmount = 0;
        private DateTime LastMessage = DateTime.Now;

        public int SuppressAlive = 0;
        public Boolean Suppressed = false;

        public Boolean TeleportingEnabled = false;

        public iPoint Point;
        public int RotHead;
        public int RotBody;

        public Boolean WalkFreezed = false;
        public Boolean PlayingGame = false;

        public List<iPoint> PathPoints = new List<iPoint>();

        public iPoint UnhandledGoalPoint = new iPoint(-1, -1);

        public Boolean Alive
        {
            get
            {
                return GetClient() != null;
            }
        }

        public Boolean IsPet
        {
            get
            {
                return BrickEngine.GetPetReactor().GetPetInfo(HabboId) != null;
            }
        }

        public Boolean NeedsWalking
        {
            get
            {
                return UnhandledGoalPoint.X != -1 && UnhandledGoalPoint.Y != -1;
            }
        }

        public Boolean IsWalking
        {
            get
            {
                return PathPoints.Count > 0;
            }
        }

        public iPoint FontPoint
        {
            get
            {
                iPoint Point = new iPoint(this.Point.X, this.Point.Y);

                if (RotBody == 0)
                {
                    Point.Y--;
                }
                else if (RotBody == 2)
                {
                    Point.X++;
                }
                else if (RotBody == 4)
                {
                    Point.Y++;
                }
                else if (RotBody == 6)
                {
                    Point.X--;
                }

                return Point;
            }
        }

        public iPoint FontBehind
        {
            get
            {
                iPoint Point = new iPoint(this.Point.X, this.Point.Y);

                if (RotBody == 0)
                {
                    Point.Y++;
                }
                else if (RotBody == 2)
                {
                    Point.X--;
                }
                else if (RotBody == 4)
                {
                    Point.Y--;
                }
                else if (RotBody == 6)
                {
                    Point.X++;
                }

                return Point;
            }
        }

        public Boolean NeedsLastWalk = false;

        private Dictionary<string, string> Statusses = new Dictionary<string, string>();

        public VirtualRoomUser(int VirtualId, int HabboId, int RoomId, int Rot)
        {
            this.VirtualId = VirtualId;
            this.HabboId = HabboId;
            this.RoomId = RoomId;
            this.RotHead = Rot;
            this.RotBody = Rot;
        }

        public void UpdatePoint(iPoint Point)
        {
            this.Point = Point;
        }

        public void GetResponse(Response Response)
        {               
            Response.AppendInt32(HabboId);

            if (!IsPet)
            {
                Response.AppendStringWithBreak(GetClient().GetUser().Username);
                Response.AppendStringWithBreak(GetClient().GetUser().Motto);
                Response.AppendStringWithBreak(GetClient().GetUser().Look);
            }
            else
            {
                PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(HabboId);

                Response.AppendStringWithBreak(Info.Name);
                Response.AppendChar(2);
                Response.AppendStringWithBreak(Info.GetLook);
            }

            Response.AppendInt32(VirtualId);
            Response.AppendInt32(Point.X);
            Response.AppendInt32(Point.Y);
            Response.AppendStringWithBreak(Point.Z.ToString().Replace(",", "."));

            if (!IsPet)
            {
                Response.AppendInt32(2);
                Response.AppendInt32(1);
                Response.AppendStringWithBreak(GetClient().GetUser().Gender.ToLower());
                Response.AppendInt32(-1);
                Response.AppendInt32(-1);
                Response.AppendInt32(-1);
                Response.AppendChar(2);
                Response.AppendInt32(GetClient().GetUser().AchievementScore);
            }
            else
            {
                Response.AppendInt32(4);
                Response.AppendInt32(2);
                Response.AppendInt32(11);
            }
        }

        public void GetStatusResponse(Response Response)
        {
            Response.AppendInt32(VirtualId);
            Response.AppendInt32(Point.X);
            Response.AppendInt32(Point.Y);
            Response.AppendStringWithBreak(Point.Z.ToString().Replace(",", "."));
            Response.AppendInt32(RotHead);
            Response.AppendInt32(RotBody);
            Response.AppendString("/");

            foreach (KeyValuePair<string, string> kvp in Statusses)
            {
                Response.AppendString(string.Format("{0} {1}/", kvp.Key, kvp.Value));
            }

            Response.AppendStringWithBreak("/");
        }

        public void Teleport(iPoint Point)
        {
            this.Point = Point;
            this.UpdateStatus(true);

            BrickEngine.GetEffectsHandler().RunFreeEffect(GetClient(), 4);
        }

        public void UpdateStatus(bool LastStep)
        {
            RemoveStatus("sit");
            RemoveStatus("lay");

            Point.Z = GetRoom().GetRoomEngine().GetTileHeight(Point);

            if (!IsPet)
            {
                if (GetRoom().GetRoomEngine().GetFloorItemsOnTile(Point).Count <= 0)
                {
                    if (BrickEngine.GetEffectsHandler().UserHasRunningEffect(HabboId) && GetClient().GetUser().HasFreeEffect)
                    {
                        BrickEngine.GetEffectsHandler().RunFreeEffect(GetClient(), -1);
                    }
                }
            }

            foreach (Item Item in GetRoom().GetRoomEngine().GetFloorItemsOnTile(Point))
            {
                Item.GetTrigger().OnPointInteract(Item, this);

                if (Item.GetBaseItem().EnableSit && LastStep)
                {
                    if (!IsPet)
                    {
                        AddStatus("sit", Item.GetBaseItem().LengthZ.ToString().Replace(',', '.'));
                    }
                    else if (!ContainsStatus("rng"))
                    {
                        AddStatus("sit", (Item.GetBaseItem().LengthZ - 0.3).ToString().Replace(',', '.'));
                    }

                    RotHead = Item.Rotation;
                    RotBody = Item.Rotation;
                }
            }

            try
            {
                Response Response = new Messages.Response(34);
                Response.AppendBoolean(true);
                GetStatusResponse(Response);

                GetRoom().GetRoomEngine().BroadcastResponse(Response);
            }
            catch { }
        }

        public void AddStatus(string Key, string Value)
        {
            lock (Statusses)
            {
                if (!Statusses.ContainsKey(Key))
                {
                    Statusses.Add(Key, Value);
                }
                else
                {
                    Statusses[Key] = Value;
                }
            }
        }

        public void UnSuppress()
        {
            SuppressAlive = 0;

            if (Suppressed)
            {
                Response SuppressedResponse = new Response(486);
                SuppressedResponse.AppendInt32(VirtualId);
                SuppressedResponse.AppendBoolean(false);
                GetRoom().GetRoomEngine().BroadcastResponse(SuppressedResponse);

                Suppressed = false;
            }
        }

        public void Talk(string Message, SpeechType Type, int Emoticon, string TargetUserName)
        {
            UnSuppress();

            MessageAmount++;

            if (!IsPet)
            {
                if (!GetRoom().GetRoomEngine().CheckPetCommand(GetClient(), Message))
                {
                    if ((DateTime.Now - LastMessage).TotalSeconds < 4)
                    {
                        if (MessageAmount > 6)
                        {
                            Response Flood = new Response(27);
                            Flood.AppendInt32(BrickEngine.GetConfigureFile().CallIntKey("flood.penalty.seconds"));
                            GetClient().SendResponse(Flood);
                            return;
                        }
                    }
                    else
                    {
                        MessageAmount = 0;
                    }
                }

                LastMessage = DateTime.Now;
            }

            if (Message.StartsWith(":"))
            {
                List<string> Params = Message.Split(' ').ToList();

                string RawCommand = Params[0];

                // Make sure the 'Command' is removed.
                Params.Remove(RawCommand);

                string CleanCommand = RawCommand.Substring(1);

                if (BrickEngine.GetCommandHandler().HandleCommand(CleanCommand, GetClient(), Params))
                {
                    return;
                }
            }

            if (!IsPet)
            {
                BrickEngine.GetChatlogHandler().AddChatlog(HabboId, RoomId, Message);
            }

            VirtualRoomUser Target = null;

            if (Type.Equals(SpeechType.Whisper) && TargetUserName.Length > 0)
            {
                if (TargetUserName.Length <= 0)
                {
                    return;
                }
                else
                {
                    int TargetHabboId = BrickEngine.GetUserReactor().GetId(TargetUserName);

                    Target = GetRoom().GetRoomEngine().GetUserByHabboId(TargetHabboId);
                }
            }

            int Header = 0;

            if (Type.Equals(SpeechType.Talk))
            {
                Header = 24;
            }
            else if (Type.Equals(SpeechType.Shout))
            {
                Header = 26;
            }
            else if (Type.Equals(SpeechType.Whisper))
            {
                Header = 25;
            }

            if (Type.Equals(SpeechType.Whisper))
            {
                if (Target != null)
                {

                    Response Response = new Response(Header);

                    Response.AppendInt32(VirtualId);
                    Response.AppendStringWithBreak(BrickEngine.GetWordFilterHandler().FilterMessage(Target.GetClient(), Message));
                    Response.AppendInt32(Emoticon);
                    Response.AppendBoolean(false);

                    // Check Receiver & Send to Receiver
                    if (!Target.GetClient().GetUser().HasIgnoredUser(HabboId))
                    {
                        Target.GetClient().SendResponse(Response);
                    }

                    // Make sure you won't forget yourself.
                    GetClient().SendResponse(Response);
                }
            }
            else
            {
                GetRoom().GetRoomEngine().BroadcastChatResponse(HabboId, VirtualId, Header, Emoticon, Message);
            }
        }

        public int GetEmoticon(string Message)
        {
            var Emoticons = new Dictionary<string[], int>();

            Emoticons.Add(new string[] { ":)", "(:", "=)", "(=", "=]", "[=", ":D", ";)", "(;", "XD" }, 1); // Happy
            Emoticons.Add(new string[] { ">:(", ":@", ">:[" }, 2); // Angry
            Emoticons.Add(new string[] { ":O", "O:", ";o", "o;", ":0", "0:", ";0", "0;" }, 3); // Suprised
            Emoticons.Add(new string[] { ":(", "):", ":[", "]:", "=[", "]=", ":'(", ")':", "='[", "]'=" }, 4); // Sad

            foreach (KeyValuePair<string[], int> kvp in Emoticons)
            {
                foreach (string Character in kvp.Key)
                {
                    if (Message.ToLower().Contains(Character.ToLower()))
                    {
                        return kvp.Value;
                    }
                }
            }

            return 0;
        }

        public void RemoveStatus(string Key)
        {
            Statusses.Remove(Key);
        }

        public void RemoveStatus()
        {
            Statusses = new Dictionary<string, string>();
        }

        public Boolean ContainsStatus(string Key)
        {
            return Statusses.ContainsKey(Key);
        }

        public VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);
        }

        public Client GetClient()
        {
            return BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();
        }
    }
}
