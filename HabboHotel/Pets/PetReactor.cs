using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using System.Data;
using BrickEmulator.Storage;
using System.Text.RegularExpressions;

namespace BrickEmulator.HabboHotel.Pets
{
    class PetReactor
    {
        public int[] ExpirienceLevels = { 0, 100, 200, 500, 750, 1000, 2000, 2500, 2750, 3000, 3500, 5000, 5500, 6500, 7000, 9000, 12000, 15000, 16000, 20000, 25000 };

        public const int MAX_PETS_PER_ROOM = 3;

        public const int MAX_HAPPINESS = 100;
        public const int MAX_EXPIRIENCE = 100;

        public static int MAX_ENERGY(int Level)
        {
            return (120 + ((Level - 1) * 20));
        }

        public const int MAX_LEVEL = 20;

        private Dictionary<int, PetInfo> Pets;
        private Dictionary<int, PetSpeech> Speeches;
        private Dictionary<int, PetAction> Actions;

        public readonly PetCommandHandler CommandHandler = new PetCommandHandler();

        private SecurityCounter PetIdCounter;

        public PetReactor()
        {
            LoadPets();
            LoadPetSpeeches();
            LoadPetActions();
        }

        public void LoadPetActions()
        {
            Actions = new Dictionary<int, PetAction>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM pets_random_actions");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                PetAction Action = new PetAction(Row);

                Actions.Add(Action.Id, Action);
            }

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] PetActions(s) cached.", Actions.Count), IO.WriteType.Outgoing);
        }

        public void LoadPetSpeeches()
        {
            Speeches = new Dictionary<int, PetSpeech>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM pets_random_speech");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                PetSpeech Speech = new PetSpeech(Row);

                Speeches.Add(Speech.Id, Speech);
            }

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] PetSpeech(s) cached.", Speeches.Count), IO.WriteType.Outgoing);
        }

        public void LoadPets()
        {
            Pets = new Dictionary<int, PetInfo>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT MAX(id) FROM user_pets LIMIT 1");

                int MaxId = Reactor.GetInt32();

                if (MaxId <= 0)
                {
                    MaxId += 1000000;
                }

                PetIdCounter = new SecurityCounter(MaxId);
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM user_pets");
                Table = Reactor.GetTable();
            }

            foreach (DataRow Row in Table.Rows)
            {
                PetInfo Pet = new PetInfo(Row);

                Pets.Add(Pet.Id, Pet);
            }

            BrickEngine.GetScreenWriter().ScretchLine(string.Format("[{0}] Pet(s) cached.", Pets.Count),IO.WriteType.Outgoing);
        }

        public List<PetAction> GetActionsForType(int Type)
        {
            var List = new List<PetAction>();

            foreach (PetAction Action in Actions.Values)
            {
                if (Action.PetType == Type)
                {
                    List.Add(Action);
                }
            }

            return List;
        }

        public List<PetSpeech> GetSpeechesForType(int Type)
        {
            var List = new List<PetSpeech>();

            foreach (PetSpeech Speech in Speeches.Values)
            {
                if (Speech.PetType == Type)
                {
                    List.Add(Speech);
                }
            }

            return List;
        }

        public List<PetInfo> GetPetsForRoom(int RoomId)
        {
            var List = new List<PetInfo>();

            foreach (PetInfo Info in Pets.Values)
            {
                if (Info.RoomId <= 0)
                {
                    continue;
                }

                if (Info.RoomId == RoomId)
                {
                    List.Add(Info);
                }
            }

            return List;
        }

        public List<PetInfo> GetPetsForInventory(int HabboId)
        {
            var List = new List<PetInfo>();

            foreach (PetInfo Info in Pets.Values)
            {
                if (Info.RoomId > 0)
                {
                    continue;
                }

                if (Info.UserId == HabboId)
                {
                    List.Add(Info);
                }
            }

            return List;
        }

        public void RemovePet(int Id)
        {
            Pets.Remove(Id);
        }

        public int GeneratePet(int UserId, int Type, int Race, string Color, string Name)
        {
            Dictionary<int, Object> Row = new Dictionary<int, object>();

            Row[0] = PetIdCounter.Next;
            Row[1] = UserId;
            Row[2] = -1;
            Row[3] = Name;
            Row[4] = Type;
            Row[5] = Race;
            Row[6] = Color;
            Row[7] = 100;
            Row[8] = 0;
            Row[9] = 120;
            Row[10] = 0;
            Row[11] = DateTime.Now;
            Row[12] = -1;
            Row[13] = -1;
            Row[14] = 0;

            PetInfo Pet = new PetInfo(Row);

            Pets.Add(Pet.Id, Pet);

            BrickEngine.GetItemReactor().AddNewUpdate(Pet.Id, 3, UserId);

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO user_pets (id, user_id, name, type, race, color, created) VALUES (@id, @user_id, @name, @type, @race, @color, @created)");
                Reactor.AddParam("id", Pet.Id);
                Reactor.AddParam("user_id", UserId);
                Reactor.AddParam("name", Name);
                Reactor.AddParam("type", Type);
                Reactor.AddParam("race", Race);
                Reactor.AddParam("color", Color);
                Reactor.AddParam("created", Pet.Created);
                Reactor.ExcuteQuery();
            }

            return Pet.Id;
        }

        public int NameCheckResult(string Name)
        {
            // 0 = Valid
            // 1 = Name to Long
            // 2 = Name to Short
            // 3 = Forbidden Characters
            // 4 = Forbidden words

            if (Name.Length < 2)
            {
                return 2;
            }
            else if (Name.Length > 16)
            {
                return 1;
            }
            else if (!Regex.IsMatch(Name, @"^[a-zA-Z0-9]+$"))
            {
                return 3;
            }

            return 0;
        }

        public PetInfo GetPetInfo(int Id)
        {
            try { return Pets[Id]; }
            catch { return null; }
        }
    }
}
