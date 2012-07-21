using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using System.Data;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;

namespace BrickEmulator.HabboHotel.Shop.Ecotron
{
    class EcotronReactor
    {
        #region Fields
        private const int LEVEL_1_CHANGE = 0;
        private const int LEVEL_2_CHANGE = 4;
        private const int LEVEL_3_CHANGE = 40;
        private const int LEVEL_4_CHANGE = 200;
        private const int LEVEL_5_CHANGE = 2000;

        private Dictionary<int, EcotronReward> Rewards;
        private Dictionary<int, DateTime> TimeToWait = new Dictionary<int,DateTime>();

        private Random Random = new Random();
        #endregion

        #region Constructors
        public EcotronReactor()
        {
            LoadRewards();
        }
        #endregion

        #region Methods
        public void LoadRewards()
        {
            Rewards = new Dictionary<int, EcotronReward>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM ecotron_rewards");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                EcotronReward Reward = new EcotronReward(Row);

                Rewards.Add(Reward.Id, Reward);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Rewards.Count + "] EcotronReward(s) cached.", IO.WriteType.Outgoing);
        }

        public Boolean GainReward(Client Client)
        {
            EcotronReward Reward = DeliverReward();

            if (Reward == null)
            {
                return false;
            }

            BaseItem EcotronBox = BrickEngine.GetFurniReactor().GetSpecifiqueItem("ecotron_box");

            if (EcotronBox == null)
            {
                return false;
            }

            int BoxId = BrickEngine.GetItemReactor().InsertItem(Client.GetUser().HabboId, EcotronBox.Id, DateTime.Now.ToShortDateString(), Reward.BaseId);

            Response Response = new Response(832);
            Response.AppendInt32(1);
            Response.AppendInt32(EcotronBox.InternalType.ToLower().Equals("s") ? 1 : 2);
            Response.AppendInt32(1);
            Response.AppendInt32(BoxId);
            Client.SendResponse(Response);

            Response Box = new Response(508);
            Box.AppendBoolean(true);
            Box.AppendInt32(BoxId);
            Client.SendResponse(Box);

            TimeToWait.Add(Client.GetUser().HabboId, DateTime.Now.AddMinutes(3));

            return true;
        }

        public int GetChangeByLevel(int Level)
        {
            if (Level.Equals(1))
            {
                return LEVEL_1_CHANGE;
            }
            else if (Level.Equals(2))
            {
                return LEVEL_2_CHANGE;
            }
            else if (Level.Equals(3))
            {
                return LEVEL_3_CHANGE;
            }
            else if (Level.Equals(4))
            {
                return LEVEL_4_CHANGE;
            }
            else if (Level.Equals(5))
            {
                return LEVEL_5_CHANGE;
            }

            return LEVEL_1_CHANGE;
        }

        public EcotronReward DeliverReward()
        {
            int Level = 1;

            for (int i = 5; i > 1; i--)
            {
                if (Level > 1)
                {
                    continue;
                }

                int Change = GetChangeByLevel(i);

                if (Random.Next(1, Change) == Change)
                {
                    Level = i;
                }
            }

            if (GetRewardsForLevel(Level).Count > 0)
            {
                return GetRewardsForLevel(Level)[Random.Next(0, GetRewardsForLevel(Level).Count - 1)];
            }

            return null;
        }

        public List<EcotronReward> GetRewardsForLevel(int Level)
        {
            var List = new List<EcotronReward>();

            foreach (EcotronReward Reward in Rewards.Values.ToList())
            {
                if (Reward.Level.Equals(Level))
                {
                    List.Add(Reward);
                }
            }

            return List;
        }

        public int GetTimerTime(int HabboId)
        {
            return (GetTimeToWait(HabboId) > 0) ? 3 : 1;
        }

        public int GetTimeToWait(int HabboId)
        {
            if (TimeToWait.ContainsKey(HabboId))
            {
                if (BrickEngine.GetConvertor().ObjectToInt32((TimeToWait[HabboId] - DateTime.Now).TotalSeconds) <= 0)
                {
                    TimeToWait.Remove(HabboId);
                    return 0;
                }

                return BrickEngine.GetConvertor().ObjectToInt32((TimeToWait[HabboId] - DateTime.Now).TotalSeconds);
            }

            return 0;
        }
        #endregion
    }
}
