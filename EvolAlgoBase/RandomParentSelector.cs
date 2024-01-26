using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoBase {
	public class RandomParentSelector<T> : IParentSelector<T>{
		private readonly IRandomGen _random;
		public RandomParentSelector(IRandomGen r) {
			_random = r;
		}
		public List<Scored<T>> GetParents(List<Scored<T>> population, int count) {
			var pop = population.ToList();
			List<Scored<T>> result = new List<Scored<T>>();
			for(int i = 0; i < count; i++) {
				result.Add(pop[_random.RandomInt(pop.Count)]);
			}
			return result;
		}

		public void InitializeNewGeneration() {}
	}
}
