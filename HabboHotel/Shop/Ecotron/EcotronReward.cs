using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Shop.Ecotron
{
    class EcotronReward
    {
        #region Fields
        public readonly int Id;
        public readonly int BaseId;
        public readonly int Level;
        #endregion

        #region Constructors
        public EcotronReward(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Level = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
        #endregion

        #region Methods
        public BaseItem GetBaseItem()
        {
            return BrickEngine.GetFurniReactor().GetItem(BaseId);
        }
        #endregion
    }
}
