using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore {
	/// <summary>
	/// A dictionary where D[x,y] == D[y,x] is always true.
	/// </summary>
	internal class Dictionary2D<TKey, TValue> where TKey : IComparable<TKey>{
		private readonly Dictionary<TKey, Dictionary<TKey, TValue>> _dictionary
			= new Dictionary<TKey, Dictionary<TKey, TValue>>();

		public int Count { get; private set; } = 0;

		public void Add(TKey key1, TKey key2, TValue value) {
			if(key1.CompareTo(key2) > 0) {
				var tmp = key1;
				key1 = key2;
				key2 = tmp;
			}
			Dictionary<TKey, TValue> inner;
			if(!_dictionary.TryGetValue(key1, out inner)) {
				inner = new Dictionary<TKey, TValue>();
				_dictionary.Add(key1, inner);
			}
			inner.Add(key2, value);
		}
		
		public TValue this[TKey key1, TKey key2] {
			get {
				if(key1.CompareTo(key2) > 0) {
					var tmp = key1;
					key1 = key2;
					key2 = tmp;
				}
				return _dictionary[key1][key2];
			}
		}
		public bool TryGetValue(TKey key1, TKey key2, out TValue value) {
			if(key1.CompareTo(key2) > 0) {
				var tmp = key1;
				key1 = key2;
				key2 = tmp;
			}
			if(_dictionary.TryGetValue(key1, out var d))
				if(d.TryGetValue(key2, out value)) {
					return true;
				}
			value = default;
			return false;
		}
	}
}
