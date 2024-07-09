using System.Collections;
using System.Collections.Generic;

namespace GameCreatingCore
{
    internal class HashedReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _list;

        public HashedReadOnlyList(IReadOnlyList<T> list)
        {
            _list = list;
        }

        public override int GetHashCode()
            => GetListHashCode(_list);

        public static int GetListHashCode(IReadOnlyList<T> list)
        {
            int result = 0;
            unchecked
            {
                for (int i = 0; i < list.Count; i++)
                {
                    result = result * 17 + (list[i]?.GetHashCode() ?? 0);
                }
            }
            return result;
        }

        public T this[int index] => _list[index];

        public int Count => _list.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
    }
}
