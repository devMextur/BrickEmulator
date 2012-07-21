using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Rooms.Virtual;

namespace BrickEmulator.HabboHotel.Rooms
{
    class RoomEvent
    {
        public readonly int RoomId;

        public string Name;
        public string Description;

        public int CategoryId;
        public List<string> Tags;

        public DateTime Started = DateTime.Now;

        public RoomEvent(int RoomId, string Name, string Description, int CategoryId, List<string> Tags)
        {
            this.RoomId = RoomId;
            this.Name = Name;
            this.Description = Description;
            this.CategoryId = CategoryId;
            this.Tags = Tags;
        }

        public void GetResponse(Response Response)
        {
            Response.AppendRawInt32(GetRoom().OwnerId);
            Response.AppendChar(2);
            Response.AppendRawInt32(RoomId);
            Response.AppendChar(2);
            Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(GetRoom().OwnerId));
            Response.AppendInt32(CategoryId);
            Response.AppendStringWithBreak(Name);
            Response.AppendStringWithBreak(Description);
            Response.AppendStringWithBreak(Started.ToShortTimeString());
            Response.AppendInt32(Tags.Count);
            Tags.ToList().ForEach(Response.AppendStringWithBreak);
        }

        public void Drop()
        {
            Response EndResponse = new Response(370);
            EndResponse.AppendRawInt32(-1);
            EndResponse.AppendChar(2);
            GetRoom().GetRoomEngine().BroadcastResponse(EndResponse);

            GetRoom().Event = null;

            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        public VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);
        }
    }
}
