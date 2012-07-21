using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.Storage;

namespace BrickEmulator.HabboHotel.Furni.Triggers
{
    struct TeleportTrigger : IFurniTrigger
    {
        public void OnPlace(Item Item, VirtualRoomUser User) { }

        public void OnUpdate(Item Item, VirtualRoomUser User) { }

        public void OnRemove(Item Item, VirtualRoomUser User) { }

        public void OnPointInteract(Item Item, VirtualRoomUser User) { }

        public void OnTrigger(int TriggerId, Item Item, VirtualRoomUser User)
        {
            if (User.Point.Compare(Item.FontPoint))
            {
                User.GetClient().Notif("Teleporters are disabled.", false);
            }
        }
    }
}
