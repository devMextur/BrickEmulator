using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger
{
    class Request
    {
        public readonly int HabboId;

        public string Username
        {
            get
            {
                return BrickEngine.GetUserReactor().GetUsername(HabboId);
            }
        }

        public string Look
        {
            get
            {
                return BrickEngine.GetUserReactor().GetLook(HabboId);
            }
        }

        public Request(int HabboId)
        {
            this.HabboId = HabboId;
        }

        public void GetResponse(Response Response)
        {
            Response.AppendInt32(HabboId);
            Response.AppendStringWithBreak(Username);
            Response.AppendStringWithBreak(Look);
        }
    }
}
