using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Messenger
{
    class Friend
    {
        public readonly int HabboId;

        public string Username
        {
            get
            {
                return BrickEngine.GetUserReactor().GetUsername(HabboId);
            }
        }

        public string Motto
        {
            get
            {
                if (!IsAlive)
                {
                    return string.Empty;
                }

                return BrickEngine.GetUserReactor().GetMotto(HabboId);
            }
        }

        public string Look
        {
            get
            {
                if (!IsAlive)
                {
                    return string.Empty;
                }

                return BrickEngine.GetUserReactor().GetLook(HabboId);
            }
        }

        public string LastVisit
        {
            get
            {
                if (IsAlive)
                {
                    return string.Empty;
                }

                return BrickEngine.GetUserReactor().GetLastVisit(HabboId);
            }
        }

        public Boolean IsAlive
        {
            get
            {
                if (GetClient() != null)
                {
                    return GetClient().IsValidUser;
                }

                return false;
            }
        }

        public Boolean InRoom
        {
            get
            {
                if (!IsAlive)
                {
                    return false; 
                }

                return GetClient().GetUser().IsInRoom && GetClient().GetUser().EnableFollow;
            }
        }

        public Friend(int HabboId)
        {
            this.HabboId = HabboId;
        }

        public void GetSerializeResponse(int CategoryId, Response Response)
        {
            Response.AppendInt32(HabboId);
            Response.AppendStringWithBreak(Username);
            Response.AppendBoolean(true);
            Response.AppendBoolean(IsAlive);
            Response.AppendBoolean(InRoom);
            Response.AppendStringWithBreak(Look);
            Response.AppendInt32(CategoryId);
            Response.AppendStringWithBreak(Motto);
            Response.AppendStringWithBreak(LastVisit);
            Response.AppendChar(2);
            Response.AppendChar(2);
        }

        public void GetSearchResponse(Response Response)
        {
            Response.AppendInt32(HabboId);
            Response.AppendStringWithBreak(Username);
            Response.AppendStringWithBreak(Motto);
            Response.AppendBoolean(InRoom);
            Response.AppendBoolean(IsAlive);
            Response.AppendChar(2);
            Response.AppendBoolean(false);
            Response.AppendStringWithBreak(Look);
            Response.AppendStringWithBreak(LastVisit);
            Response.AppendChar(2);
        }

        public Client GetClient()
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId) != null)
            {
                return BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId).GetClient();
            }

            return null;
        }
    }
}
