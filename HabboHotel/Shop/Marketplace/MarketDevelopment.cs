using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Shop.Marketplace
{
    class MarketDevelopment
    {
        #region Fields
        public readonly int Id;
        public readonly int BaseId;
        public readonly DateTime DateTime;
        public readonly int CreditsRequest;
        #endregion

        #region Constructors
        public MarketDevelopment(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            DateTime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
            CreditsRequest = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public MarketDevelopment(Dictionary<int, Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            DateTime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
            CreditsRequest = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
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
