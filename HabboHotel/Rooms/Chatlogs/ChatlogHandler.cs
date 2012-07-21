using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Rooms.Chatlogs
{
    class ChatlogHandler
    {
        private List<Chatlog> Chatlogs = new List<Chatlog>();

        public ChatlogHandler() { }

        public void AddChatlog(int UserId, int RoomId, string Message)
        {
            Chatlogs.Add(new Chatlog(UserId, RoomId, Message));
        }

        public List<Chatlog> GetChatlogsForUserId(int Id)
        {
            var List = new List<Chatlog>();

            foreach (Chatlog Chatlog in Chatlogs)
            {
                if (Chatlog.UserId == Id)
                {
                    List.Add(Chatlog);
                }
            }

            return (from chatlog in List orderby chatlog.Time ascending select chatlog).ToList();
        }

        public List<Chatlog> GetChatlogsForRoomId(int Id)
        {
            var List = new List<Chatlog>();

            foreach (Chatlog Chatlog in Chatlogs)
            {
                if (Chatlog.RoomId == Id)
                {
                    List.Add(Chatlog);
                }
            }

            return (from chatlog in List orderby chatlog.Time ascending select chatlog).ToList();
        }

        public List<Chatlog> GetChatlogsForRoomId(RoomVisit Visit)
        {
            var List = new List<Chatlog>();

            foreach (Chatlog Chatlog in Chatlogs)
            {
                if (Visit.Updated)
                {
                    if (Chatlog.Time >= Visit.Entered && Chatlog.Time <= Visit.Leaved)
                    {
                        if (Chatlog.RoomId == Visit.RoomId)
                        {
                            List.Add(Chatlog);
                        }
                    }
                }
                else
                {
                    if (Chatlog.Time >= Visit.Entered)
                    {
                        if (Chatlog.RoomId == Visit.RoomId)
                        {
                            List.Add(Chatlog);
                        }
                    }
                }
            }

            return (from chatlog in List orderby chatlog.Time ascending select chatlog).ToList();
        }
    }
}
