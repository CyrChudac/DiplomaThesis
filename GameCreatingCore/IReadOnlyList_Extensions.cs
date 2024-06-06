using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore {
	internal static class IReadOnlyList_Extensions {
        public static int IndexOfMin<V>(this IReadOnlyList<V> self) where V : struct, IComparable
            => IndexOfMin(self, x => x);

        public static int IndexOfMin<T, V>(this IReadOnlyList<T> self, Func<T, V> valueFunc) where V : struct, IComparable{
            if (self == null) {
                throw new ArgumentNullException($"{nameof(self)}");
            }

            if (self.Count == 0) {
                throw new ArgumentException("List is empty.", $"{nameof(self)}");
            }

            V min = valueFunc(self[0]);
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i) {
                var val = valueFunc(self[i]);
                if (val.CompareTo(min) < 0) {
                    min = val;
                    minIndex = i;
                }
            }

            return minIndex;
        }
	}
}
