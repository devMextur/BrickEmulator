using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using System.Threading.Tasks;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using System.Threading;

namespace BrickEmulator.HabboHotel.Rooms.Games
{
    enum EffectType
    {
        Helmet,
        Shield
    }

    enum FreezeTeam
    {
        None,
        Blue,
        Red,
        Green,
        Yellow
    }


    /// <summary>
    /// Freeze Game, play with your friends.
    /// </summary>
    /// <author>Breakz0ne</author>
    class Freeze
    {
        #region Constant Fields

        #region EffectIds

        public const int FREEZE_USER_EFFECT = 12;

        #region Helmets
        public const int RED_HELMET_EFFECT     = 40;
        public const int GREEN_HELMET_EFFECT   = 41;
        public const int BLUE_HELMET_EFFECT    = 42;
        public const int YELLOW_HELMET_EFFECT  = 43;
        #endregion

        #region Shields
        public const int RED_SHIELD_EFFECT     = 49;
        public const int GREEN_SHIELD_EFFECT   = 50;
        public const int BLUE_SHIELD_EFFECT    = 51;
        public const int YELLOW_SHIELD_EFFECT  = 52;
        #endregion

        #endregion

        #endregion

        #region Fields

        private int RoomId;
        private Dictionary<int, FreezeTeam> Gamers = new Dictionary<int, FreezeTeam>();
        private Dictionary<int, DateTime> FreezedUsers = new Dictionary<int, DateTime>();
        private Dictionary<int, DateTime> ArmouredUsers = new Dictionary<int, DateTime>();

        private Timer CollectionCleaner;

        #endregion

        #region Properties

        private Dictionary<int, Item> GetTiles()
        {
            var Dic = new Dictionary<int, Item>();

            foreach (Item Item in GetEngine().GetFloorItems())
            {
                if (Item.GetBaseItem().InternalName.ToLower().Equals("es_tile"))
                {
                    Dic.Add(Item.Id, Item);
                }
            }

            return Dic;
        }

        private Dictionary<int, Item> GetBlocks()
        {
            var Dic = new Dictionary<int, Item>();

            foreach (Item Item in GetEngine().GetFloorItems())
            {
                if (Item.GetBaseItem().InternalName.ToLower().Equals("es_box"))
                {
                    Dic.Add(Item.Id, Item);
                }
            }

            return Dic;
        }

        #endregion

        #region Constructors

        public Freeze(int RoomId)
        {
            this.RoomId = RoomId;
            this.CollectionCleaner = new Timer(new TimerCallback(CleanCollections), CollectionCleaner, 0, 100);
        }

        #endregion

        #region Methods

        private void CleanCollections(Object e)
        {
            foreach (int UserId in Gamers.Values)
            {
                CheckFreezed(UserId);
                CheckArmour(UserId);
            }
        }

        public void EndGaming(int HabboId)
        {
            Gamers.Remove(HabboId);
            FreezedUsers.Remove(HabboId);
            ArmouredUsers.Remove(HabboId);
        }

        public void StartGaming(int HabboId, FreezeTeam Team)
        {
            if (GetUsersForTeam(Team).Count >= 5)
            {
                return;
            }

            if (!Gamers.ContainsKey(HabboId))
            {
                Gamers.Add(HabboId, Team);
            }
            else
            {
                Gamers[HabboId] = Team;
            }

            goHelmetUser(HabboId);
        }

        public void TriggerTile(int HabboId, Item Item)
        {
            if (!GetTiles().ContainsKey(Item.Id))
            {
                return;
            }

            VirtualRoomUser myUser = GetUser(HabboId);

            FreezeTeam myTeam = GetTeam(HabboId);

            if (myTeam.Equals(FreezeTeam.None))
            {
                return;
            }

            if (myUser == null)
            {
                return;
            }

            if (CheckFreezed(HabboId))
            {
                return;
            }

            iPoint CrossCenter = myUser.Point;

            if (BlockOnPoint(CrossCenter))
            {
                return;
            }

            foreach (iPoint Point in CollectCrossCoords(CrossCenter))
            {
                foreach (int UserId in GetHabboIdsForPoint(Point))
                {
                    FreezeTeam UserTeam = GetTeam(UserId);

                    if (CheckArmour(UserId))
                    {
                        continue;
                    }

                    if (UserTeam.Equals(FreezeTeam.None) || UserTeam.Equals(myTeam))
                    {
                        continue;
                    }

                    goFreezeUser(UserId);
                }
            }
        }

        #endregion

        #region Events

        public void goFreezeUser(int HabboId)
        {
            VirtualRoomUser User = GetUser(HabboId);

            if (User == null)
            {
                return;
            }

            BrickEngine.GetEffectsHandler().RunFreeEffect(User.GetClient(), FREEZE_USER_EFFECT);
        }

