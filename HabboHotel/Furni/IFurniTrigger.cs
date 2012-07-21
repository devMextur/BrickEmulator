using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Furni
{
    interface IFurniTrigger
    {
        void OnPlace(Item Item, VirtualRoomUser User);
        void OnUpdate(Item Item, VirtualRoomUser User);
        void OnRemove(Item Item, VirtualRoomUser User);
        void OnPointInteract(Item Item, VirtualRoomUser User);
        void OnTrigger(int TriggerId, Item Item, VirtualRoomUser User);
    }
}
