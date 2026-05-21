namespace hagymix.lib
{
    internal class Room
    {
        /*
         *           0
         *      4   ROOM   2
         *           3
         */
        public bool[] ways = new bool[4];

        public string? roomChar; // ha null, akkor üres
        public bool isEntrance = false;
        public bool isTreasure = false;

        public static int GetMazeWidth(Room?[,] maze)
        {
            return maze.GetLength(1);
        }
        public static int GetMazeLength(Room?[,] maze)
        {
            return maze.GetLength(0);
        }

        public Room(char roomChar)
        {
            this.roomChar = roomChar.ToString();

            switch(roomChar) // ezt még majd sanitize-olni kell a szomszédok alapján, mert a karakter nem mutatja, hogy le van-e zárva egy irányban
            {
                case '║':
                    this.ways = new bool[4] { true, false, true, false };
                    break;
                case '═':
                    this.ways = new bool[4] { false, true, false, true };
                    break;
                case '╝':
                    this.ways = new bool[4] { true, false, false, true };
                    break;
                case '╚':
                    this.ways = new bool[4] { true, true, false, false };
                    break;
                case '╦':
                    this.ways = new bool[4] { false, true, true, true };
                    break;
                case '╗':
                    this.ways = new bool[4] { false, false, true, true };
                    break;
                case '╩':
                    this.ways = new bool[4] { true, true, false, true };
                    break;
                case '╠':
                    this.ways = new bool[4] { true, true, true, false };
                    break;
                case '╬':
                    this.ways = new bool[4] { true, true, true, true };
                    break;
                case '╣':
                    this.ways = new bool[4] { true, false, true, true };
                    break;
                case '╔':
                    this.ways = new bool[4] { false, true, true, false };
                    break;
                case '█':
                    this.isTreasure = true;
                    this.ways = new bool[4] { true, true, true, true };
                    break;
                case '.':
                    this.ways = new bool[4] { false, false, false, false };
                    break;
            }
        }


        public static char GetCharFromWays(bool[] ways)
        {
            if (ways == null || ways.Length != 4) return '.';

            bool u = ways[0], r = ways[1], d = ways[2], l = ways[3];

            if (u && r && d && l) return '╬';
            if (u && !r && d && !l) return '║';
            if (!u && r && !d && l) return '═';
            if (u && !r && !d && l) return '╝';
            if (u && r && !d && !l) return '╚';
            if (!u && r && d && l) return '╦';
            if (!u && !r && d && l) return '╗';
            if (u && r && !d && l) return '╩';
            if (u && r && d && !l) return '╠';
            if (u && !r && d && l) return '╣';
            if (!u && r && d && !l) return '╔';
            if (!u && !r && !d && !l) return '.';


            return '.';
        }
    }
}
