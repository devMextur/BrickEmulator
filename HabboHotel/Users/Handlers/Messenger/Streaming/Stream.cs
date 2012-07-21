using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger.Streaming
{
    class Stream
    {
        public readonly int HabboId;
        public readonly StreamType StreamType;
        public readonly DateTime Achieved = DateTime.Now;

        public readonly int AchievedItemId;
        public readonly Object AchievedItem;

        public TimeSpan RunningTime
        {
            get
            {
                return (DateTime.Now - Achieved);
            }
        }

        public Stream(int HabboId, int AchievedItemId, Object AchievedItem, StreamType StreamType)
        {
            this.HabboId = HabboId;
            this.AchievedItemId = AchievedItemId;
            this.AchievedItem = AchievedItem;
            this.StreamType = StreamType;
        }

        public void GetResponse(Response Response, int UserId)
        {
            Response.AppendInt32(-1);
            Response.AppendInt32(BrickEngine.GetStreamHandler().GetPrimairStreamIndexer(StreamType));

            Response.AppendRawInt32(HabboId);
            Response.AppendChar(2);

            Response.AppendStringWithBreak(GetUsername());
            Response.AppendStringWithBreak(GetGender().ToLower());

            if (StreamType.Equals(StreamType.AchievedAchievement))
            {
                Response.AppendStringWithBreak("http://www.habbo.com/habbo-imaging/badge/" + AchievedItem.ToString() + ".png");
            }
            else
            {
                Response.AppendStringWithBreak(BrickEngine.GetConfigureFile().CallStringKey("site.link") + "/habbo-imaging/avatar/" + GetLook() + ".gif");
            }

            Response.AppendInt32(BrickEngine.GetConvertor().ObjectToInt32(RunningTime.TotalMinutes));

            if (StreamType.Equals(StreamType.MadeFriends))
            {
                Response.AppendInt32(BrickEngine.GetStreamHandler().GetFriendState(UserId, AchievedItemId));
            }
            else
            {
                Response.AppendInt32(BrickEngine.GetStreamHandler().GetSecondairStreamIndexer(StreamType));
            }

            Response.AppendBoolean(true);
            Response.AppendBoolean(true);

            if (StreamType.Equals(StreamType.RatedRoom) || StreamType.Equals(StreamType.MadeFriends))
            {
                Response.AppendRawInt32(AchievedItemId);
                Response.AppendChar(2);

                if (StreamType.Equals(StreamType.MadeFriends))
                {
                    Response.AppendStringWithBreak(BrickEngine.GetUserReactor().GetUsername(AchievedItemId));
                }
                else
                {
                    Response.AppendStringWithBreak(AchievedItem.ToString());
                }
            }
            else
            {
                Response.AppendStringWithBreak(AchievedItem.ToString());
            }
        }

        public string GetUsername()
        {
            return BrickEngine.GetUserReactor().GetUsername(HabboId);
        }

        public string GetMotto()
        {
            return BrickEngine.GetUserReactor().GetMotto(HabboId);
        }

        public string GetLook()
        {
            return BrickEngine.GetUserReactor().GetLook(HabboId);
        }

        public string GetGender()
        {
            return BrickEngine.GetUserReactor().GetGender(HabboId);
        }
    }
}
