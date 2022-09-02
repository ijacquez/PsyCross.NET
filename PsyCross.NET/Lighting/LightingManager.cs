using System.Collections.Generic;

namespace PsyCross {
    public static class LightingManager {
        public static readonly int DirectionalLightsCapacity = 3;
        public static readonly int PointLightsCapacity       = 2;

        private static readonly ObjectPool<DirectionalLight> _DirectionalLightPool =
            new ObjectPool<DirectionalLight>(DirectionalLightsCapacity, DirectionalLightCreator);
        private static readonly ObjectPool<PointLight> _PointLightPool =
            new ObjectPool<PointLight>(PointLightsCapacity, PointLightCreator);

        public static IReadOnlyList<PointLight> PointLights => _PointLightPool.Objects;
        public static IReadOnlyList<DirectionalLight> DirectionalLights => _DirectionalLightPool.Objects;

        public static int Capacity => _PointLightPool.Capacity + _DirectionalLightPool.Capacity;

        public static int Count => (_PointLightPool.Count + _DirectionalLightPool.Count);

        static LightingManager() {
        }

        public static DirectionalLight AllocateDirectionalLight() => _DirectionalLightPool.AllocateObject();

        public static PointLight AllocatePointLight() => _PointLightPool.AllocateObject();

        public static void FreeLight(DirectionalLight light) => _DirectionalLightPool.FreeObject(light);

        public static void FreeLight(PointLight light) => _PointLightPool.FreeObject(light);

        private static DirectionalLight DirectionalLightCreator() => new DirectionalLight();

        private static PointLight PointLightCreator() => new PointLight();
    }
}
