using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maziacs
{
    public class Cell
    {
        public int X;
        public int Y;

        public Maze.State State;

        public bool Visited = false;
        public bool IsSolution = false;

        // Solution stuff
        //public int Depth;
        public int Distance;
    }
}
