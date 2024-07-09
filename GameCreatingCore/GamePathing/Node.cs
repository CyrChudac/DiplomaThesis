using UnityEngine;

namespace GameCreatingCore.GamePathing {
	public class Node
    {
        public Vector2 Position { get; }
        public Node(Vector2 value)
        {
            Position = value;
        }

        public override string ToString() {
            var res = Position.ToString();
            res = $"<{res[1..(res.Length-1)]}>";
            return res ;
        }

		public override int GetHashCode() {
			return Position.GetHashCode();
		}

        public override bool Equals(object obj) { 
            return obj != null && obj is Node && ((Node)obj).Position.Equals(Position);
        }
	}
}
