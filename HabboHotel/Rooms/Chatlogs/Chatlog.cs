using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrickEmulator.HabboHotel.Rooms.Chatlogs
{
    class Chatlog
    {
        public readonly int UserId;
        public readonly int RoomId;
        public readonly string Message;

        public DateTime Time = DateTime.Now;

        public Chatlog(int UserId, int RoomId, string Message)
        {
            this.UserId = UserId;
            this.RoomId = RoomId;
            this.Message = Message;
        }
    }
}
