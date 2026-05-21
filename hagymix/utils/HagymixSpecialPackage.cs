using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using hagymix.lib;

namespace hagymix.utils
{
    internal class HagymixSpecialPackage
    {
        /// <summary>
        /// Megadja, hogy hány termet tartamaz a térkép
        /// </summary>
        /// <param name="map">Labirintus mátrixa</param>
        /// <returns>Termek száma</returns>
        static int GetRoomNumber(char[,] map)
        {
            var maze = StringHelper.Char2DToRoomMap(map);
            var flat = StringHelper.ToFlatRoomsArray(maze);
            return flat.Count(x => x.isTreasure == Treasure.Contains || x.isTreasure == Treasure.Collected);
        }
        /// <summary>
        /// A kapott térkép széleit végignézve megállapítja, hogy hány kijárat van.
        /// </summary>
        /// <param name="map">Labirintus mátrixa</param>
        /// <returns>Az alkalmas kijáratok száma</returns>
        static int GetSuitableEntrance(char[,] map)
        {
            var maze = StringHelper.Char2DToRoomMap(map);
            var flat = StringHelper.ToFlatRoomsArray(maze);
            return flat.Count(x => x.isEntrance);
        }
        /// <summary>
        /// Megnézi, hogy van-e a térképen meg nem engedett karakter?
        /// </summary>
        /// <param name="map">Labirintus mátrixa</param>
        /// <returns>true - A térkép tartalmaz szabálytalan karaktert, false - nincs benne ilyen</returns>
        static bool IsInvalidElement(char[,] map)
        {
            var validChars = new HashSet<char>() { '║','═','╝','╚','╦','╗','╩','╠','╬','╣','╔','█','.',' ' };
            int rows = map.GetLength(0);
            int cols = map.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (!validChars.Contains(map[i, j])) return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Visszaadja azoknak a járatkaraktereknek a pozícióját, amelyekhez egyetlen szomszéd pozícióból sem lehet eljutni.
        /// </summary>
        /// <param name="map">Labirintus mátrixa</param>
        /// <returns>A pozíciók "sor_index:oszlop_index" formátumban szerepelnek a lista elemeiként
        static List<string> GetUnavailableElements(char[,] map)
        {
            List<string> unavailables = new List<string>();
            var maze = StringHelper.Char2DToRoomMap(map);
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var room = maze[i, j];
                    if (room == null) continue;
                    if (room.ways.All(w => w == false))
                    {
                        unavailables.Add($"{i}:{j}");
                    }
                }
            }

            return unavailables;
        }
        /// <summary>
        /// Labiritust generál a kapott pozíciókat tartalmazó lista alapján. A lista elemei egymáshoz kapcsolódó járatok pozíciói.
        /// </summary>
        /// <param name="positionsList">"sor_index:oszlop_index" formátumban az egymáshoz kapcsolódó járatok pozícióit tartalmazó lista </param>
        /// <returns>A létrehozott labirintus térképe</returns>
        static char[,] GenerateLabyrinth(List<string> positionsList)
        {
            if (positionsList == null || positionsList.Count == 0)
            {
                return new char[0, 0];
            }

            var coords = positionsList
                .Select(p => p.Split(':'))
                .Where(parts => parts.Length == 2)
                .Select(parts => new { r = int.Parse(parts[0].Trim()), c = int.Parse(parts[1].Trim()) })
                .ToList();

            if (coords.Count == 0) return new char[0, 0];

            int minR = coords.Min(x => x.r);
            int maxR = coords.Max(x => x.r);
            int minC = coords.Min(x => x.c);
            int maxC = coords.Max(x => x.c);

            int rows = maxR - minR + 1;
            int cols = maxC - minC + 1;

            char[,] map = new char[rows, cols];
            // fill with dots
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    map[i, j] = '.';

            // mark input positions with a placeholder 'C'
            foreach (var p in coords)
            {
                int rr = p.r - minR;
                int cc = p.c - minC;
                if (rr >= 0 && rr < rows && cc >= 0 && cc < cols)
                    map[rr, cc] = 'C';
            }

            // replace each 'C' with appropriate char based on neighbor occupancy
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (map[i, j] != 'C') continue;

                    bool[] ways = new bool[4];
                    // up
                    ways[0] = (i > 0 && map[i - 1, j] != '.');
                    // right
                    ways[1] = (j < cols - 1 && map[i, j + 1] != '.');
                    // down
                    ways[2] = (i < rows - 1 && map[i + 1, j] != '.');
                    // left
                    ways[3] = (j > 0 && map[i, j - 1] != '.');

                    map[i, j] = hagymix.lib.Room.GetCharFromWays(ways);
                }
            }

            return map;
        }
    }
}
