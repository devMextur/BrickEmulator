using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Pets
{
    class PetAction
    {
        #region Fields
        public readonly int Id;
        public readonly int PetType;
        public readonly string Key;
        public readonly string Value;
        #endregion

        #region Constructors
        public PetAction(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            PetType = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Key = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Value = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }
        #endregion
    }
}
