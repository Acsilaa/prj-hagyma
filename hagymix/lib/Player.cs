using System;
using System.Collections.Generic;
using System.Text;

namespace hagymix.lib
{
    enum Direction
    {
        Up = 0, Right = 1, Down = 2, Left = 3,
    }
    internal class Player
    {
        int x = 0;
        int y = 0;
        bool isStarted = false;
        Room[,] map;
        int treasureCount = 0;

        public int X { get => x; }
        public int Y { get => y; }
        public bool IsStarted { get => isStarted; }

        public Player(Room[,] map) { 
            this.map = map;
            ChangeEntrance();
        }
        public void ChangeEntrance()
        {
            if (this.isStarted) return;
            for (int i = this.y; i < this.map.GetLength(0); i++) {
                for (int z = this.x + 1; z < this.map.GetLength(1); z++)
                {
                    if (this.map[i, z].isEntrance)
                    {
                        this.y = i;
                        this.x = z;
                        return;
                    }
                }
                this.x = 0;
            }
            for (int i = 0; i < this.map.GetLength(0); i++)
            {
                for (int z = 0; z < this.map.GetLength(1); z++)
                {
                    if (this.map[i, z].isEntrance)
                    {
                        this.y = i;
                        this.x = z;
                        return;
                    }
                }
            }
        }
        public void SetEntrance()
        {
            this.isStarted = true;
            return;
        }
        void moved()
        {
            //check treasure
            if (this.map[this.y, this.x].isTreasure)
            {
                this.treasureCount++;
                this.map[this.y, this.x].isTreasure = false;
            }
        }
        public bool Move(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    if(this.map[this.y, this.x].ways[0])
                    {
                        this.y = this.y - 1;
                        moved();
                        return true;
                    }
                    break;
                case Direction.Right:
                    if (this.map[this.y, this.x].ways[1])
                    {
                        this.x = this.x + 1;
                        moved();
                        return true;
                    }
                    break;
                case Direction.Down:
                    if (this.map[this.y, this.x].ways[2])
                    {
                        this.y = this.y + 1;
                        moved();
                        return true;
                    }
                    break;
                case Direction.Left:
                    if (this.map[this.y, this.x].ways[3])
                    {
                        this.x = this.x - 1;
                        moved();
                        return true;
                    }
                    break;
                default:
                    return false;
            }
            return false;
        }
    }
}
