using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;

namespace BrickEmulator.HabboHotel.Users.Handlers.Effects
{
    class Effect
    {
        #region Fields

        public readonly int Id;
        public readonly int UserId;
        public readonly int EffectId;
        public readonly int EffectLength;

        public DateTime Activated = new DateTime(1, 1, 1);

        #endregion

        #region Properties

        public Boolean IsActivated
        {
            get
            {
                return Activated.Year > 1;
            }
        }

        public int RemainingTime
        {
            get
            {
                if (!IsActivated)
                {
                    return -1;
                }

                if (BrickEngine.GetConvertor().ObjectToInt32(EffectLength - (DateTime.Now - Activated).TotalSeconds) <= 0)
                {
                    return 0;
                }

                return BrickEngine.GetConvertor().ObjectToInt32(EffectLength - (DateTime.Now - Activated).TotalSeconds);
            }
        }

        #endregion

        #region Constructors

        public Effect(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            EffectId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            EffectLength = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            DateTime.TryParse(BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]), out Activated);
        }

        public Effect(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            EffectId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            EffectLength = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            DateTime.TryParse(BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]), out Activated);
        }

        #endregion

        #region Methods

        public void Excecute()
        {
            Activated = DateTime.Now;
        }

        public void GetResponse(Response Response)
        {
            Response.AppendInt32(EffectId);
            Response.AppendInt32(EffectLength);
            Response.AppendBoolean(!IsActivated);
            Response.AppendInt32(RemainingTime);
        }

        #endregion
    }
}
