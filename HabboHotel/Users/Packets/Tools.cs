using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Pets;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void GetRoomInfo(Client Client, Request Request)
        {
            int RoomId = Request.PopWiredInt32();

            if (RoomId <= 0)
            {
                return;
            }

            Client.SendResponse(BrickEngine.GetToolReactor().GetRoomResponse(Client, RoomId));
        }

        private void GetUserInfo(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            if (UserId <= 0)
            {
                return;
            }

            Client.SendResponse(BrickEngine.GetToolReactor().GetUserInfo(Client, UserId));
        }

        private void GetUserRoomVisits(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            if (UserId <= 0)
            {
                return;
            }

            Client.SendResponse(BrickEngine.GetToolReactor().GetUserRoomVisits(Client, UserId));
        }

        private void GetUserChatlogs(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            if (UserId <= 0)
            {
                return;
            }

            Client.SendResponse(BrickEngine.GetToolReactor().GetUserChatlogs(Client, UserId));
        }

        private void GetRoomChatlogs(Client Client, Request Request)
        {
            // Avoid junk
            Request.PopWiredInt32();

            int RoomId = Request.PopWiredInt32();

            if (RoomId <= 0)
            {
                return;
            }

            Client.SendResponse(BrickEngine.GetToolReactor().GetRoomChatlogs(Client, RoomId));
        }

        private void AlertSelectedUser(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            string Message = BrickEngine.CleanString(Request.PopFixedString());

            if (UserId <= 0)
            {
                return;
            }

            BrickEngine.GetToolReactor().AlertUser(Client, UserId, Message, false, false, false);
        }

        private void KickSelectedUser(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            string Message = BrickEngine.CleanString(Request.PopFixedString());

            if (UserId <= 0)
            {
                return;
            }

            BrickEngine.GetToolReactor().AlertUser(Client, UserId, Message, false, true, false);
        }

        private void WarnSelectedUser(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            string Message = BrickEngine.CleanString(Request.PopFixedString());

            if (UserId <= 0)
            {
                return;
            }

            BrickEngine.GetToolReactor().AlertUser(Client, UserId, Message, true, false, false);
        }

        private void BanSelectedUser(Client Client, Request Request)
        {
            int UserId = Request.PopWiredInt32();

            string Reason = BrickEngine.CleanString(Request.PopFixedString());

            int DurationHours = Request.PopWiredInt32();

            if (UserId <= 0)
            {
                return;
            }

            BrickEngine.GetToolReactor().BanUser(Client, UserId, Reason, DurationHours, false);
        }

        private void AlertSelectedRoom(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            // Avoid Junk
            Request.PopWiredInt32();

            Boolean Warning = !Request.PopWiredInt32().Equals(3);

            string Message = BrickEngine.CleanString(Request.PopFixedString());

            BrickEngine.GetToolReactor().AlertRoom(Client, Client.GetUser().RoomId, Message, Warning);
        }

        private void PreformAction(Client Client, Request Request)
        {
            int RoomId = Request.PopWiredInt32();

            if (RoomId <= 0)
            {
                return;
            }

            Boolean SetDoorBell = Request.PopWiredBoolean();
            Boolean SetRoomName = Request.PopWiredBoolean();
            Boolean KickUsers = Request.PopWiredBoolean();

            BrickEngine.GetToolReactor().HandleRoom(Client, RoomId, SetDoorBell, SetRoomName, KickUsers);
        }

        private void GetPetsTool(Client Client, Request Request)
        {            
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int PetId = Request.PopWiredInt32();

            VirtualRoomUser PetUser = Client.GetUser().GetRoom().GetRoomEngine().GetUserByPetId(PetId);

            if (PetUser == null)
            {
                return;
            }

            PetInfo Info = BrickEngine.GetPetReactor().GetPetInfo(PetId);

            if (Info == null)
            {
                return;
            }

            Response Response = new Response(605);
            Response.AppendInt32(PetId);
            Response.AppendInt32(PetReactor.MAX_LEVEL);

            for (int i = 0; i <= PetReactor.MAX_LEVEL; i++)
            {
                Response.AppendInt32(i);
            }

            for (int i = 0; i <= Info.Level; i++)
            {
                Response.AppendInt32(i);
            }

            Client.SendResponse(Response);
        }
    }
}
