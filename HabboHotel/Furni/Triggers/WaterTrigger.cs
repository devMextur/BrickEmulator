using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;

namespace BrickEmulator.HabboHotel.Furni.Triggers
{
    struct WaterTrigger: IFurniTrigger
    {
        public void OnPlace(Item Item, VirtualRoomUser User) { }

        public void OnUpdate(Item Item, VirtualRoomUser User) { }

        public void OnRemove(Item Item, VirtualRoomUser User) { }

        public void OnPointInteract(Item Item, VirtualRoomUser User)
        {
            if (User.IsPet)
            {
                User.AddStatus("swm", string.Empty);
            }
            else
            {
                if (Item.GetBaseItem().InternalName.EndsWith("_1"))
                {
                    BrickEngine.GetEffectsHandler().RunFreeEffect(User.GetClient(), 30);
                }
                else if (Item.GetBaseItem().InternalName.EndsWith("_2"))
                {
                    BrickEngine.GetEffectsHandler().RunFreeEffect(User.GetClient(), 29);
                }
            }
        }

        public void OnTrigger(int TriggerId, Item Item, VirtualRoomUser User) { }
    }
}
