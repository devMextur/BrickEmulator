using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.Storage;

namespace BrickEmulator.HabboHotel.Furni.Triggers
{
    struct DefaultTrigger : IFurniTrigger
    {
        public void OnPlace(Item Item, VirtualRoomUser User) { }

        public void OnUpdate(Item Item, VirtualRoomUser User) { }

        public void OnRemove(Item Item, VirtualRoomUser User) { }

        public void OnPointInteract(Item Item, VirtualRoomUser User)
        {
            if (!User.IsPet)
            {
                if (BrickEngine.GetEffectsHandler().UserHasRunningEffect(User.HabboId))
                {
                    BrickEngine.GetEffectsHandler().RunFreeEffect(User.GetClient(), -1);
                }
            }
        }

        public void OnTrigger(int TriggerId, Item Item, VirtualRoomUser User)
        {
            int CurrentState = 0;

            int.TryParse(Item.ExtraData, out CurrentState);

            CurrentState++;

            if (CurrentState > Item.GetBaseItem().InteractorAmount)
            {
                CurrentState = 0;
            }

            Item.ExtraData = BrickEngine.GetConvertor().ObjectToString(CurrentState);

            User.GetRoom().GetRoomEngine().BroadcastResponse(Item.GetTriggerResponse());

            // Update Info & MySQL
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE items SET extra_data = @extra WHERE id = @itemid LIMIT 1");
                Reactor.AddParam("itemid", Item.Id);
                Reactor.AddParam("extra", CurrentState);
                Reactor.ExcuteQuery();
            }
        }
    }
}
