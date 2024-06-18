using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore {
	public static class IList_Extensions {
		public static void RemoveFirst<T>(this IList<T> list)
			=> RemoveFirst(list, x => true);

		public static bool RemoveFirst<T>(this IList<T> list, Predicate<T> predicate) {
			for(int i = 0; i < list.Count; i++) {
				if(predicate(list[i])) {
					list.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		public static int IndexOf<T>(this IEnumerable<T> enumerable, Predicate<T> predicate) {
			int index = 0;
			foreach(T item in enumerable) {
				if(predicate(item)) {
					return index;
				}
				index++;
			}
			return -1;
		}
	}
}
