using UnityEngine;
using System.Collections.Generic;

namespace GameCreatingCore {
	[System.Serializable]
	public sealed class Path {
		[SerializeField]
		public bool Cyclic;
		[SerializeField]
		[SerializeReference]
		public List<PatrolCommand> Commands;

		public Path(bool cyclic, List<PatrolCommand> commands) {
			Cyclic = cyclic;
			Commands = commands;
		}
	}
}