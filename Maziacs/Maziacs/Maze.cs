using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace Maziacs
{
    public class Maze
    {
        //public Cell[,] Cells;
        public Cell[] Cells;
        public Cell StartCell;
        public Cell GoalCell;
        public Cell LastRebuildTarget;

        List<Cell> visitedCells;

        public List<Cell> solutionPath;
        
        int width;
        int height;

        int difficulty;

        public enum State
        {
            Wall,
            Out,
            Frontier,
            Path,
            Prisoner,
            Sword,
            Food,
            Treasure,
            Start
        }

        Random random = new Random();

        public void Initialize(int width, int height, int difficulty)
        {
            this.width = width;
            this.height = height;
            this.difficulty = difficulty;

            Cells = new Cell[this.width * this.height];

            visitedCells = new List<Cell>();
            
            solutionPath = new List<Cell>();
        }

        private int[] PickRandomCell()
        {
            int x, y;

            x = random.Next(1, width - 2);
            y = random.Next(1, height - 2);

            if (x % 2 == 0)
            {
                x = (x > width / 2) ? x - 1 : x + 1;
            }

            if (y % 2 == 0)
            {
                y = (y > height / 2) ? y - 1 : y + 1;
            }

            return new int[2] { x, y };
        }
        
        private List<int[]> FindNeighbours(int x, int y, State state)
        {
            int d;
            int[] dx = { 0, 0, -2, 2 };
            int[] dy = { -2, 2, 0, 0 };

            List<int[]> neighbours = new List<int[]>();
            for (d = 0; d < 4; d++)
            {
                if (x + dx[d] > 0 && x + dx[d] < width - 1 && y + dy[d] > 0 && y + dy[d] < height - 1 && Cells[(x + dx[d]) + (y + dy[d]) * width].State == state)
                {
                    neighbours.Add(new int[] { x + dx[d], y + dy[d] });
                }
            }
            
            return neighbours;
        }
        
        private List<Cell> FindNeighbours(Cell t, State state)
        {
            int d;
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            List<Cell> neighbours = new List<Cell>();

            for (d = 0; d < 4; d++)
            {
                if (t.X + dx[d] > 0 && t.X + dx[d] < width - 1 && t.Y + dy[d] > 0 && t.Y + dy[d] < height - 1 && Cells[(t.X + dx[d]) + (t.Y + dy[d]) * width].State == state)
                {
                    Cell o = Cells[(t.X + dx[d]) + (t.Y + dy[d]) * width];

                    if (o.Visited == false)
                    {
                        neighbours.Add(o);
                    }
                }
            }

            return neighbours;
        }
        private int[] FindRandomNeighbour(int x, int y, State state)
        {
            int d;
            int[] dx = { 0, 0, -2, 2 };
            int[] dy = { -2, 2, 0, 0 };

            do
            {
                d = random.Next(4);
            }
            while (x + dx[d] < 0 || x + dx[d] > width - 1 || y + dy[d] < 0 || y + dy[d] > height - 1 || Cells[(x + dx[d]) + (y + dy[d]) * width].State < state);
            
            return new int[] { dx[d], dy[d] };
        }

        private Cell FindRandomStart()
        {
            DateTime tstart = DateTime.Now;
            int x, y;

            Cell start = new Cell();
            bool found = false;

            do
            {
                int[] randomCell = PickRandomCell();
                x = randomCell[0];
                y = randomCell[1];

                List<Cell> neighbours = FindNeighbours(Cells[x + y * width], State.Wall);

                if (neighbours.Count == 3 && Cells[x + y * width].Distance > GameSettings.MinimumDistance[difficulty] && Cells[x + y * width].Distance < GameSettings.MaximumDistance[difficulty])
                {
                    start = Cells[x + y * width];
                    start.State = State.Start;
                    found = true;
                }
            }
            while (found == false);

            return start;
        }

        private Cell FindRandomGoal()
        {
            int x, y;

            Cell goal = new Cell();
            bool found = false;

            do
            {
                int[] randomCell = PickRandomCell();
                x = randomCell[0];
                y = randomCell[1];

                List<Cell> neighbours = FindNeighbours(Cells[x + y * width], State.Wall);

                if (neighbours.Count == 3)
                {
                    goal = Cells[x + y * width];
                    goal.State = State.Treasure;
                    found = true;
                }
            }
            while (found == false);

            return goal;
        }

        public void PlaceRandomItems()
        {
            List<Cell> itemCells = new List<Cell>();

            int x, y, count, rnd;

            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    if (Cells[x + y * width].State == State.Wall)
                    {
                        List<Cell> neighbours = FindNeighbours(Cells[x + y * width], State.Path);

                        if (neighbours.Count > 0)
                        {
                            itemCells.Add(Cells[x + y * width]);
                        }
                    }
                }
            }

            count = 0;

            while (count < GameSettings.Prisoners[difficulty])
            {
                rnd = random.Next(0, itemCells.Count);
                Cell c = itemCells[rnd];
                c.State = State.Prisoner;
                itemCells.RemoveAt(rnd);
                count++;
            }

            count = 0;

            while (count < GameSettings.Swords[difficulty])
            {
                rnd = random.Next(0, itemCells.Count);
                Cell c = itemCells[rnd];
                c.State = State.Sword;
                itemCells.RemoveAt(rnd);
                count++;
            }

            count = 0;

            while (count < GameSettings.Food[difficulty])
            {
                rnd = random.Next(0, itemCells.Count);
                Cell c = itemCells[rnd];
                c.State = State.Food;
                itemCells.RemoveAt(rnd);
                count++;
            }
        }

        public void Generate()
        {
            int x, y, n;

            int[][] todo = new int[width * height][];
            int todonum = 0;

            Cells = new Cell[width * height];

            List<int[]> neighbours;

            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    // Mark all even cells as Wall.
                    if (x % 2 == 0 || y % 2 == 0)
                    {
                        Cells[x + y * width] = new Cell();
                        Cells[x + y * width].X = x;
                        Cells[x + y * width].Y = y;
                        Cells[x + y * width].State = State.Wall;
                    }
                    // Mark the rest as Out.
                    else
                    {
                        Cells[x + y * width] = new Cell();
                        Cells[x + y * width].X = x;
                        Cells[x + y * width].Y = y;
                        Cells[x + y * width].State = State.Out;
                    }
                }
            }

            // Pick a random cell and mark it Path.
            int[] randomCell = PickRandomCell();
            x = randomCell[0];
            y = randomCell[1];

            Cells[x + y * width].X = x;
            Cells[x + y * width].Y = y;
            Cells[x + y * width].State = State.Path;

            // Mark all neighbours of current cell as Frontier.
            neighbours = FindNeighbours(x, y, State.Out);
            foreach (int[] neighbour in neighbours)
            {
                todo[todonum++] = neighbour;
                Cells[neighbour[0] + neighbour[1] * width].State = State.Frontier;
            }

            while (todonum > 0)
            {
                // Pick a random cell from Frontier and mark it as Path.
                n = random.Next(todonum);
                x = todo[n][0];
                y = todo[n][1];

                todo[n] = todo[--todonum];

                // Mark current cell as State.Path
                Cells[x + y * width].State = State.Path;

                // Pick a random neighbour of current cell that is Path and mark the cell between it as Path.
                int[] randomNeighbour = FindRandomNeighbour(x, y, State.Path);
                Cells[(((x + randomNeighbour[0] / 2) + (y + randomNeighbour[1] / 2) * width))].State = State.Path;

                // Mark all neighbours of current cell as State.Frontier
                neighbours = FindNeighbours(x, y, State.Out);
                foreach (int[] neighbour in neighbours)
                {
                    todo[todonum++] = neighbour;
                    Cells[neighbour[0] + neighbour[1] * width].State = State.Frontier;
                }
            }

            PlaceRandomItems();

            // Find random goal  location enclosed with 3 walls
            GoalCell = FindRandomGoal();

            BuildDistanceTable(GoalCell);
            
            // Find random starting location enclosed with 3 walls
            StartCell = FindRandomStart();
        }

        public void BuildDistanceTable(Cell start)
        {
            LastRebuildTarget = start;
            
            List<Cell>[] distanceTable = new List<Cell>[width * height];

            int i = 0;

            start.Visited = true;
            start.Distance = i;

            distanceTable[i] = new List<Cell>();
            distanceTable[i].Add(start);

            while (distanceTable[i].Count > 0)
            {
                distanceTable[i + 1] = new List<Cell>();
                foreach (Cell t in distanceTable[i])
                {
                    List<Cell> neighbours = FindNeighbours(t, State.Path);
                    foreach (Cell o in neighbours)
                    {
                        o.Visited = true;
                        o.Distance = i + 1;
                        distanceTable[i + 1].Add(o);
                    }
                }
                i++;
            }

            i = 0;
            while (distanceTable[i].Count > 0)
            {
                foreach (Cell t in distanceTable[i])
                {
                    t.Visited = false;
                }
                distanceTable[i].Clear();
                i++;
            }
        }

        public void ClearPath()
        {
            foreach (Cell o in solutionPath)
            {
                o.IsSolution = false;
            }

            solutionPath.Clear();
        }

        public void FindPath(Cell start, Cell goal)
        {
            int i;

            // Clear previous path.
            ClearPath();

            if (start == goal)
            {
                return;
            }

            i = goal.Distance;
            Cell c = goal;
            
            solutionPath.Add(c);

            while (i >= 0)
            {
                c.IsSolution = true;
                List<Cell> neighbours = FindNeighbours(c, State.Path);
                foreach (Cell o in neighbours)
                {
                    if (o.Distance < i)
                    {
                        solutionPath.Add(o);
                        c = o;
                        break;
                    }
                }
                i--;
            }
        }
    }
}
