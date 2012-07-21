using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.Storage;
using System.Data;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Rooms.Virtual.Units.Chatting
{
    class WordFilterHandler
    {
        #region Fields
        private List<string> ForcedParams;
        #endregion

        #region Constructors
        public WordFilterHandler()
        {
            LoadParams();
        }
        #endregion

        #region Methods
        public void LoadParams()
        {
            ForcedParams = new List<string>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT param FROM wordfilter_items ORDER BY id ASC");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                string Param = BrickEngine.GetConvertor().ObjectToString(Row[0]);

                if(string.IsNullOrEmpty(Param))
                {
                    continue;
                }

                if (!ForcedParams.Contains(Param))
                {
                    ForcedParams.Add(Param);
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] Forced Word(s) cached.", ForcedParams.Count), IO.WriteType.Outgoing);
        }

        public string FilterMessage(Client Receiver, string RawMessage)
        {
            string Result = RawMessage;

            if (Receiver.GetUser().EnableWordfilter)
            {
                foreach (string ForcedParam in ForcedParams)
                {
                    if (Result.ToLower().Contains(ForcedParam.ToLower()))
                    {
                        Result = Result.Replace(ForcedParam, GenerateStars(ForcedParam));
                    }
                }
            }

            return Result;
        }

        private string GenerateStars(string Param)
        {
            string Builder = string.Empty;

            for (int i = 0; i < Param.Length; i++)
            {
                Builder += '*';
            }

            return Builder;
        }
        #endregion
    }
}
