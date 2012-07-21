using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users
{
    partial class PacketHandler
    {
        private void HandleSessionDetails(Client Client, Request Request)
        {
            Response Response = new Response(257);
            Response.AppendInt32(9);
            Response.AppendBoolean(false);
            Response.AppendBoolean(false);
            Response.AppendBoolean(true);
            Response.AppendBoolean(true);
            Response.AppendInt32(3);
            Response.AppendBoolean(false);
            Response.AppendInt32(2);
            Response.AppendBoolean(false);
            Response.AppendInt32(4);
            Response.AppendBoolean(true);
            Response.AppendInt32(5);
            Response.AppendStringWithBreak("dd-MM-yyyy");
            Response.AppendInt32(7);
            Response.AppendBoolean(false);
            Response.AppendInt32(8);
            Response.AppendStringWithBreak("http://");
            Response.AppendInt32(9);
            Response.AppendBoolean(false);
            Client.SendResponse(Response);
        }

        private void HandleTicket(Client Client, Request Request)
        {
            string Ticket = Request.PopFixedString();

            Client.ParseUser(BrickEngine.GetUserReactor().HandleTicket(Ticket));

            if (Client.IsValidUser)
            {
                if (BrickEngine.GetToolReactor().IsBanned(Client.GetUser().HabboId, Client.IPAddress))
                {
                    Client.Dispose();
                    return;
                }

                Response Userhash = new Response(439);
                Userhash.AppendStringWithBreak(Client.GetUser().Hash);
                Client.SendResponse(Userhash);

                Client.SendResponse(new Response(3));

                if (BrickEngine.GetConfigureFile().CallBooleanKey("welcomemessage.enabled"))
                {
                    Client.LongNotif(string.Empty);
                }

                Client.SendResponse(Client.GetUser().GetLoginResponse());

                Client.Authenticated = true;

                Client.SendResponse(BrickEngine.GetToolReactor().GetResponse(Client));
            }
            else
            {
                Client.Notif("Invalid Ticket, try again!", false);
                Client.Dispose();
            }
        }
    }
}
