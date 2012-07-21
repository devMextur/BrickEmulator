using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Security;
using BrickEmulator.Storage;
using BrickEmulator.HabboHotel.Rooms.Navigator.Items.Featured;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Rooms.Virtual;
using BrickEmulator.HabboHotel.Rooms.Navigator.Items;

namespace BrickEmulator.HabboHotel.Rooms.Navigator
{
    class NavigatorReactor
    {
        private Dictionary<int, FeacturedItem> FeacturedItems;
        private Dictionary<int, PrivateCategory> PrivateCategorys;

        private List<int> StaffPickedRooms = new List<int>();

        public NavigatorReactor()
        {
            LoadFeacturedRooms();
            LoadPrivateCategorys();
        }

        public void LoadFeacturedRooms()
        {
            FeacturedItems = new Dictionary<int, FeacturedItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM featured_rooms ORDER BY order_id ASC");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                FeacturedItem FeacturedItem = new FeacturedItem(Row);

                FeacturedItems.Add(FeacturedItem.Id, FeacturedItem);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + FeacturedItems.Count + "] FeacturedItem(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadPrivateCategorys()
        {
            PrivateCategorys = new Dictionary<int, PrivateCategory>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM private_rooms_categorys");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    PrivateCategory PrivateCategory = new PrivateCategory(Row);

                    PrivateCategorys.Add(PrivateCategory.Id, PrivateCategory);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + PrivateCategorys.Count + "] PrivateCategory(s) cached.", IO.WriteType.Outgoing);
        }

        public PrivateCategory GetPrivateCategory(int Id)
        {
            try { return PrivateCategorys[Id]; }
            catch { return null; }
        }

        public Response GetPrivateResponse()
        {
            Response Response = new Response(221);
            Response.AppendInt32(PrivateCategorys.Count);

            var Sorted = (from cat in PrivateCategorys.Values.ToList() orderby cat.Id ascending select cat).ToList();

            foreach (PrivateCategory Category in Sorted)
            {
                if (Category.Id > 0)
                {
                    Response.AppendBoolean(true);
                }

                Response.AppendInt32(Category.Id);
                Response.AppendStringWithBreak(Category.Name);
            }

            Response.AppendChar(2);

            return Response;
        }

        public List<FeacturedItem> GetRoomsForCategory(int Category)
        {
            var List = new List<FeacturedItem>();

            foreach (FeacturedItem FeacturedItem in FeacturedItems.Values.ToList())
            {
                if (FeacturedItem.CategoryId == Category)
                {
                    List.Add(FeacturedItem);
                }
            }

            return (from cat in List orderby cat.OrderId ascending select cat).ToList();
        }

        public Response GetFeaturedResponse()
        {
            try
            {
                Response Response = new Response(450);
                Response.AppendInt32(FeacturedItems.Count);

                foreach (FeacturedItem FeacturedItem in GetRoomsForCategory(-1))
                {
                    FeacturedItem.GetResponse(Response);

                    foreach (FeacturedItem Room in GetRoomsForCategory(FeacturedItem.Id))
                    {
                        Room.GetResponse(Response);
                    }
                }

                return Response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public Response GetResponseForUser(Client Client)
        {
            Response Response = new Response(451);
            Response.AppendInt32(4);
            Response.AppendChar(2);

            var List = BrickEngine.GetRoomReactor().GetMe(Client.GetUser().HabboId).ToList();

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, false);
            }

            return Response;
        }

        public Response GetRoomsResponse(int Category)
        {
            Response Response = new Response(451);
            Response.AppendBoolean(true);
            Response.AppendRawInt32(Category);
            Response.AppendChar(2);

            var List = BrickEngine.GetRoomReactor().GetVirtualRooms(Category);

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, false);
            }

            return Response;
        }

        public Response GetEventRoomsResponse(int Category)
        {
            Response Response = new Response(451);
            Response.AppendInt32(12);
            Response.AppendRawInt32(Category);
            Response.AppendChar(2);

            var List = BrickEngine.GetRoomReactor().GetEventRooms(Category);

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, true);
            }

            return Response;
        }

        public Response GetHighestScore()
        {
            Response Response = new Response(451);
            Response.AppendInt32(2);
            Response.AppendChar(2);

            var List = BrickEngine.GetRoomReactor().GetHighestScore();

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, false);
            }

            return Response;
        }

        public Response GetPopulairTags()
        {
            Response Response = new Response(452);

            var Tags = BrickEngine.GetRoomReactor().GetPopulairTags();

            Response.AppendInt32(Tags.Count);

            foreach (string Tag in Tags)
            {
                Response.AppendStringWithBreak(Tag);
                Response.AppendInt32(-1);
            }

            return Response;
        }

        public Response GetFavoriteRooms(Client Client)
        {
            Response Response = new Response(451);
            Response.AppendInt32(6);
            Response.AppendChar(2);

            var List = BrickEngine.GetRoomReactor().GetFavoriteRooms(Client);

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, false);
            }

            return Response;
        }

        public Response GetLastVisited(Client Client)
        {
            Response Response = new Response(451);
            Response.AppendInt32(7);
            Response.AppendChar(2);

            var List = BrickEngine.GetRoomReactor().GetLastVisited(Client);

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, false);
            }

            return Response;
        }

        public Response GetSearchResponse(string Query)
        {
            Response Response = new Response(451);
            Response.AppendInt32(8);
            Response.AppendStringWithBreak(Query);

            var List = BrickEngine.GetRoomReactor().GetSearchRooms(Query);

            Response.AppendInt32(List.Count);

            foreach (VirtualRoom Room in List)
            {
                Room.GetNavigatorResponse(Response, false);
            }

            return Response;
        }
    }
}
