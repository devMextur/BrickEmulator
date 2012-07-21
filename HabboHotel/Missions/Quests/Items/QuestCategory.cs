using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Missions.Quests.Items
{
    class QuestCategory
    {
        #region Fields
        public readonly int Id;
        public readonly string Name;
        public readonly int OrderId;
        #endregion

        #region Constructors
        public QuestCategory(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
            Name = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            OrderId = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);
        }
        #endregion

        #region Methods
        public void GetResponse(Client Client, Response Response)
        {
            var AchievedQuests = Client.GetUser().Quests;
            var CategoryQuests = BrickEngine.GetQuestReactor().OrderQuestsForCategory(Id);

            Response.AppendStringWithBreak(Name.ToLower());

            if (!AchievedQuests.ContainsKey(Id))
            {
                Response.AppendInt32(0);
            }
            else
            {
                Response.AppendInt32(AchievedQuests[Id]);
            }

            Response.AppendInt32(CategoryQuests.Count);

            Response.AppendInt32(0); // Pixels Type - Reward

            if (AchievedCategory(Client) || BrickEngine.GetQuestReactor().GetQuestForCategory(Client, Id) == null)
            {
                Response.AppendInt32(0);
                Response.AppendBoolean(false);
                Response.AppendChar(2);
                Response.AppendChar(2);
                Response.AppendInt32(0);
                Response.AppendChar(2);
                Response.AppendInt32(0);
                Response.AppendInt32(0);
            }
            else
            {
                BrickEngine.GetQuestReactor().GetQuestForCategory(Client, Id).GetResponse(Client, Response);
            }

            Response.AppendBoolean(false);
        }

        public Boolean AchievedCategory(Client Client)
        {
            var AchievedQuests = Client.GetUser().Quests;
            var CategoryQuests = BrickEngine.GetQuestReactor().OrderQuestsForCategory(Id);

            if (AchievedQuests.ContainsKey(Id))
            {
                return AchievedQuests[Id] >= CategoryQuests.Count;
            }

            return false;
        }
        #endregion
    }
}
