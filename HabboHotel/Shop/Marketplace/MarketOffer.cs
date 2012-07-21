using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Furni.Items;

namespace BrickEmulator.HabboHotel.Shop.Marketplace
{
    class MarketOffer
    {
        #region Fields
        public readonly int Id;
        public readonly int UserId;
        public readonly int BaseId;
        public int State;
        public readonly string BaseName;
        public readonly int CreditsRequest;
        public readonly int CreditsRequestTot;
        public readonly DateTime DateTime;
        #endregion

        #region Properties
        public Boolean Expired
        {
            get
            {
                return MinutesLeft <= 0;
            }
        }

        public int MinutesLeft
        {
            get
            {
                int Amount = (2880 - BrickEngine.GetConvertor().ObjectToInt32((DateTime.Now - DateTime).TotalMinutes));

                if (Amount < 0)
                {
                    return 0;
                }

                return Amount;
            }
        }

        public int TradedToday
        {
            get
            {
                return BrickEngine.GetMarketplaceReactor().GetDevelopmentDay(0, GetBaseItem().SpriteId).Count;
            }
        }

        public int TotOfferCount
        {
            get
            {
                return BrickEngine.GetMarketplaceReactor().GetOffersForSpriteId(GetBaseItem().SpriteId).Count;
            }
        }
        #endregion

        #region Constructors
        public MarketOffer(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            State = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseName = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            CreditsRequest = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CreditsRequestTot = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            DateTime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
        }

        public MarketOffer(Dictionary<int,Object> Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            UserId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            State = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            BaseName = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            CreditsRequest = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CreditsRequestTot = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            DateTime = BrickEngine.GetConvertor().ObjectToDateTime(Row[Counter.Next]);
        }
        #endregion

        #region Methods
        public void GetResponse(Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendInt32(State);
            Response.AppendInt32(GetBaseItem().InternalType.ToLower().Equals("s") ? 1 : 2);
            Response.AppendInt32(GetBaseItem().SpriteId);
            Response.AppendChar(2);
            Response.AppendInt32(CreditsRequestTot);
            Response.AppendInt32(GetBaseItem().SpriteId);
            Response.AppendInt32(BrickEngine.GetMarketplaceReactor().GetCreditsRequestAverageForSpriteId(GetBaseItem().SpriteId));
            Response.AppendInt32(TotOfferCount);
        }

        public void GetOwnResponse(Response Response)
        {
            int ItemState = State;

            if (Expired)
            {
                ItemState = 3;
            }

            Response.AppendInt32(Id);
            Response.AppendInt32(ItemState);
            Response.AppendInt32(GetBaseItem().InternalType.ToLower().Equals("s") ? 1 : 2);
            Response.AppendInt32(GetBaseItem().SpriteId);
            Response.AppendChar(2);
            Response.AppendInt32(CreditsRequestTot);
            Response.AppendInt32(MinutesLeft);
            Response.AppendInt32(GetBaseItem().SpriteId);
        }

        public Response GetPurchaseResponse()
        {
            Response Response = new Response(67);
            Response.AppendInt32(BaseId);
            Response.AppendStringWithBreak(GetBaseItem().InternalName);
            Response.AppendInt32(0);
            Response.AppendInt32(0);
            Response.AppendInt32(0);
            Response.AppendInt32(1);
            Response.AppendStringWithBreak(GetBaseItem().InternalType.ToLower());
            Response.AppendInt32(GetBaseItem().SpriteId);
            Response.AppendChar(2);
            Response.AppendInt32(1);
            Response.AppendInt32(-1);
            Response.AppendInt32(0);
            Response.AppendChar(2);
            return Response;
        }

        public BaseItem GetBaseItem()
        {
            return BrickEngine.GetFurniReactor().GetItem(BaseId);
        }
        #endregion 
    }
}
