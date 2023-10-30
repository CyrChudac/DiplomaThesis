using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoLevelGenerator {
	public class GenerationPercentsPlotter{
		private readonly int _maxGenerations;
		private readonly int _reportCount;
		private int _currGeneration = 0;
		private int _report = 0;

		public GenerationPercentsPlotter(int maxGenerations, int reportCount) {
			_maxGenerations = maxGenerations;
			_reportCount = reportCount;
		}

		public void Plot<T>(IEnumerable<Scored<T>> generation) {
			var bound = (_report + 1.0f) / (_reportCount + 1);
			if((_currGeneration + 1.0f) / _maxGenerations > bound) {
				_report++;
				Console.WriteLine($"Gen {_currGeneration} - {_currGeneration * 100.0f / _maxGenerations}%");
			}
			_currGeneration++;
		}
	}
}
