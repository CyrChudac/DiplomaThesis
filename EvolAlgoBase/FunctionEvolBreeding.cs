using System;
using System.Collections.Generic;
using System.Text;

namespace EvolAlgoBase {
	public class FunctionEvolBreeding<TIndividuum> : EvolutionBreeding<TIndividuum> {
		public FunctionEvolBreeding(IRandomGen random, 
			int parentCount, int childCount, float crossProb,
			Func<IList<TIndividuum>, IList<TIndividuum>> crossFun,
			Func<IList<TIndividuum>, IList<TIndividuum>> withoutCrossFun
			) : base(random) {
			ParentCount = parentCount;
			ChildCount = childCount;
			CrossoverProb = crossProb;
			_crossFun = crossFun;
			_withoutCrossFun = withoutCrossFun;
		}

		public override int ParentCount { get; }

		public override int ChildCount { get; }

		protected override float CrossoverProb { get; }

		private Func<IList<TIndividuum>, IList<TIndividuum>> _crossFun;
		private Func<IList<TIndividuum>, IList<TIndividuum>> _withoutCrossFun;

		protected override IList<TIndividuum> Crossover(IList<TIndividuum> parents) {
			return _crossFun(parents);
		}

		protected override IList<TIndividuum> WithoutCrossover(IList<TIndividuum> parents) {
			return _withoutCrossFun(parents);
		}
	}
}
