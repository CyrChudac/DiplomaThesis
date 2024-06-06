using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore {
	internal class SimpleEnumerator<T> {
		private IEnumerator<T> Enumerator { get; }
		private bool ended = false;
		private readonly T Default;
		public SimpleEnumerator(IEnumerable<T> enumerable, T @default) {
			Enumerator = enumerable.GetEnumerator();
			Default = @default;
		}

		public T Get() {
			if(!ended) {
				ended = !Enumerator.MoveNext();
				return Enumerator.Current;
			}
			return Default;
		}
	}
}
