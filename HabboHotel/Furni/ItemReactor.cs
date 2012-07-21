using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.Storage;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;

namespace BrickEmulator.HabboHotel.Furni
{
    class ItemReactor
    {
        private SecurityCounter ItemIdCounter;

        private Dictionary<int, Item> Items;
        private Dictionary<KeyValuePair<int, int>, int> ItemUpdates;

        public ItemReactor() { }

        public void Prepare()
        {
            LoadItems();
            LoadItemUpdates();

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT MAX(id) FROM items LIMIT 1");
                ItemIdCounter = new Security.SecurityCounter(Reactor.GetInt32());
            }
        }

        public void LoadItems()
        {
            Items = new Dictionary<int, Item>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM items");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                Item Item = new Item(Row);

                Items.Add(Item.Id, Item);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Items.Count + "] Item(s) cached.", IO.WriteType.Outgoing);
        }

        public void LoadItemUpdates()
        {
            ItemUpdates = new Dictionary<KeyValuePair<int,int>,int>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM new_item_updates");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                int UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[1]);
                int TabId = BrickEngine.GetConvertor().ObjectToInt32(Row[2]);
                int ItemId = BrickEngine.GetConvertor().ObjectToInt32(Row[3]);

                KeyValuePair<int, int> kvp = new KeyValuePair<int,int>(ItemId, TabId);

                ItemUpdates.Add(kvp, UserId);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + ItemUpdates.Count + "] ItemUpdate(s) cached.", IO.WriteType.Outgoing);
        }

        public Item GetItem(int Id)
        {
            try { return Items[Id]; }
            catch { return null; }
        }

        public Response RemoveItem(int Id)
        {
            Items.Remove(Id);

            Response Response = new Response(99);
            Response.AppendInt32(Id);

            return Response;
        }

        public List<Item> GetItemsForUser(int HabboId)
        {
            var List = new List<Item>();

            foreach (Item Item in Items.Values.ToList())
            {
                if (Item.RoomId > 0)
                {
                    continue;
                }

                if (Item.OwnerId == HabboId)
                {
                    List.Add(Item);
                }
            }

            return List;
        }

        public List<Item> GetItemsForRoom(int RoomId)
        {
            var List = new List<Item>();

            foreach (Item Item in Items.Values.ToList())
            {
                if (Item.RoomId <= 0)
                {
                    continue;
                }

                if (Item.RoomId == RoomId)
                {
                    List.Add(Item);
                }
            }

            return List;
        }

        public List<int> GetUnseenItems(int HabboId, int TabId)
        {
            var List = new List<int>();

            foreach (KeyValuePair<KeyValuePair<int, int>, int> kvp in ItemUpdates)
            {
                if (kvp.Key.Value == TabId)
                {
                    if (kvp.Value == HabboId)
                    {
                        List.Add(kvp.Key.Key);
                    }
                }
            }

            return List;
        }

        public void HandleUnseenItems(int HabboId, int TabId)
        {
            // Clear cache
            foreach (KeyValuePair<KeyValuePair<int, int>, int> kvp in ItemUpdates)
            {
                if (kvp.Value == HabboId)
                {
                    if (kvp.Key.Value == TabId)
                    {
                        ItemUpdates.Remove(kvp.Key);
                    }
                }
            }

            // Clean Database
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("DELETE FROM new_item_updates WHERE user_id = @habboid AND tab_id = @tabid");
                Reactor.AddParam("habboid", HabboId);
                Reactor.AddParam("tabid", TabId);
                Reactor.ExcuteQuery();
            }
        }

        // Recoded From Uber
        public List<iPoint> GetAffectedPoints(iPoint Point, int Length, int Width, int Rotation)
        {
            var List = new List<iPoint>();

            if (Length > 1)
            {
                if (Rotation == 0 || Rotation == 4)
                {
                    for (int i = 1; i < Length; i++)
                    {
                        List.Add(new iPoint(Point.X, Point.Y + i));

                        for (int j = 1; j < Width; j++)
                        {
                            List.Add(new iPoint(Point.X + j, Point.Y + i));
                        }
                    }
                }
                else if (Rotation == 2 || Rotation == 6)
                {
                    for (int i = 1; i < Length; i++)
                    {
                        List.Add(new iPoint(Point.X + i, Point.Y));

                        for (int j = 1; j < Width; j++)
                        {
                            List.Add(new iPoint(Point.X + i, Point.Y + j));
                        }
                    }
                }
            }

            if (Width > 1)
            {
                if (Rotation == 0 || Rotation == 4)
                {
                    for (int i = 1; i < Width; i++)
                    {
                        List.Add(new iPoint(Point.X + i, Point.Y));

                        for (int j = 1; j < Length; j++)
                        {
                            List.Add(new iPoint(Point.X + i, Point.Y + j));
                        }
                    }
                }
                else if (Rotation == 2 || Rotation == 6)
                {
                    for (int i = 1; i < Width; i++)
                    {
                        List.Add(new iPoint(Point.X, Point.Y + i));

                        for (int j = 1; j < Length; j++)
                        {
                            List.Add(new iPoint(Point.X + j, Point.Y + i));
                        }
                    }
                }
            }

            return List;
        }

        public void AddNewUpdate(int Id, int TabId, int HabboId)
        {
            ItemUpdates.Add(new KeyValuePair<int, int>(Id, TabId), HabboId);

            // Doing SeenItem queru
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO new_item_updates (user_id, tab_id, item_id) VALUES (@habboid, @tabid, @itemid)");
                Reactor.AddParam("habboid", HabboId);
                Reactor.AddParam("tabid", TabId);
                Reactor.AddParam("itemid", Id);
                Reactor.ExcuteQuery();
            }
        }

        public int InsertItem(int HabboId, int BaseId, string ExtraData, int InsideBaseId)
        {
            SecurityCounter Counter = new SecurityCounter(-1);

            Dictionary<int, Object> Row = new Dictionary<int,object>();

            if (string.IsNullOrEmpty(ExtraData))
            {
                ExtraData = "0";
            }

            Row[Counter.Next] = ItemIdCounter.Next;
            Row[Counter.Next] = BaseId;
            Row[Counter.Next] = HabboId;
            Row[Counter.Next] = 0;
            Row[Counter.Next] = -1;
            Row[Counter.Next] = -1;
            Row[Counter.Next] = 0.0;
            Row[Counter.Next] = 0;
            Row[Counter.Next] = string.Empty;
            Row[Counter.Next] = ExtraData;
            Row[Counter.Next] = InsideBaseId;

            Item Item = new Item(Row);

            Items.Add(Item.Id, Item);

            // Doing Item query
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO items (user_id, base_id, extra_data, inside_base_id) VALUES (@habboid, @baseid, @extradata, @inside)");
                Reactor.AddParam("habboid", HabboId);
                Reactor.AddParam("baseid", BaseId);
                Reactor.AddParam("extradata", ExtraData);
                Reactor.AddParam("inside", InsideBaseId);
                Reactor.ExcuteQuery();
            }

            int TabId = Item.GetBaseItem().InternalType.ToLower().Equals("s") ? 1 : 2;

            AddNewUpdate(Item.Id, TabId, HabboId);

            return Item.Id;
        }
    }
}
