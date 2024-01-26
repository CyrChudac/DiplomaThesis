using System.Xml.Linq;

namespace EvolAlgoBase {
	public class Mutator<TIndividuum> {
		private readonly float _mutProb;
		private readonly MutateFunc<TIndividuum> _mutFunc;
		private readonly IRandomGen _random;
		public Mutator(float mutProb, IRandomGen random, MutateFunc<TIndividuum> mutFunc) {
			_mutProb = mutProb;
			_mutFunc = mutFunc;
			_random = random;
		}
		public TIndividuum Invoke(TIndividuum individuum) {
			if(_random.RandomFloat() < _mutProb) {
				return _mutFunc(individuum);
			}
			return individuum;
		}
	}
}
