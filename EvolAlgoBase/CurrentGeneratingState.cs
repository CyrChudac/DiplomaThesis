
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
		public override string ToString() {
			return $"{nameof(CurrentGeneratingState)}: G-{Generations}; T-{TimeSpent}; S-{BestScoring}";
		}
	}
}