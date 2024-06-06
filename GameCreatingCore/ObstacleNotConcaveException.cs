using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore {
	public class ObstacleNotConcaveException : System.InvalidOperationException {
		internal ObstacleNotConcaveException(string message) 
			:base($"Obstacle shape was not concave when initializing. [{message}]")
			{ }
	}
}
