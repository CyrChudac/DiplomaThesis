namespace EvolAlgoLevelGenerator {
		
	public class ChildrenCountException : ArgumentException {
		public ChildrenCountException(int expectedCount, int actualCount) 
			:base($"Expected number of <u>children</u> is {expectedCount} but got {actualCount} instead."){ }
	}
}