/*############################
 # BrickEmulator Pathfinder #
 # By Breakz0ne 13-8-2011   #
 ############################*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BrickEmulator.Security;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace BrickEmulator.HabboHotel.Rooms.Pathfinding
{
    struct PathfinderNode
    {
        #region Fields
        public readonly double Distance;
        public readonly iPoint Point;
        public readonly Boolean Diagonal;
        #endregion

        #region Constructors
        public PathfinderNode(double Distance, iPoint Point, Boolean Diagonal)
        {
            this.Distance = Distance;
            this.Point = Point;
            this.Diagonal = Diagonal;
        }  
        #endregion
    }

    struct Pathfinder : IDisposable
    {
        #region Fields
        internal readonly int RoomId;

        internal readonly TileState[,] Tiles;
        internal readonly double[,] HeightMap;
        internal readonly RoomModel Model;

        internal readonly ReaderWriterLock Lock;

        internal Boolean FoundPath;
        #endregion

        #region Constructors
        public Pathfinder(int RoomId, TileState[,] Tiles, double[,] HeightMap, RoomModel Model)
        {
            this.RoomId = RoomId;
            this.Tiles = Tiles;
            this.HeightMap = HeightMap;
            this.Model = Model;

            this.Lock = new ReaderWriterLock();
            this.FoundPath = false;
        }
        #endregion

        #region Methods

        public List<iPoint> GeneratePath(iPoint Start, iPoint End)
        {
            lock (Lock)
            {
                var List = new List<iPoint>();

                if (Start.Compare(End))
                {
                    return List;
                }

                iPoint WorkingTile = Start;
                iPoint OldTile = new iPoint(-1,-1);

                short[,] AroundTiles = new short[8, 2] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 }, { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } };

                if (End.X >= Model.XLength || End.Y >= Model.YLength || Tiles[End.X, End.Y] == TileState.Blocked)
                {
                    return List;
                }

                do
                {
                    var TilesAround = new List<PathfinderNode>();

                    for (short i = 0; i < AroundTiles.GetLength(0); i++)
                    {
                        int X = (WorkingTile.X + AroundTiles[i, 0]);
                        int Y = (WorkingTile.Y + AroundTiles[i, 1]);

                        Boolean Diagonal = (i > 3);

                        if (X < 0 || Y < 0 || X >= Model.XLength || Y >= Model.YLength)
                        {
                            continue;
                        }

                        double Height = HeightMap[X, Y];

                        iPoint NexTile = new iPoint(X, Y, Height);

                        if (!GetRoom().GetRoomEngine().CanWalk(NexTile))
                        {
                            continue;
                        }

                        var State = Tiles[NexTile.X, NexTile.Y];

                        if (State == TileState.Walkable || (NexTile.Compare(End) && State == TileState.Walkable_laststep))
                        {
                            TilesAround.Add(new PathfinderNode(TileDistance(NexTile, End), NexTile, Diagonal));
                        }
                    }

                    if (TilesAround.Count > 0)
                    {
                        var Sorted = (from node in TilesAround orderby node.Distance ascending select node).ToList();

                        if (!List.Contains(Sorted[0].Point))
                        {
                            WorkingTile = Sorted[0].Point;

                            List.Add(Sorted[0].Point);
                        }
                        else
                        {
                            FoundPath = true;
                        }

                        if (End.Compare(WorkingTile))
                        {
                            FoundPath = true;
                        }
                    }
                    else
                    {
                        FoundPath = true;
                    }
                }
                while (!FoundPath);

                return List;
            }
        }

        public void Dispose()
        {
            BrickEngine.GetProgressReactor().GetCollector().Finialize(this);
        }

        private double GetDistance(iPoint point1, iPoint point2)
        {
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }

        private int TileDistance(iPoint Start, iPoint End)
        {
            return Math.Abs(Start.X - End.X) + Math.Abs(Start.Y - End.Y);
        }

        public BrickEmulator.HabboHotel.Rooms.Virtual.VirtualRoom GetRoom()
        {
            return BrickEngine.GetRoomReactor().GetVirtualRoom(RoomId, RoomRunningState.Alive);
        }

        #endregion
    }
}
