using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Missions.Quests.Items;
using System.Data;
using BrickEmulator.Storage;
using BrickEmulator.Messages;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Missions.Quests
{
    class QuestReactor
    {
        private Dictionary<int, QuestCategory> QuestCategorys;
        private Dictionary<int, Quest> Quests;

        public List<QuestCategory> OrderCategorys
        {
            get
            {
                var List = new List<QuestCategory>();

                foreach (QuestCategory Category in QuestCategorys.Values)
                {
                    if (OrderQuestsForCategory(Category.Id).Count > 0)
                    {
                        List.Add(Category);
                    }
                }

                return (from cat in List orderby cat.OrderId ascending select cat).ToList();
            }
        }

        public QuestReactor()
        {
            LoadQuestCategorys();
            LoadQuests();
        }

        public void LoadQuestCategorys()
        {
            QuestCategorys = new Dictionary<int, QuestCategory>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM quests_categorys ORDER by order_id ASC");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                QuestCategory Category = new QuestCategory(Row);

                QuestCategorys.Add(Category.Id, Category);
            }

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] QuestCategory(s) cached.", QuestCategorys.Count), IO.WriteType.Outgoing);
        }

        public void LoadQuests()
        {
            Quests = new Dictionary<int, Quest>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM quests_items ORDER by order_id ASC");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                Quest Quest = new Quest(Row);

                Quests.Add(Quest.Id, Quest);
            }

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] Quest(s) cached.", Quests.Count), IO.WriteType.Outgoing);
        }

        public QuestCategory GetQuestCategory(int Id)
        {
            try { return QuestCategorys[Id]; }
            catch { return null; }
        }

        public Quest GetQuest(int Id)
        {
            try { return Quests[Id]; }
            catch { return null; }
        }

        public List<Quest> OrderQuestsForCategory(int Id)
        {
            var List = new List<Quest>();

            foreach (Quest Quest in Quests.Values)
            {
                if (Quest.CategoryId == Id)
                {
                    List.Add(Quest);
                }
            }

            return (from quest in List orderby quest.OrderId ascending select quest).ToList();
        }

        public Quest GetQuestForCategory(Client Client, int CategoryId)
        {
            var AchievedQuests = Client.GetUser().Quests;

            if (!AchievedQuests.ContainsKey(CategoryId))
            {
                return OrderQuestsForCategory(CategoryId)[0];
            }
            else
            {
                int LastAchievedLevel = AchievedQuests[CategoryId];

                if (LastAchievedLevel + 1 >= OrderQuestsForCategory(CategoryId).Count)
                {
                    return null;
                }

                return OrderQuestsForCategory(CategoryId)[LastAchievedLevel];
            }
        }

        public Response GetResponse(Client Client)
        {
            try
            {
                Response Response = new Response(800);
                Response.AppendInt32(OrderCategorys.Count);

                foreach (QuestCategory Category in OrderCategorys)
                {
                    Category.GetResponse(Client, Response);
                }

                Response.AppendBoolean(true);

                return Response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null; }
        }
    }
}
