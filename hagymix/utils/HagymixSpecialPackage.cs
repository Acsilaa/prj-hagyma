using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            return flat.Count(x => x.isTreasure);
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
            // ?
            // pld: string poz = "4:12"; 
            return unavailables;
        }
        /// <summary>
        /// Labiritust generál a kapott pozíciókat tartalmazó lista alapján. A lista elemei egymáshoz kapcsolódó járatok pozíciói.
        /// </summary>
        /// <param name="positionsList">"sor_index:oszlop_index" formátumban az egymáshoz kapcsolódó járatok pozícióit tartalmazó lista </param>
        /// <returns>A létrehozott labirintus térképe</returns>
        static char[,] GenerateLabyrinth(List<string> positionsList)
        {
            return null;
        }
    }
}
