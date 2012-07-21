using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Furni.Items;
using BrickEmulator.Storage;

namespace BrickEmulator.HabboHotel.Furni
{
    class FurniReactor
    {
        #region Fields
        private Dictionary<int, BaseItem> BaseItems;
        #endregion

        public FurniReactor() { }

        public void Prepare()
        {
            BaseItems = new Dictionary<int, BaseItem>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM furnidata");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                BaseItem Item = new BaseItem(Row);

                BaseItems.Add(Item.Id, Item);
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + BaseItems.Count + "] BaseItem(s) cached.", IO.WriteType.Outgoing);
        }

        public BaseItem GetSpecifiqueItem(string BaseName)
        {
            foreach (BaseItem Item in BaseItems.Values.ToList())
            {
                if (Item.InternalName.ToLower() == BaseName.ToLower())
                {
                    return Item;
                }
            }

            return null;
        }

        public BaseItem GetItem(int Id)
        {
            try { return BaseItems[Id]; }
            catch { return null; }
        }
    }
}
