using System.Collections.Generic;
using PsyCross.Math;

namespace PsyCross {
    public static class LightingManager {
        public static readonly int DirectionalLightsCapacity = 8;
        public static readonly int PointLightsCapacity       = 8;
        public static readonly int Capacity                  = DirectionalLightsCapacity + PointLightsCapacity;

        private static readonly List<Light> _DirectionalLights = new List<Light>(DirectionalLightsCapacity);
        private static readonly List<Light> _PointLights = new List<Light>(PointLightsCapacity);

        private static readonly List<DirectionalLight> _AllocatedDirectionalLights = new List<DirectionalLight>(DirectionalLightsCapacity);
        private static readonly List<PointLight> _AllocatedPointLights = new List<PointLight>(PointLightsCapacity);

        public static IReadOnlyList<PointLight> PointLights => _AllocatedPointLights.AsReadOnly();
        public static IReadOnlyList<DirectionalLight> DirectionalLights => _AllocatedDirectionalLights.AsReadOnly();

        public static int Count => (_AllocatedPointLights.Count + _AllocatedDirectionalLights.Count);

        static LightingManager() {
            for (int i = 0; i < Capacity; i++) {
                _PointLights.Add(new PointLight());
                _DirectionalLights.Add(new DirectionalLight());
            }

            _PointLights.TrimExcess();
            _DirectionalLights.TrimExcess();
        }

        public static DirectionalLight AllocateDirectionalLight() =>
            AllocateLight<DirectionalLight>(_DirectionalLights, _AllocatedDirectionalLights);

        public static PointLight AllocatePointLight() =>
            AllocateLight<PointLight>(_PointLights, _AllocatedPointLights);

        private static T AllocateLight<T>(List<Light> lights, List<T> allocatedLights) where T : Light {
            if (lights.Count == 0) {
                return null;
            }

            T light = (T)lights[0];

            lights.RemoveAt(0);

            light.Init();

            allocatedLights.Add(light);

            return light;
        }

        public static void FreeLight(DirectionalLight light) {
            _AllocatedDirectionalLights.Remove(light);
            _DirectionalLights.Add(light);
        }

        public static void FreeLight(PointLight light) {
            _AllocatedPointLights.Remove(light);
            _PointLights.Add(light);
        }
    }
}
