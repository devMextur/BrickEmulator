using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Missions.Quests.Items
{
    class Quest
    {
        public readonly int Id;
        public readonly int CategoryId;
        public readonly string Action;
        public readonly int ActionAmount;
        public readonly int PixelReward;
        public readonly int OrderId;

        private string FilterAction
        {
            get
            {
                return (Action.ToUpper().StartsWith("FIND")) ? "FIND_STUFF" : Action;
            }
        }

        private string FilterReAction
        {
            get
            {
                return Action.Replace("_USERS_", "S").Replace("_ITEM_", string.Empty).Replace("_", string.Empty);
            }
        }

        private string FilterParam
        {
            get
            {
                return (OrderId == 1 && CategoryId >= 2 && CategoryId <= 4) ? "_2" : string.Empty;
            }
        }

        public Quest(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            CategoryId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Action = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            ActionAmount = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            PixelReward = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }

        public void GetResponse(Client Client, Response Response)
        {
            Response.AppendInt32(Id);
            Response.AppendBoolean((Client.GetUser().CurrentQuest.Equals(Id)) ? true : false);
            Response.AppendStringWithBreak(FilterAction);
            Response.AppendStringWithBreak(FilterParam);
            Response.AppendInt32(PixelReward);
            Response.AppendStringWithBreak(FilterReAction);
            Response.AppendInt32((Client.GetUser().CurrentQuest.Equals(Id)) ? Client.GetUser().CurrentQuestProgress : 0);
            Response.AppendInt32(ActionAmount);
        }
    }
}
