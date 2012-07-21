using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Users;

namespace BrickEmulator.HabboHotel.Pets
{
    class PetCommandHandler
    {
        private Dictionary<string, Interaction> Interactions;

        private delegate void Interaction(Client Commander, PetInfo Pet);

        public PetCommandHandler()
        {
            Register();
        }

        public void Interact(Client Commander, PetInfo Pet, string Command)
        {
            if (Interactions.ContainsKey(Command.ToLower()))
            {
                Interactions[Command.ToLower()].Invoke(Commander, Pet);
            }
        }

        public void Register()
        {
            Interactions = new Dictionary<string, Interaction>();

            Interactions["free"] = new Interaction(Free);
            Interactions["sit"] = new Interaction(Sit);
            Interactions["down"] = new Interaction(Down);
            Interactions["here"] = new Interaction(Here);
            Interactions["beg"] = new Interaction(Beg);
            Interactions["play dead"] = new Interaction(PlayDead);
            Interactions["stay"] = new Interaction(Stay);
            Interactions["follow"] = new Interaction(Follow);
            Interactions["stand"] = new Interaction(Stand);
            Interactions["jump"] = new Interaction(Stand);
            Interactions["speak"] = new Interaction(Speak);
            Interactions["play"] = new Interaction(Play);
            Interactions["silent"] = new Interaction(Silent);
        }

        private void Free(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 5 && Pet.Level >= 0)
            {
                Pet.Energy -= 5;

                Pet.GiveExpirience(10);

                Pet.DoActions(PetInterAction.Walking, Pet.GetRoomUser());

                Pet.UpdateEnergy();
            }
        }

        private void Sit(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 7 && Pet.Level >= 1)
            {
                Pet.Energy -= 7; 

                Pet.GiveExpirience(20);

                Pet.GetRoomUser().AddStatus("sit", string.Empty);
                Pet.GetRoomUser().UpdateStatus(true);

                Pet.UpdateEnergy();
            }
        }

        private void Down(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 9 && Pet.Level >= 2)
            {
                Pet.Energy -= 9;

                Pet.GiveExpirience(30);

                Pet.GetRoomUser().AddStatus("lay", string.Empty);
                Pet.GetRoomUser().UpdateStatus(true);

                Pet.UpdateEnergy();
            }
        }

        private void Here(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 12 && Pet.Level >= 3)
            {
                Pet.Energy -= 12;

                Pet.GiveExpirience(40);

                Pet.GetRoomUser().UnhandledGoalPoint = Client.GetUser().GetRoomUser().FontPoint;

                Pet.UpdateEnergy();
            }
        }

        private void Beg(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 12 && Pet.Level >= 4)
            {
                Pet.Energy -= 12;

                Pet.GiveExpirience(45);

                Pet.GetRoomUser().AddStatus("beg", string.Empty);
                Pet.GetRoomUser().UpdateStatus(true);

                Pet.UpdateEnergy();
            }
        }

        private void PlayDead(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 12 && Pet.Level >= 5)
            {
                Pet.Energy -= 12;

                Pet.GiveExpirience(45);

                Pet.GetRoomUser().AddStatus("ded", string.Empty);
                Pet.GetRoomUser().UpdateStatus(true);

                Pet.UpdateEnergy();
            }
        }

        private void Stay(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 10 && Pet.Level >= 6)
            {
                Pet.Energy -= 10;

                Pet.GiveExpirience(30);

                Pet.UpdateEnergy();
            }
        }

        private void Follow(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 12 && Pet.Level >= 7)
            {
                Pet.Energy -= 12;

                Pet.GiveExpirience(50);

                Pet.GetRoomUser().UnhandledGoalPoint = Client.GetUser().GetRoomUser().FontBehind;

                Pet.UpdateEnergy();
            }
        }

        private void Stand(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 15 && Pet.Level >= 8)
            {
                Pet.Energy -= 15;

                Pet.GiveExpirience(80);

                Pet.GetRoomUser().AddStatus("std", string.Empty);
                Pet.GetRoomUser().UpdateStatus(true);

                Pet.UpdateEnergy();
            }
        }

        private void Jump(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 15 && Pet.Level >= 9)
            {
                Pet.Energy -= 15;

                Pet.GiveExpirience(120);

                Pet.GetRoomUser().AddStatus("jmp", string.Empty);
                Pet.GetRoomUser().UpdateStatus(true);

                Pet.UpdateEnergy();
            }
        }

        private void Speak(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 10 && Pet.Level >= 10)
            {
                Pet.Energy -= 10;

                Pet.GiveExpirience(150);

                Pet.GetRoomUser().Talk("Waaaaw , gelawaaa!", Rooms.Virtual.Units.SpeechType.Shout, 0, string.Empty);

                Pet.UpdateEnergy();
            }
        }

        private void Play(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 15 && Pet.Level >= 11)
            {
                Pet.Energy -= 15;

                Pet.GiveExpirience(200);

                if (Pet.GetRoom().GetRoomEngine().GetPetToys().Count > 0)
                {
                    Pet.GetRoomUser().AddStatus("gst", "plf");
                    Pet.GetRoomUser().UpdateStatus(true);

                    Pet.GetRoomUser().UnhandledGoalPoint = Pet.GetRoom().GetRoomEngine().GetPetToys()[Pet.Random.Next(0, Pet.GetRoom().GetRoomEngine().GetPetToys().Count - 1)].Point;
                    Pet.DoActions(PetInterAction.Progressing, Pet.GetRoomUser());
                }
                else
                {
                    Pet.DoActions(PetInterAction.Playing, Pet.GetRoomUser());
                    Pet.GetRoomUser().UpdateStatus(true);
                }

                Pet.UpdateEnergy();
            }
        }

        private void Silent(Client Client, PetInfo Pet)
        {
            if (Pet.Sleeping)
            {
                return;
            }

            if (Pet.Energy >= 10 && Pet.Level >= 12)
            {
                Pet.Energy -= 10;

                Pet.GiveExpirience(100);

                Pet.MutedDateTime = DateTime.Now;

                Pet.UpdateEnergy();
            }
        }
    }
}
