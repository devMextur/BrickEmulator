using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.Security;
using System.Data;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Rooms
{
    class RoomIcon
    {
        public readonly int RoomId;
        public int BackgroundId;
        public int ForegroundId;

        public Dictionary<int, int> Items = new Dictionary<int,int>();

        public RoomIcon(int RoomId, int BackgroundId, int ForegroundId, string ItemsRaw)
        {
            this.RoomId = RoomId;
            this.BackgroundId = BackgroundId;
            this.ForegroundId = ForegroundId;

            if (ItemsRaw.Length > 0)
            {
                foreach (string Bit in ItemsRaw.ToString().Split(','))
                {
                    if (string.IsNullOrEmpty(Bit))
                    {
                        continue;
                    }

                    string[] tBit = Bit.Split('.');

                    int a = 0; // SlotId
                    int b = 0; // IconId

                    int.TryParse(tBit[0], out a);

                    if (tBit.Length > 1)
                    {
                        int.TryParse(tBit[1], out b);
                    }

                    Items.Add(a, b);
                }
            }
        }

        public void GetResponse(Response Response)
        {
            Response.AppendInt32(BackgroundId);
            Response.AppendInt32(ForegroundId);
            Response.AppendInt32(Items.Count);

            foreach (KeyValuePair<int, int> kvp in Items)
            {
                Response.AppendInt32(kvp.Key);
                Response.AppendInt32(kvp.Value);
            }
        }
    }
}
