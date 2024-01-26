using System;
using System.Collections.Generic;
using System.Text;

namespace EvolAlgoBase {
	public class InitialPopCountException : ArgumentException {
		internal InitialPopCountException(int desiredCount, int actualCount)
			: base($"Initial population needs to have at least {desiredCount}, but only {actualCount} was available."){ }
	}
}
