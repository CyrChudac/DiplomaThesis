
namespace EvolAlgoLevelGenerator {
	
	public class ParentCountException : ArgumentException {
		public ParentCountException(int expectedCount, int actualCount) 
			:base($"Expected number of <u>parents</u> is {expectedCount} but got {actualCount} instead."){ }
	}
}