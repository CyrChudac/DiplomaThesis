using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCreatingCore {
	internal static class IReadOnlyList_Extensions {
        public static int IndexOfMin<V>(this IEnumerable<V> self) where V : struct, IComparable
            => IndexOfMin(self, x => x);

        public static int IndexOfMin<T, V>(this IEnumerable<T> self, Func<T, V> valueFunc) where V : struct, IComparable{
            if (self == null) {
                throw new ArgumentNullException($"{nameof(self)}");
            }

            if (!self.Any()) {
                throw new ArgumentException("List is empty.", $"{nameof(self)}");
            }

            V min = valueFunc(self.First());
            int minIndex = 0;

            int index = 1;
            foreach (var v in self.Skip(1)) {
                var val = valueFunc(v);
                if (val.CompareTo(min) < 0) {
                    min = val;
                    minIndex = index;
                }
                index++;
            }

            return minIndex;
        }
	}
}
