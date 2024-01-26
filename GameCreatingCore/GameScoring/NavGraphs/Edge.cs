using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GameScoring.NavGraphs {
	internal class Edge<T> where T : Node
    {
        public T First { get; }
        public T Second { get; }
        public float Score { get; }

        public Edge(T first, T second, float score)
        {
            First = first;
            Second = second;
            Score = score;
        }
    }
}
