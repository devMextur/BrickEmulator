using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Clothes
{
    class Clothe
    {
        public readonly int HabboId;
        public int SlotId;
        public string Look;
        public string Gender;

        public Clothe(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            HabboId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            SlotId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Look = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Gender = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }

        public Clothe(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            HabboId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            SlotId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Look = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Gender = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
        }

        public void GetResponse(Response Response)
        {
            Response.AppendInt32(SlotId);
            Response.AppendStringWithBreak(Look);
            Response.AppendStringWithBreak(Gender.ToUpper());
        }
    }
}
