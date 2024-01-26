using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoBase {
	public class GenerationPercentsPlotter{
		private readonly int _maxGenerations;
		private readonly int _reportCount;
		private int _currGeneration = 0;
		private int _report = 0;
		private TextWriter _textWriter;
		private bool _writeLineEnd;
		private const bool _defaultWriteEnd = true;

		public GenerationPercentsPlotter(int maxGenerations, int reportCount, bool writeLineEnd = _defaultWriteEnd) 
			:this(maxGenerations, reportCount, Console.Out) { }
		public GenerationPercentsPlotter(int maxGenerations, int reportCount, TextWriter textWriter, bool writeLineEnd = _defaultWriteEnd) {
			_maxGenerations = maxGenerations;
			_reportCount = reportCount;
			_textWriter = textWriter;
			_writeLineEnd = writeLineEnd;
		}

		public void Plot<T>(IEnumerable<Scored<T>> generation) {
			var bound = (_report + 1.0f) / (_reportCount + 1);
			if((_currGeneration + 1.0f) / _maxGenerations > bound) {
				_report++;
				var text = $"Gen {_currGeneration} - {_currGeneration * 100.0f / _maxGenerations}%";
				if(_writeLineEnd)
					_textWriter.WriteLine(text);
				else
					_textWriter.Write(text);
			}
			_currGeneration++;
		}
	}
}
