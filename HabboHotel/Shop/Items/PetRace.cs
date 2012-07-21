using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Shop.Items
{
    class PetRace
    {
        public readonly int Id;
        public readonly int Type;
        public readonly int RaceAmount;
        public readonly int StartIndexer;

        public PetRace(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Type = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            RaceAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            StartIndexer = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
    }
}