        public void goShieldUser(int HabboId)
        {
            VirtualRoomUser User = GetUser(HabboId);

            if (User == null)
            {
                return;
            }

            FreezeTeam UserTeam = GetTeam(HabboId);

            if (UserTeam.Equals(FreezeTeam.None))
            {
                return;
            }

            if (ArmouredUsers.ContainsKey(HabboId))
            {
                return;
            }

            ArmouredUsers.Add(HabboId, DateTime.Now);

            BrickEngine.GetEffectsHandler().RunFreeEffect(User.GetClient(), GetEffectIdForTeam(UserTeam, EffectType.Shield));
        }

        public void goHelmetUser(int HabboId)
        {
            VirtualRoomUser User = GetUser(HabboId);

            if (User == null)
            {
                return;
            }

            FreezeTeam UserTeam = GetTeam(HabboId);

            if (UserTeam.Equals(FreezeTeam.None))
            {
                return;
            }

            BrickEngine.GetEffectsHandler().RunFreeEffect(User.GetClient(), GetEffectIdForTeam(UserTeam, EffectType.Helmet));
        }

        #endregion

        #region PropertiesReturners

        public Boolean CheckFreezed(int HabboId)
        {
            if (FreezedUsers.ContainsKey(HabboId))
            {
                if ((DateTime.Now - FreezedUsers[HabboId]).TotalSeconds >= 5)
                {
                    goShieldUser(HabboId);

                    FreezedUsers.Remove(HabboId);
                    return false;
                }
            }

            return false;
        }

        public Boolean CheckArmour(int HabboId)
        {
            if (ArmouredUsers.ContainsKey(HabboId))
            {
                if ((DateTime.Now - ArmouredUsers[HabboId]).TotalSeconds >= 5)
                {
                    goHelmetUser(HabboId);

                    ArmouredUsers.Remove(HabboId);
                    return false;
                }
            }

            return false;
        }

        public Boolean BlockOnPoint(iPoint Point)
        {
            foreach (Item Item in GetBlocks().Values)
            {
                if (Item.Point.Compare(Point))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetEffectIdForTeam(FreezeTeam Team, EffectType Type)
        {
            if (Type.Equals(EffectType.Helmet))
            {
                if (Team.Equals(FreezeTeam.Red))
                {
                    return RED_HELMET_EFFECT;
                }
                else if (Team.Equals(FreezeTeam.Blue))
                {
                    return BLUE_HELMET_EFFECT;
                }
                else if (Team.Equals(FreezeTeam.Green))
                {
                    return GREEN_HELMET_EFFECT;
                }
                else if (Team.Equals(FreezeTeam.Yellow))
                {
                    return YELLOW_HELMET_EFFECT;
                }
            }
            else if (Type.Equals(EffectType.Shield))
            {
                if (Team.Equals(FreezeTeam.Red))
                {
                    return RED_SHIELD_EFFECT;
                }
                else if (Team.Equals(FreezeTeam.Blue))
                {
                    return BLUE_SHIELD_EFFECT;
                }
                else if (Team.Equals(FreezeTeam.Green))
                {
                    return GREEN_SHIELD_EFFECT;
                }
                else if (Team.Equals(FreezeTeam.Yellow))
                {
                    return YELLOW_SHIELD_EFFECT;
                }
            }

            return -1;
        }

        public FreezeTeam GetFreezeTeamForUser(int HabboId)
        {
            try { return Gamers[HabboId]; }
            catch { return FreezeTeam.None; }
        }

        public List<int> GetUsersForTeam(FreezeTeam Team)
        {
            var List = new List<int>();

            foreach (KeyValuePair<int, FreezeTeam> kvp in Gamers)
            {
                if (kvp.Value.Equals(Team))
                {
                    List.Add(kvp.Key);
                }
            }

            return List;
        }

        #endregion

        #region QuickReturners

        public FreezeTeam GetTeam(int HabboId)
        {
            try { return Gamers[HabboId]; }
            catch
            {
                return FreezeTeam.None;
            }
        }

        public VirtualRoomEngine GetEngine()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive).GetRoomEngine();
        }

        public VirtualRoomUser GetUser(int HabboId)
        {
            return GetEngine().GetUserByHabboId(HabboId);
        }

        #endregion

        #region Garbage & Collection

        private List<int> GetHabboIdsForPoint(iPoint Point)
        {
            var List = new List<int>();

            foreach (VirtualRoomUser User in GetEngine().GetUsers())
            {
                if (User.Point.Compare(Point))
                {
                    List.Add(User.HabboId);
                }
            }

            return List;
        }

        private List<iPoint> CollectCrossCoords(iPoint CenterPoint)
        {
            var PointList = new List<iPoint>();

            short[,] CrossTiles = new short[4, 2] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < CrossTiles.GetLength(0); i++)
            {
                int Xupdater = CrossTiles[i, 0];
                int Yupdater = CrossTiles[i, 1];

                PointList.Add(new iPoint(CenterPoint.X + Xupdater, CenterPoint.Y + Yupdater));
            }

            return PointList;
        }

        #endregion
    }
}
