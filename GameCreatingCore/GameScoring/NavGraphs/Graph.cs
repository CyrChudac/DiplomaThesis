using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

namespace GameCreatingCore.GameScoring.NavGraphs {

    internal class Graph<T> where T : Node {
        public List<T> vertices;

        public List<Edge<T>> edges;

        public Graph(List<T> vertices, List<Edge<T>> edges){
            this.vertices = vertices;
            this.edges = edges;
        }

        

    }
}
