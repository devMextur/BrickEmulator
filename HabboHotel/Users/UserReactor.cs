using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.Storage;
using BrickEmulator.Network;

namespace BrickEmulator.HabboHotel.Users
{
    class UserReactor
    {
        #region Fields
        public DateTime LastRespectUpdate;

        private readonly Random Random = new Random();
        private readonly char[] Charakters = new char[] { 'a','b','c','d','e','f' } ;

        public UserReactor() { }
        #endregion

        #region Properties
        public Boolean NeedsRespectUpdate
        {
            get
            {
                return (DateTime.Now - LastRespectUpdate).TotalDays >= 1;
            }
        }
        #endregion

        #region Methods

        public void LoadSettings()
        {
            LastRespectUpdate = DateTime.Now;

            string Result = string.Empty;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET status = '0'"); // , sso_hash = ''
                Reactor.ExcuteQuery();
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT last_respect_update FROM server_settings LIMIT 1");
                Result = Reactor.GetString();
            }

            if (string.IsNullOrEmpty(Result))
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("INSERT INTO server_settings (last_respect_update) VALUES (@item)");
                    Reactor.AddParam("item", LastRespectUpdate);
                    Reactor.ExcuteQuery();
                }

                UpdateRespect(false);
            }
            else
            {
                this.LastRespectUpdate = BrickEngine.GetConvertor().ObjectToDateTime(Result);
            }
        }

        public void UpdateRespect(bool Update)
        {
            this.LastRespectUpdate = DateTime.Now;

            foreach (SocketClient Client in BrickEngine.GetSocketShield().Sessions)
            {
                if (Client.GetClient() != null)
                {
                    if (Client.GetClient().IsValidUser)
                    {
                        Client.GetClient().GetUser().RespectLeft = 3;
                        Client.GetClient().GetUser().RespectLeftPets = 3;
                    }
                }
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("UPDATE users SET respect_left = '3', respect_left_pets = '3'");
                Reactor.ExcuteQuery();
            }

            if (Update)
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("UPDATE server_settings SET last_respect_update = @item LIMIT 1");
                    Reactor.AddParam("item", LastRespectUpdate);
                    Reactor.ExcuteQuery();
                }
            }
        }

        public UserCache HandleTicket(string Ticket)
        {
            if (Ticket.Length > 200)
            {
                return null;
            }

            DataRow Row = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM users WHERE sso_hash = @ticket LIMIT 1");
                Reactor.AddParam("ticket", Ticket);
                Row = Reactor.GetRow();
            }

            if (Row == null)
            {
                return null;
            }

            if (AlreadyLoggedIn(Convert.ToUInt32(Row[0])))
            {
                return null;
            }

            return new UserCache(Row);
        }

        public Boolean AlreadyLoggedIn(uint HabboId)
        {
            for (int i = 0; i < BrickEngine.GetSocketShield().Sessions.Count; i++)
            {
                var SocketClient = BrickEngine.GetSocketShield().Sessions[i];

                if (SocketClient.GetClient().IsValidUser)
                {
                    if (SocketClient.GetClient().GetUser().HabboId == HabboId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string GenerateHash()
        {
            StringBuilder Builder = new StringBuilder();

            for (short i = 0; i < 10; i++)
            {
                Builder.Append(Random.Next(0, 9));
            }

            Builder.Append(Charakters[Random.Next(0, Charakters.Length)]);

            for (short i = 0; i < 5; i++)
            {
                Builder.Append(Random.Next(0, 9));
            }

            Builder.Append(Charakters[Random.Next(0, Charakters.Length)]);

            for (short i = 0; i < 2; i++)
            {
                Builder.Append(Random.Next(0, 9));
            }

            for (short i = 0; i < 5; i++)
            {
                Builder.Append(Charakters[Random.Next(0, Charakters.Length)]);
            }

            for (short i = 0; i < 3; i++)
            {
                Builder.Append(Random.Next(0, 9));
            }

            return Builder.ToString();
        }

        public Boolean IsOnline(int HabboId)
        {
            return BrickEngine.GetSocketShield().GetSocketClientByHabboId(HabboId) != null;
        }

        public string GetUsername(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().Username;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT username FROM users WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetString();
                }
            }

            return string.Empty;
        }

        public int GetId(string Username)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboName(Username) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboName(Username).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboName(Username).GetClient().GetUser().HabboId;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT id FROM users WHERE username = @name LIMIT 1");
                    Reactor.AddParam("name", Username);
                    return Reactor.GetInt32();
                }
            }

            return 0;
        }

        public string GetLook(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().Look;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT look FROM users WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetString();
                }
            }

            return string.Empty;
        }

        public string GetLastVisit(int Id)
        {
            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT last_alive FROM users WHERE id = @id LIMIT 1");
                Reactor.AddParam("id", Id);
                return Reactor.GetString();
            }
        }

        public string GetMotto(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().Motto;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT motto FROM users WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetString();
                }
            }

            return string.Empty;
        }

        public string GetGender(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().Gender;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT gender FROM users WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetString();
                }
            }

            return string.Empty;
        }

        public Boolean GetEnableNewFriends(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().EnableNewFriends;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT enable_new_friends FROM users WHERE id = @id LIMIT 1");
                    Reactor.AddParam("id", Id);
                    return Reactor.GetInt32() == 1;
                }
            }

            return false;
        }

        #endregion

        #region ModTools
        public int GetWarnings(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().Warnings;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT warnings FROM users WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("habboid", Id);
                    return Reactor.GetInt32();
                }
            }

            return 0;
        }

        public string GetRegistered(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().RegisteredDatetime.ToString();
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT registered_datetime FROM users WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("habboid", Id);
                    return Reactor.GetString();
                }
            }

            return DateTime.Now.ToString();
        }

        public int GetUserRank(int Id)
        {
            if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id) != null)
            {
                if (BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().IsValidUser)
                {
                    return BrickEngine.GetSocketShield().GetSocketClientByHabboId(Id).GetClient().GetUser().Rank;
                }
            }
            else
            {
                using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
                {
                    Reactor.SetQuery("SELECT rank FROM users WHERE id = @habboid LIMIT 1");
                    Reactor.AddParam("habboid", Id);
                    return Reactor.GetInt32();
                }
            }

            return 1;
        }
        #endregion
    }
}
