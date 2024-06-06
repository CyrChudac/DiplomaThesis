using System;
using System.Collections.Generic;
using System.Text;
using GameCreatingCore.GamePathing.GameActions;
using UnityEngine;

namespace GameCreatingCore.GamePathing
{
    public interface IGamePathSolver {
		/// <summary>
		/// Calculates the path for the given <paramref name="levelRepresentation"/> within the rules given by <paramref name="staticGameRepresentation"/>.
		/// </summary>
		/// <returns>The path. Null if it doesn't exist.</returns>
		List<IGameAction>? GetPath(
			StaticSettings.StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation);
	}
}
