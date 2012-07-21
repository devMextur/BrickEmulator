using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void GetAchievementResponse(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetAchievementReactor().GetResponse(Client));
        }

        private void GetQuestList(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetQuestReactor().GetResponse(Client));
        }
    }
}
