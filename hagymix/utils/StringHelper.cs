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

        public static Room?[,] Char2DToRoomMap(char[,] charmaze) {
            int rows = charmaze.GetLength(0);
            int cols = charmaze.GetLength(1);
            Room?[,] processed = new Room?[rows, cols];

            for(int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (charmaze[i, j] != ' ')
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

            for (int i = 0; i < rows; i++)
            {
                for(int j = 0;j < cols; j++)
                {
                    if (rawmaze[i, j] != null)
                    {
                        if (i > 0 && rawmaze[i - 1, j] != null && rawmaze[i - 1, j].ways[2]) rawmaze[i, j]?.ways[0] = true;
                        if (j < cols - 1 && rawmaze[i, j + 1] != null && rawmaze[i, j + 1].ways[3]) rawmaze[i, j]?.ways[1] = true;
                        if (i < rows - 1 && rawmaze[i + 1, j] != null && rawmaze[i + 1, j].ways[0]) rawmaze[i, j]?.ways[2] = true;
                        if (j > 0 && rawmaze[i, j - 1] != null && rawmaze[i, j - 1].ways[1]) rawmaze[i, j]?.ways[3] = true;

                        // mark edge rooms as entrances if their ways point outward
                        if (i == 0 && rawmaze[i, j]?.ways[0] == true) rawmaze[i, j]?.isEntrance = true;
                        if (j == cols - 1 && rawmaze[i, j]?.ways[1] == true) rawmaze[i, j]?.isEntrance = true;
                        if (i == rows - 1 && rawmaze[i, j]?.ways[2] == true) rawmaze[i, j]?.isEntrance = true;
                        if (j == 0 && rawmaze[i, j]?.ways[3] == true) rawmaze[i, j]?.isEntrance = true;
                    }
                }
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var room = rawmaze[i, j];
                    if (room != null && room.isTreasure)
                    {
                        room.ways[0] = (i > 0 && rawmaze[i - 1, j] != null && rawmaze[i - 1, j].ways[2]);
                        room.ways[1] = (j < cols - 1 && rawmaze[i, j + 1] != null && rawmaze[i, j + 1].ways[3]);
                        room.ways[2] = (i < rows - 1 && rawmaze[i + 1, j] != null && rawmaze[i + 1, j].ways[0]);
                        room.ways[3] = (j > 0 && rawmaze[i, j - 1] != null && rawmaze[i, j - 1].ways[1]);
                    }
                }
            }
        }

        public static Room?[] ToFlatRoomsArray(Room?[,] maze)
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);
            Room?[] flat = new Room?[rows * cols];
            int idx = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    flat[idx++] = maze[i, j];
                }
            }
            return flat;
        }
    }
}
