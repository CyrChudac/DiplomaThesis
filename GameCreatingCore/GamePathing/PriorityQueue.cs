﻿using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections;

namespace GameCreatingCore.GamePathing
{
    internal class PriorityQueue<TKey, TValue> : IEnumerable<(TKey, TValue)>
    {
        private readonly SortedDictionary<TKey, Queue<TValue>> _queue;

        [DebuggerNonUserCode]
        public PriorityQueue(IComparer<TKey> comparer)
        {
            _queue = new SortedDictionary<TKey, Queue<TValue>>(comparer);
        }

        [DebuggerNonUserCode]
        public PriorityQueue()
        {
            _queue = new SortedDictionary<TKey, Queue<TValue>>();
        }

        public int Count { get; private set; } = 0;

        [DebuggerNonUserCode]
        public void Enqueue(TKey key, TValue val)
        {
            if (!_queue.ContainsKey(key))
            {
                _queue.Add(key, new Queue<TValue>());
            }
            _queue[key].Enqueue(val);
            Count++;
        }

        [DebuggerNonUserCode]
        public TValue DequeueMin()
        {
            var key = _queue.Keys.First();
            var list = _queue[key];
            var result = list.Dequeue();
            Count--;
            if (list.Count == 0)
            {
                _queue.Remove(key);
            }
            return result;
        }

        [DebuggerNonUserCode]
        public (TKey Key, TValue Value) DequeueMinWithKey()
        {
            var key = _queue.Keys.First();
            var list = _queue[key];
            var result = list.Dequeue();
            Count--;
            if (list.Count == 0)
            {
                _queue.Remove(key);
            }
            return (key, result);
        }

        [DebuggerNonUserCode]
        public TValue PeekMin()
        {
            var key = _queue.Keys.First();
            var list = _queue[key];
            return list.Peek();
        }

        [DebuggerNonUserCode]
        public bool Any() => Count > 0;

        public override string ToString()
        {
            return $"{nameof(PriorityQueue<TKey, TValue>)}: {Count}";
        }

        public IEnumerator<(TKey, TValue)> GetEnumerator()
        {
            foreach (var p in _queue)
            {
                foreach (var v in p.Value)
                {
                    yield return (p.Key, v);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
