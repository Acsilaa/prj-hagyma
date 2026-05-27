using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace hagymix.lib
{
    enum Direction
    {
        Up = 0, Right = 1, Down = 2, Left = 3,
    }

    enum MoveResult
    {
        Blocked = 0,
        Moved = 1,
        TreasureFound = 2,
        Exited = 3
    }

    internal class Player
    {
        int x = 0;
        int y = 0;
        bool isOnMap = false;
        bool isStarted = false;
        Room?[,] map;
        int treasureCount = 0;
        int totalTreasureCount = 0;

        public int X { get => x; }
        public int Y { get => y; }
        public bool IsStarted { get => isStarted; }
        public bool IsOnMap { get => isOnMap; }
        public int TreasureCount { get => treasureCount; }
        public int TotalTreasureCount { get => totalTreasureCount; }
        public bool AllTreasuresCollected { get => treasureCount >= totalTreasureCount; }

        public Player(Room?[,] map) { 
            this.map = map;
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    if (map[i, j] != null && map[i, j].isTreasure == Treasure.Contains)
                    {
                        totalTreasureCount++;
                    }
                }
            }
            ChangeEntrance();
        }
        public void ChangeEntrance()
        {
            if (this.isStarted) return;
            this.isOnMap = true;
            for (int i = this.y; i < this.map.GetLength(0); i++) {
                for (int z = this.x + 1; z < this.map.GetLength(1); z++)
                {
                    if (this.map[i, z] != null && this.map[i, z].isEntrance)
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
                    if (this.map[i, z] != null && this.map[i, z].isEntrance)
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
            this.isOnMap = true;
            return;
        }
        private void moved()
        {
            //check treasure
            if (this.map[this.y, this.x] != null && this.map[this.y, this.x].isTreasure == Treasure.Contains)
            {
                this.treasureCount++;
                this.map[this.y, this.x].isTreasure = Treasure.Collected;
            }
        }

        public bool CanExit(Direction direction)
        {
            if (!this.isOnMap) return false;
            if (this.map[this.y, this.x] == null) return false;
            if (!this.map[this.y, this.x].ways[(int)direction]) return false;

            return (direction == Direction.Up && this.y == 0)
                || (direction == Direction.Right && this.x == this.map.GetLength(1) - 1)
                || (direction == Direction.Down && this.y == this.map.GetLength(0) - 1)
                || (direction == Direction.Left && this.x == 0);
        }

        public MoveResult Move(Direction direction)
        {
            if (!this.isOnMap) return MoveResult.Blocked;

            if (CanExit(direction))
            {
                this.isOnMap = false;
                this.isStarted = false;
                return MoveResult.Exited;
            }

            switch (direction)
            {
                case Direction.Up:
                    if(this.map[this.y, this.x] != null && this.map[this.y, this.x].ways[0])
                    {
                        this.y = this.y - 1;
                        bool wasTreasure = this.map[this.y, this.x] != null && this.map[this.y, this.x].isTreasure == Treasure.Contains;
                        moved();
                        return wasTreasure ? MoveResult.TreasureFound : MoveResult.Moved;
                    }
                    break;
                case Direction.Right:
                    if (this.map[this.y, this.x] != null && this.map[this.y, this.x].ways[1])
                    {
                        this.x = this.x + 1;
                        bool wasTreasure = this.map[this.y, this.x] != null && this.map[this.y, this.x].isTreasure == Treasure.Contains;
                        moved();
                        return wasTreasure ? MoveResult.TreasureFound : MoveResult.Moved;
                    }
                    break;
                case Direction.Down:
                    if (this.map[this.y, this.x] != null && this.map[this.y, this.x].ways[2])
                    {
                        this.y = this.y + 1;
                        bool wasTreasure = this.map[this.y, this.x] != null && this.map[this.y, this.x].isTreasure == Treasure.Contains;
                        moved();
                        return wasTreasure ? MoveResult.TreasureFound : MoveResult.Moved;
                    }
                    break;
                case Direction.Left:
                    if (this.map[this.y, this.x] != null && this.map[this.y, this.x].ways[3])
                    {
                        this.x = this.x - 1;
                        bool wasTreasure = this.map[this.y, this.x] != null && this.map[this.y, this.x].isTreasure == Treasure.Contains;
                        moved();
                        return wasTreasure ? MoveResult.TreasureFound : MoveResult.Moved;
                    }
                    break;
                default:
                    return MoveResult.Blocked;
            }
            return MoveResult.Blocked;
        }

        public void RestoreState(int x, int y, bool started, bool onMap, IEnumerable<(int row, int col)> collectedTreasures)
        {
            this.x = x;
            this.y = y;
            this.isStarted = started;
            this.isOnMap = onMap;
            this.treasureCount = 0;

            foreach (var pos in collectedTreasures)
            {
                if (pos.row < 0 || pos.col < 0 || pos.row >= this.map.GetLength(0) || pos.col >= this.map.GetLength(1))
                {
                    continue;
                }
                if (this.map[pos.row, pos.col] != null && this.map[pos.row, pos.col].isTreasure == Treasure.Contains)
                {
                    this.map[pos.row, pos.col].isTreasure = Treasure.Collected;
                    this.treasureCount++;
                }
            }
        }
    }
}
