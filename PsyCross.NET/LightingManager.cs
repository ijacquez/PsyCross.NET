using System.Collections.Generic;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross {
    public static class LightingManager {
        public static readonly int Capacity = 16;

        private static readonly List<Light> _Lights = new List<Light>(Capacity);
        private static readonly List<Light> _AllocatedLights = new List<Light>(Capacity);

        public static IReadOnlyList<Light> AllocatedLights => _AllocatedLights.AsReadOnly();

        public static int Count => _AllocatedLights.Count;

        static LightingManager() {
            for (int i = 0; i < Capacity; i++) {
                _Lights.Add(new Light());
            }

            _Lights.TrimExcess();
        }

        public static Light AllocateLight() {
            if (_Lights.Count == 0) {
                return null;
            }

            Light light = _Lights[0];

            _Lights.RemoveAt(0);

            light.Position = Vector3.Zero;
            light.ConstantAttenuation = 1.0f;
            light.Color = Rgb888.White;
            light.DiffuseIntensity = 1.0f;
            light.CutOffDistance = 10.0f;
            light.Flags = LightFlags.Point;

            _AllocatedLights.Add(light);

            return light;
        }

        public static void FreeLight(Light light) {
            _AllocatedLights.Remove(light);
            _Lights.Add(light);
        }
    }
}
