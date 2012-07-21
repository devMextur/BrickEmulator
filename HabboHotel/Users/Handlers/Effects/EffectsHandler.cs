using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using BrickEmulator.Storage;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Effects
{
    class EffectsHandler
    {
        #region Fields
        private Timer EffectsChecker;

        private Security.SecurityCounter EffectIdCounter = new Security.SecurityCounter(0);

        private Dictionary<int, Effect> Effects;

        // Key = HabboId, Value = CurrentEffect
        private Dictionary<int, Effect> RunningEffects = new Dictionary<int, Effect>();
        #endregion

        #region Constructors
        public EffectsHandler()
        {
            LoadEffects();

            EffectsChecker = new Timer(new TimerCallback(CheckEffects), EffectsChecker, 0, 1000);
        }
        #endregion

        #region Methods
        public void LoadEffects()
        {
            Effects = new Dictionary<int, Effect>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT MAX(id) FROM user_effects LIMIT 1");
                EffectIdCounter = new Security.SecurityCounter(Reactor.GetInt32());
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_effects");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                Effect Effect = new Effect(Row);

                Effects.Add(Effect.Id, Effect);
            }
        }

        public List<Effect> GetEffectsForUser(int HabboId)
        {
            var List = new List<Effect>();

            foreach (Effect Effect in Effects.Values.ToList())
            {
                if (Effect.UserId == HabboId)
                {
                    List.Add(Effect);
                }
            }

            return List;
        }

        public Effect GetLastAddedEffect(int HabboId, int EffectId)
        {
            var Effects = new List<Effect>();

            foreach (Effect Effect in GetEffectsForUser(HabboId))
            {
                if (Effect.EffectId == EffectId)
                {
                    Effects.Add(Effect);
                }
            }

            var Sorted = (from effect in Effects orderby effect.Id descending select effect).ToList();

            return (from effect in Sorted orderby effect.Activated descending select effect).ToList()[0];
        }

        public Boolean UserHasRunningEffect(int HabboId)
        {
            return GetRunningEffect(HabboId) != null;
        }

        public Effect GetRunningEffect(int HabboId)
        {
            try { return RunningEffects[HabboId]; }
            catch { return null; }
        }

        public List<Effect> GetRunningEffects(int HabboId)
        {
            var List = new List<Effect>();

            foreach (Effect Effect in Effects.Values.ToList())
            {
                if (Effect.UserId == HabboId)
                {
                    if (Effect.IsActivated)
                    {
                        List.Add(Effect);
                    }
                }
            }

            return List;
        }

        public List<Effect> GetRunningEffects()
        {
            var List = new List<Effect>();

            foreach (Effect Effect in Effects.Values.ToList())
            {
                if (Effect.IsActivated)
                {
                    List.Add(Effect);
                }
            }

            return List;
        }

        private void CheckEffects(Object e)
        {
            foreach (Effect Effect in GetRunningEffects())
            {
                if (Effect.RemainingTime == 0)
                {
                    DeleteEffect(Effect);
                }
            }
        }

        public void GetResponse(Client Client, Response Response)
        {
            var Effects = GetEffectsForUser(Client.GetUser().HabboId);

            Response.Initialize(460);
            Response.AppendInt32(Effects.Count);

            foreach (Effect Effect in Effects)
            {
                Effect.GetResponse(Response);
            }
        }

        public void InsertEffect(Client Client, int EffectId, int Duration)
        {
            Dictionary<int, Object> Row = new Dictionary<int, object>();

            Row[0] = EffectIdCounter.Next;
            Row[1] = Client.GetUser().HabboId;
            Row[2] = EffectId;
            Row[3] = Duration;
            Row[4] = string.Empty;

            Effect Effect = new Effect(Row);

            Effects.Add(Effect.Id, Effect);

            Response Response = new Response(461);
            Response.AppendInt32(EffectId);
            Response.AppendInt32(Duration);
            Client.SendResponse(Response);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO user_effects (user_id, effect_id, effect_length) VALUES (@habboid, @effectid, @length)");
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.AddParam("effectid", EffectId);
                Reactor.AddParam("length", Duration);
                Reactor.ExcuteQuery();
            }
        }

        public void ActivateEffect(Client Client, Effect Effect)
        {
            if (Effect.RemainingTime == 0)
            {
                return;
            }

            Effect.Excecute();

            if (Client != null)
            {
                Response Response = new Response(462);
                Response.AppendInt32(Effect.EffectId);
                Response.AppendInt32(Effect.EffectLength);
                Client.SendResponse(Response);
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE user_effects SET activated = @dt WHERE id = @effectid LIMIT 1");
                Reactor.AddParam("dt", Effect.Activated); 
                Reactor.AddParam("effectid", Effect.Id);
                Reactor.ExcuteQuery();
            }
        }

        public void RunFreeEffect(Client Client, int EffectId)
        {
            Client.GetUser().HasFreeEffect = true;

            if (EffectId > 0)
            {
                Dictionary<int, Object> Row = new Dictionary<int, object>();

                Row[0] = -1;
                Row[1] = Client.GetUser().HabboId;
                Row[2] = EffectId;
                Row[3] = int.MaxValue;
                Row[4] = string.Empty;

                Effect GeneratedEffect = new Effect(Row);

                if (RunningEffects.ContainsKey(Client.GetUser().HabboId))
                {
                    RunningEffects[Client.GetUser().HabboId] = GeneratedEffect;
                }
                else
                {
                    RunningEffects.Add(Client.GetUser().HabboId, GeneratedEffect);
                }
            }
            else
            {
                RunningEffects.Remove(Client.GetUser().HabboId);
            }

            if (Client.GetUser().IsInRoom)
            {
                Response Response = new Response(485);
                Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);
                Response.AppendInt32(EffectId);
                Client.SendRoomResponse(Response);
            }
        }

        public void RunEffect(Client Client, Effect Effect)
        {
            Client.GetUser().HasFreeEffect = false;

            if (Effect != null)
            {
                if (Effect.RemainingTime == 0)
                {
                    return;
                }
            }

            Response Response = new Response(485);

            if (Client.GetUser().IsInRoom)
            {
                Response.AppendInt32(Client.GetUser().GetRoomUser().VirtualId);
            }

            if (Effect == null)
            {
                if (Client.GetUser().IsInRoom)
                {
                    Response.AppendInt32(-1);
                }

                RunningEffects.Remove(Client.GetUser().HabboId);
            }
            else
            {
                if (!Effect.IsActivated)
                {
                    ActivateEffect(Client, Effect);
                }

                if (Client.GetUser().IsInRoom)
                {
                    Response.AppendInt32(Effect.EffectId);
                }

                if (RunningEffects.ContainsKey(Client.GetUser().HabboId))
                {
                    RunningEffects[Effect.UserId] = Effect;
                }
                else
                {
                    RunningEffects.Add(Client.GetUser().HabboId, Effect);
                }
            }

            if (Client.GetUser().IsInRoom)
            {
                Client.SendRoomResponse(Response);
            }
        }

        private void DeleteEffect(Effect Effect)
        {
            if (Effects.ContainsKey(Effect.Id))
            {
                Effects.Remove(Effect.Id);
            }

            if (BrickEngine.GetUserReactor().IsOnline(Effect.UserId))
            {
                Response Response = new Response(463);
                Response.AppendInt32(Effect.EffectId);

                BrickEngine.GetSocketShield().GetSocketClientByHabboId(Effect.UserId).GetClient().SendResponse(Response);

                if (RunningEffects.ContainsKey(Effect.UserId))
                {
                    if (RunningEffects[Effect.UserId] == Effect)
                    {
                        RunEffect(BrickEngine.GetSocketShield().GetSocketClientByHabboId(Effect.UserId).GetClient(), null);
                    }
                }
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM user_effects WHERE id = @effectid LIMIT 1");
                Reactor.AddParam("effectid", Effect.Id);
                Reactor.ExcuteQuery();
            }
        }

        #endregion
    }
}
