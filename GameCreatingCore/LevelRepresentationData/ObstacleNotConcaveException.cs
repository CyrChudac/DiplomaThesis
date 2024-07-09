using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.LevelRepresentationData
{
    public class ObstacleNotConcaveException : InvalidOperationException
    {
        internal ObstacleNotConcaveException(string message)
            : base($"Obstacle shape was not concave when initializing. [{message}]")
        { }
    }
}
