using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototype
{
    public class List<T>
    {
        private SizableArray<T> _payload = new (0, 0);

        public void Add(T item)
        {
            if (_payload.Count == _payload.Capacity)
            {
                // need more room
                var newCap = Math.Max(_payload.Capacity * 2, 4);
                SizableArray<T>.Resize(ref _payload, newCap, _payload.Count);
            }

            _ = SizableArray<T>.IncreaseCount(ref _payload);
            _payload[_payload.Count - 1] = item;
        }

        public T this[int index]
        {
            get => _payload[index];
            set => _payload[index] = value;
        }

        public void Clear() => _payload.Clear();
        public int Count => _payload.Count;
    }
}
