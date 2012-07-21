using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Tools
{
    class Issue
    {
        public readonly int Id;

        public int State;
        public int CategoryId;
        public int ReportedCategoryId;

        public DateTime TimeStampReport;

        public int Priority;
        public int ReporterId;

        public string ReporterUsername
        {
            get
            {
                return BrickEngine.GetUserReactor().GetUsername(ReporterId);
            }
        }

        public int ModeratorId;

        public string ModeratorUsername
        {
            get
            {
                return BrickEngine.GetUserReactor().GetUsername(ModeratorId);
            }
        }

        public string IssueMessage;
        public int RoomId;

        public string RoomName
        {
            get
            {
                return BrickEngine.GetRoomReactor().GetRoomName(RoomId);
            }
        }

        public int RoomCategory
        {
            get
            {
                return BrickEngine.GetRoomReactor().GetRoomCategory(RoomId);
            }
        }

        public Issue()
        {
        }

        public void GetResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendInt32(State);
            Response.AppendInt32(CategoryId);
            Response.AppendInt32(ReportedCategoryId);
            Response.AppendInt32(0); // TimeStampInteger
            Response.AppendInt32(Priority);
            Response.AppendInt32(ReporterId);
            Response.AppendStringWithBreak(ReporterUsername);
            Response.AppendInt32(ModeratorId);
            Response.AppendStringWithBreak(ModeratorUsername);
            Response.AppendStringWithBreak(IssueMessage);
            Response.AppendInt32(RoomId);
            Response.AppendStringWithBreak(RoomName);
            Response.AppendInt32(0);
            Response.AppendStringWithBreak(string.Empty);
            Response.AppendInt32(RoomCategory);
            Response.AppendInt32(0);
        }
    }
}
