using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace BrickEmulator.HabboHotel.Pets
{
    class PetSpeech
    {
        #region Fields
        public readonly int Id;
        public readonly int PetType;
        public readonly string Speech;
        public readonly Boolean Shout;
        #endregion

        #region Constructors
        public PetSpeech(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            PetType = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Speech = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Shout = BrickEngine.GetConvertor().ObjectToBoolean(Row[Counter.Next]);
        }
        #endregion
    }
}
