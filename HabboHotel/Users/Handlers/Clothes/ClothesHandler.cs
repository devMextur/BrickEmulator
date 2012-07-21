using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Storage;
using BrickEmulator.Security;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Clothes
{
    class ClothesHandler
    {
        #region Fields
        private Dictionary<int, Dictionary<int, Clothe>> Clothes;
        #endregion

        #region Constructors
        public ClothesHandler()
        {
            LoadClothes();
        }
        #endregion

        #region Methods

        public void LoadClothes()
        {
            Clothes = new Dictionary<int, Dictionary<int, Clothe>>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_clothes ORDER BY slot_id ASC");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach(DataRow Row in Table.Rows)
                {
                    Clothe Clothe = new Clothe(Row);

                    if (!Clothes.ContainsKey(Clothe.HabboId))
                    {
                        Clothes.Add(Clothe.HabboId, new Dictionary<int, Clothe>());
                    }

                    if (!Clothes[Clothe.HabboId].ContainsKey(Clothe.SlotId) && Clothe.SlotId >= 1 && Clothe.SlotId <= 10)
                    {
                        Clothes[Clothe.HabboId].Add(Clothe.SlotId, Clothe);
                    }
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Table.Rows.Count + "] ClotheCache(s) cached.", IO.WriteType.Outgoing);
        }

        public Dictionary<int, Clothe> GetClothes(int HabboId)
        {
            try { return Clothes[HabboId]; }
            catch { return new Dictionary<int, Clothe>(); }
        }

        public Response GetWardRobeResponse(int HabboId, int Max)
        {
            Response Response = new Response(267);
            Response.AppendBoolean(true);

            var Items = (from clothe in GetClothes(HabboId).Values.ToList() orderby clothe.SlotId ascending select clothe).ToList();

            Response.AppendInt32(Items.Count);

            int i = 0;

            foreach (Clothe Clothe in Items)
            {
                if (i < Max)
                {
                    Clothe.GetResponse(Response);
                    i++;
                }
            }

            return Response;
        }

        public void UpdateClothe(int HabboId, Clothe Clothe)
        {
            if (!Clothes.ContainsKey(HabboId))
            {
                Clothes.Add(HabboId, new Dictionary<int, Clothe>());
            }

            if (!Clothes[HabboId].ContainsKey(Clothe.SlotId))
            {
                Clothes[HabboId].Add(Clothe.SlotId, Clothe);

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("INSERT INTO user_clothes (user_id, slot_id, look, gender) VALUES (@habboid, @slotid, @look, @gender)");
                    Reactor.AddParam("look", Clothe.Look);
                    Reactor.AddParam("gender", Clothe.Gender);
                    Reactor.AddParam("habboid", HabboId);
                    Reactor.AddParam("slotid", Clothe.SlotId);
                    Reactor.ExcuteQuery();
                }
            }
            else
            {
                Clothes[HabboId][Clothe.SlotId] = Clothe;

                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE user_clothes SET look = @look, gender = @gender WHERE user_id = @habboid AND slot_id = @slotid LIMIT 1");
                    Reactor.AddParam("look", Clothe.Look);
                    Reactor.AddParam("gender", Clothe.Gender);
                    Reactor.AddParam("habboid", HabboId);
                    Reactor.AddParam("slotid", Clothe.SlotId);
                    Reactor.ExcuteQuery();
                }
            }
        }

        #endregion
    }
}
