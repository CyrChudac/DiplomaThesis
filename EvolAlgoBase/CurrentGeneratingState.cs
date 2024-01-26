
namespace EvolAlgoBase {

	public sealed class CurrentGeneratingState {
		public int Generations { get; }
		public double TimeSpent { get; }
		public float BestScoring { get; }
		public CurrentGeneratingState(int Generations, double TimeSpent, float BestScoring) {
			this.Generations = Generations;
			this.TimeSpent = TimeSpent;
			this.BestScoring = BestScoring;
		}
	}
}