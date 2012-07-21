using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrickEmulator.HabboHotel.Rooms.Pathfinding
{
    struct iPoint
    {
        public int X;
        public int Y;
        public double Z;

        public iPoint(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
            this.Z = 0.0;
        }

        public iPoint(int X, int Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Boolean Compare(iPoint obj)
        {
            return (obj.X == X && obj.Y == Y);
        }

        public int Calculate()
        {
            return (X * Y);
        }

        public override string ToString()
        {
            return X + ":" + Y + ":" + Z;
        }
    }
}
