using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger.Streaming
{
    class StreamHandler
    {
        private List<Stream> Streams = new List<Stream>();

        public StreamHandler() { }

        private List<Stream> GetStreamsForClient(Client Client)
        {
            var List = new List<Stream>();

            foreach (Stream Stream in (from stream in Streams orderby stream.RunningTime ascending select stream))
            {
                if (Stream.HabboId == Client.GetUser().HabboId)
                {
                    List.Add(Stream);
                    continue;
                }

                if (BrickEngine.GetMessengerHandler().HasFriend(Client.GetUser().HabboId, Stream.HabboId))
                {
                    List.Add(Stream);
                }
            }

            return (from stream in List orderby stream.RunningTime.TotalMinutes ascending select stream).ToList();
        }

        public Response GetResponse(Client Client)
        {
            var Streams = GetStreamsForClient(Client);

            Response Response = new Response(950);
            Response.AppendInt32(Streams.Count);

            foreach (Stream Stream in Streams)
            {
                Stream.GetResponse(Response, Client.GetUser().HabboId);
            }

            return Response;
        }

        public void AddStream(int HabboId, StreamType StreamType)
        {
            Streams.Add(new Stream(HabboId, -1, new Object(), StreamType));
        }

        public void AddStream(int HabboId, StreamType StreamType, Object AchievementItem)
        {
            Streams.Add(new Stream(HabboId, -1, AchievementItem, StreamType));
        }

        public void AddStream(int HabboId, StreamType StreamType, int AchievedItemId)
        {
            Streams.Add(new Stream(HabboId, AchievedItemId, new Object(), StreamType));
        }

        public void AddStream(int HabboId, StreamType StreamType, int AchievedItemId, Object AchievedItem)
        {
            Streams.Add(new Stream(HabboId, AchievedItemId, AchievedItem, StreamType));
        }

        public short GetFriendState(int UserId, int SelectedId)
        {
            if (UserId == SelectedId)
            {
                return -1;
            }

            if (BrickEngine.GetMessengerHandler().HasFriend(UserId, SelectedId))
            {
                return -1;
            }

            return GetSecondairStreamIndexer(StreamType.MadeFriends);
        }

        public short GetPrimairStreamIndexer(StreamType StreamType)
        {
            if (StreamType.Equals(StreamType.AchievedAchievement))
            {
                return 2;
            }
            else if (StreamType.Equals(StreamType.EditedMotto))
            {
                return 3;
            }
            else if (StreamType.Equals(StreamType.RatedRoom))
            {
                return 1;
            }
            else if (StreamType.Equals(StreamType.MadeFriends))
            {
                return 0;
            }

            return -1;
        }

        public short GetSecondairStreamIndexer(StreamType StreamType)
        {
            if (StreamType.Equals(StreamType.AchievedAchievement))
            {
                return 3;
            }
            else if (StreamType.Equals(StreamType.EditedMotto))
            {
                return 4;
            }
            else if (StreamType.Equals(StreamType.RatedRoom))
            {
                return 2;
            }
            else if (StreamType.Equals(StreamType.MadeFriends))
            {
                return 5;
            }

            return -1;
        }
    }
}
