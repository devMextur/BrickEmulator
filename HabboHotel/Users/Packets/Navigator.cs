using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void GetPrivates(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetPrivateResponse());
        }

        private void GetFeatured(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetFeaturedResponse());
        }

        private void GetMe(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetResponseForUser(Client));
        }

        private void GetRooms(Client Client, Request Request)
        {
            int Category = int.Parse(Request.PopFixedString());

            Client.SendResponse(BrickEngine.GetNavigatorManager().GetRoomsResponse(Category));
        }

        private void GetEventRooms(Client Client, Request Request)
        {
            int Category = int.Parse(Request.PopFixedString());

            Client.SendResponse(BrickEngine.GetNavigatorManager().GetEventRoomsResponse(Category));
        }

        private void GetHighestScore(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetHighestScore());
        }

        private void GetPopulairTags(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetPopulairTags());
        }

        private void GetFavoriteRooms(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetFavoriteRooms(Client));
        }

        private void GetLastVisitedRooms(Client Client, Request Request)
        {
            Client.SendResponse(BrickEngine.GetNavigatorManager().GetLastVisited(Client));
        }

        private void SearchRooms(Client Client, Request Request)
        {
            string Query = string.Empty;

            if (Request.Length > 3)
            {
                Query = Request.PopFixedString();
            }

            Client.SendResponse(BrickEngine.GetNavigatorManager().GetSearchResponse(Query));
        }

        private void CheckRoomCreate(Client Client, Request Request)
        {
            int Limit = BrickEngine.GetConfigureFile().CallIntKey("max.rooms.amount");
            int RoomAmount = BrickEngine.GetRoomReactor().GetMe(Client.GetUser().HabboId).Count;

            Response Response = new Response(512);
            Response.AppendBoolean(RoomAmount >= Limit);
            Response.AppendInt32(Limit);
            Client.SendResponse(Response);
        }

        private void CreateRoom(Client Client, Request Request)
        {
            int Limit = BrickEngine.GetConfigureFile().CallIntKey("max.rooms.amount");
            int RoomAmount = BrickEngine.GetRoomReactor().GetMe(Client.GetUser().HabboId).Count;

            if (RoomAmount >= Limit)
            {
                Client.Notif("You're over the rooms limit, first delete a room before you create a new one.", false);
                return;
            }

            string RawName = Request.PopFixedString();
            string RawModel = Request.PopFixedString();

            int RoomId = -1;

            if ((RoomId = BrickEngine.GetRoomReactor().CreateRoom(Client, RawName, RawModel)) > 0)
            {
                Response Response = new Response(59);
                Response.AppendInt32(RoomId);
                Response.AppendStringWithBreak(BrickEngine.CleanString(RawName));
                Client.SendResponse(Response);
            }
            else
            {
                CheckRoomCreate(Client, null);
            }
        }

        private void UpdateHomeRoom(Client Client, Request Request)
        {            
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            int RoomId = Request.PopWiredInt32();

            if (!Client.GetUser().GetRoom().GetRoomEngine().HasRights(Client.GetUser().HabboId, Rooms.RightsType.Founder))
            {
                // You can't use anothers room.
                return;
            }

            if (Client.GetUser().HomeRoomId == RoomId)
            {
                return;
            }

            Client.GetUser().HomeRoomId = RoomId;

            Response Response = new Response(455);
            Response.AppendInt32(RoomId);
            Client.SendResponse(Response);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET home_room_id = @roomid WHERE id = @habboid LIMIT 1");
                Reactor.AddParam("roomid", RoomId);
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.ExcuteQuery();
            }
        }

        private void UpdateFavorite(Client Client, Request Request)
        {
            int RoomId = Request.PopWiredInt32();

            string Query = string.Empty;

            Response Response = new Response(459);
            Response.AppendInt32(RoomId);

            if (Client.GetUser().FavoriteRoomIds.Contains(RoomId))
            {
                Client.GetUser().FavoriteRoomIds.Remove(RoomId);

                Query = "DELETE FROM user_favorite_rooms WHERE user_id = @habboid AND room_id = @roomid LIMIT 1";

                Response.AppendBoolean(false);
            }
            else
            {
                Client.GetUser().FavoriteRoomIds.Add(RoomId);

                Query = "INSERT INTO user_favorite_rooms (user_id, room_id) VALUES (@habboid, @roomid)";

                Response.AppendBoolean(true);
            }

            Client.SendResponse(Response);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery(Query);
                Reactor.AddParam("habboid", Client.GetUser().HabboId);
                Reactor.AddParam("roomid", RoomId);
                Reactor.ExcuteQuery();
            }
        }

        private void GoHotelView(Client Client, Request Request)
        {
            if (!Client.GetUser().IsInRoom)
            {
                return;
            }

            Client.GetUser().GetRoomUser().UnhandledGoalPoint = Client.GetUser().GetRoom().GetRoomModel().Door;
        }
    }
}
