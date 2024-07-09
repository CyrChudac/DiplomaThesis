using System.Collections.Generic;
using GameCreatingCore.GameActions;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.LevelRepresentationData; 

namespace GameCreatingCore.LevelSolving
{
    public interface IGamePathSolver
    {
        /// <summary>
        /// Calculates the path for the given <paramref name="levelRepresentation"/> within the rules given by <paramref name="staticGameRepresentation"/>.
        /// </summary>
        /// <returns>The path. Null if it doesn't exist.</returns>
        List<IGameAction>? GetPath(
            StaticGameRepresentation staticGameRepresentation,
            LevelRepresentation levelRepresentation);

        List<List<IGameAction>>? GetFullPathsTree(
            StaticGameRepresentation staticGameRepresentation,
            LevelRepresentation levelRepresentation);
    }
}
