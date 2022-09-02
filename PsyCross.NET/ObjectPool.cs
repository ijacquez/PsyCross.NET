using System;
using System.Collections.Generic;

namespace PsyCross {
    public class ObjectPool<TObject> where TObject : class {
        private readonly List<TObject> _objects;
        private readonly List<TObject> _allocatedObjects;

        public IReadOnlyList<TObject> Objects => _allocatedObjects.AsReadOnly();

        public int Capacity { get; }

        public int Count => _allocatedObjects.Count;

        private ObjectPool() {
        }

        public ObjectPool(int capacity, Func<TObject> objectCreator) {
            Capacity = capacity;
            _objects = new List<TObject>(Capacity);
            _allocatedObjects = new List<TObject>(Capacity);

            for (int i = 0; i < Capacity; i++) {
                _objects.Add(objectCreator());
            }

            _objects.TrimExcess();
        }

        public TObject AllocateObject() =>
            AllocateObject(_objects, _allocatedObjects);

        public void FreeObject(TObject @object) {
            _allocatedObjects.Remove(@object);
            _objects.Add(@object);
        }

        private TObject AllocateObject(List<TObject> objects, List<TObject> allocatedObjects) {
            if (objects.Count == 0) {
                return null;
            }

            TObject @object = (TObject)objects[0];

            objects.RemoveAt(0);

            allocatedObjects.Add(@object);

            return @object;
        }
    }
}
