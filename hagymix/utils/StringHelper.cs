using hagymix.lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Shapes;

namespace hagymix.utils
{
    internal class StringHelper
    {
        public static char[,] FileToChar2D(string filename) {
            string[] lines = File.ReadAllLines(filename);
            char[,] processed = new char[lines.Length, lines[0].Length];

            for(int i = 0; i < lines.Length; i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    processed[i, j] = lines[i][j];
                }
            }
            return processed;
        }
        public static char[,] FileToChar2D(Stream stream)
        {
            string[] lines = new StreamReader(stream).ReadToEnd().Split(Environment.NewLine);
            char[,] processed = new char[lines.Length, lines[0].Length];

            for (int i = 0; i < lines.Length; i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    processed[i, j] = lines[i][j];
                }
            }
            return processed;
        }

        public static Room?[,] Char2DToRoomMap(char[,] charmaze) {
            int rows = charmaze.GetLength(0);
            int cols = charmaze.GetLength(1);
            Room?[,] processed = new Room?[rows, cols];

            for(int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (charmaze[i, j] != ' ' && charmaze[i, j] != '.')
                    {
                        processed[i, j] = new Room(charmaze[i, j]);
                    }
                }
            }

            FixMazeFromRooms(processed);

            return processed;
        }

        private static void FixMazeFromRooms(Room?[,] rawmaze)
        {
            int rows = rawmaze.GetLength(0);
            int cols = rawmaze.GetLength(1);

            // Pass 1: Disconnect any way that points into a null cell
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var room = rawmaze[i, j];
                    if (room == null) continue;

                    if (i == 0) room.ways[0] = room.ways[0]; // edge, keep as-is (entrance candidate)
                    else if (rawmaze[i - 1, j] == null) room.ways[0] = false;

                    if (j == cols - 1) room.ways[1] = room.ways[1];
                    else if (rawmaze[i, j + 1] == null) room.ways[1] = false;

                    if (i == rows - 1) room.ways[2] = room.ways[2];
                    else if (rawmaze[i + 1, j] == null) room.ways[2] = false;

                    if (j == 0) room.ways[3] = room.ways[3];
                    else if (rawmaze[i, j - 1] == null) room.ways[3] = false;
                }
            }

            // Pass 2: Enforce mutual agreement — both sides must point at each other,
            // otherwise neither gets the connection (except edge-facing ways which are entrances)
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var room = rawmaze[i, j];
                    if (room == null) continue;

                    // North (ways[0]): must be an edge OR neighbor also points south (ways[2])
                    if (i > 0 && rawmaze[i - 1, j] != null)
                    {
                        bool mutual = room.ways[0] && rawmaze[i - 1, j].ways[2];
                        room.ways[0] = mutual;
                        rawmaze[i - 1, j].ways[2] = mutual;
                    }

                    // East (ways[1]): must be an edge OR neighbor also points west (ways[3])
                    if (j < cols - 1 && rawmaze[i, j + 1] != null)
                    {
                        bool mutual = room.ways[1] && rawmaze[i, j + 1].ways[3];
                        room.ways[1] = mutual;
                        rawmaze[i, j + 1].ways[3] = mutual;
                    }

                    // South (ways[2]): must be an edge OR neighbor also points north (ways[0])
                    if (i < rows - 1 && rawmaze[i + 1, j] != null)
                    {
                        bool mutual = room.ways[2] && rawmaze[i + 1, j].ways[0];
                        room.ways[2] = mutual;
                        rawmaze[i + 1, j].ways[0] = mutual;
                    }

                    // West (ways[3]): must be an edge OR neighbor also points east (ways[1])
                    if (j > 0 && rawmaze[i, j - 1] != null)
                    {
                        bool mutual = room.ways[3] && rawmaze[i, j - 1].ways[1];
                        room.ways[3] = mutual;
                        rawmaze[i, j - 1].ways[1] = mutual;
                    }
                }
            }

            // Pass 3: Mark entrances — edge rooms whose way points outward (into the border)
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var room = rawmaze[i, j];
                    if (room == null) continue;

                    if (i == 0 && room.ways[0]) room.isEntrance = true;
                    if (j == cols - 1 && room.ways[1]) room.isEntrance = true;
                    if (i == rows - 1 && room.ways[2]) room.isEntrance = true;
                    if (j == 0 && room.ways[3]) room.isEntrance = true;
                }
            }

            // Pass 4: Fix treasure rooms — their ways are purely derived from neighbors
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var room = rawmaze[i, j];
                    if (room == null || room.isTreasure != Treasure.Contains) continue;

                    room.ways[0] = (i > 0 && rawmaze[i - 1, j] != null && rawmaze[i - 1, j].ways[2]);
                    room.ways[1] = (j < cols - 1 && rawmaze[i, j + 1] != null && rawmaze[i, j + 1].ways[3]);
                    room.ways[2] = (i < rows - 1 && rawmaze[i + 1, j] != null && rawmaze[i + 1, j].ways[0]);
                    room.ways[3] = (j > 0 && rawmaze[i, j - 1] != null && rawmaze[i, j - 1].ways[1]);
                }
            }
        }

        public static Room[] ToFlatRoomsArray(Room?[,] maze)
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);
            var list = new List<Room>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (maze[i, j] != null) list.Add(maze[i, j]!);
                }
            }
            return list.ToArray();
        }
    }
}
