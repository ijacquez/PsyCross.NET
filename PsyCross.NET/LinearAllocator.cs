using System;

namespace PsyCross {
    public class LinearAllocator<TObject> {
        private readonly TObject[] _objects;

        public ReadOnlySpan<TObject> Objects => _objects.AsSpan(0, _index);
        private int _index;

        public int Capacity { get; }

        public int Count => _index;

        private LinearAllocator() {
        }

        public LinearAllocator(int capacity, Func<TObject> objectCreator) {
            Capacity = capacity;
            _objects = new TObject[capacity];

            for (int i = 0; i < Capacity; i++) {
                _objects[i] = objectCreator();
            }
        }

        public bool AllocateObject(out TObject @object) {
            @object = default(TObject);

            if (_index >= Capacity) {
                return false;
            }

            @object = _objects[_index];

            _index++;

            return true;
        }

        public void Reset() {
            _index = 0;
        }
    }
}
