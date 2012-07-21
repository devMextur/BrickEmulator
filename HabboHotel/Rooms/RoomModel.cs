using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BrickEmulator.HabboHotel.Rooms.Pathfinding;
using BrickEmulator.Security;

namespace BrickEmulator.HabboHotel.Rooms
{
    class RoomModel
    {
        public readonly string Id;
        public readonly string Map;
        public readonly iPoint Door;

        public readonly int DoorRot;

        public readonly int LimitUsers;

        public TileState[,] DefaultTiles;
        public double[,] DefaultHeightMap;

        public List<string> Lines = new List<string>();

        public int XLength
        {
            get
            {
                return Lines[0].Length;
            }
        }

        public int YLength
        {
            get
            {
                return Lines.Count;
            }
        }

        public RoomModel(DataRow Row)
        {
            Security.SecurityCounter Counter = new Security.SecurityCounter(-1);

            Id = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
            Map = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);

            Door = new iPoint(-1, -1);

            Door.X = BrickEngine.GetConvertor().ObjectToShort(Row[Counter.Next]) - 1;
            Door.Y = BrickEngine.GetConvertor().ObjectToShort(Row[Counter.Next]);
            Door.Z = BrickEngine.GetConvertor().ObjectToDouble(Row[Counter.Next]);
            DoorRot = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            LimitUsers = BrickEngine.GetConvertor().ObjectToInt32(Row[Counter.Next]);

            GenerateLines();
        }

        private void GenerateLines()
        {
            // Add Lines to List
            Map.Replace(Convert.ToChar(10).ToString(), "").Split(Convert.ToChar(13)).ToList().ForEach(Lines.Add);

            try
            {
                GetPremairParams();
                GetSecondairParams();
            }
            catch (Exception e)
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, IO.PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("[" + Id + "] Room Model error " + e.Message, IO.WriteType.Outgoing);
            }
        }

        public string GetPremairParams()
        {
            StringBuilder Builder = new StringBuilder();

            for (short y = 0; y < YLength; y++)
            {
                string Line = Lines[y];

                Builder.AppendLine(Line);
            }

            return Builder.ToString().Replace(Convert.ToChar(10).ToString(), "");
        }

        public string GetSecondairParams()
        {
            DefaultTiles = new TileState[XLength, YLength];
            DefaultHeightMap = new double[XLength, YLength];

            StringBuilder Builder = new StringBuilder();

            for (short y = 0; y < YLength; y++)
            {
                string FixedLine = string.Empty;

                for (short x = 0; x < XLength; x++)
                {
                    string Character = Lines[y][x].ToString().Trim().ToLower();

                    double HeightMapChar = 0.0;

                    double.TryParse(Character, out HeightMapChar);

                    DefaultHeightMap[x, y] = HeightMapChar;

                    if (x == Door.X && y == Door.Y)
                    {
                        DefaultTiles[x, y] = TileState.Walkable_laststep;

                        DefaultHeightMap[x, y] = Door.Z;

                        FixedLine += Door.Z;
                    }
                    else
                    {
                        if (Character == "x")
                        {
                            DefaultTiles[x, y] = TileState.Blocked;
                        }
                        else
                        {
                            DefaultTiles[x, y] = TileState.Walkable;
                        }

                        FixedLine += Character;
                    }
                }

                Builder.AppendLine(FixedLine);
            }

            return Builder.ToString().Replace(Convert.ToChar(10).ToString(),"");
        }
    }
}
