using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public static partial class Renderer {
        private static void CalculateFog(Render render, GenPrimitive genPrimitive) {
            // Perform fog
            // float FogStart = render.Camera.DepthNear + 2.5f;
            // float FogEnd = FogStart*10;
            float FogStart = 0.8f;
            float FogEnd = 4f;

            float FogDifferenceDenom = 1f / (FogEnd - FogStart);
            // DQA
            float FogCoefficient = -(FogStart * FogEnd) * FogDifferenceDenom;
            // DQB
            float FogOffset = FogEnd * FogDifferenceDenom;
            // XXX: Move over as a function
            Func<float, float> CalculateFogIntensity = (z) =>
                System.Math.Clamp(((FogCoefficient / z) + FogOffset), 0f, 1f);

            Vector3 bgColor = render.DrawEnv.Color;
            Vector3 ambientColor = Rgb888.White;

            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                float z = System.Math.Max(genPrimitive.ViewPoints[i].Z, render.Camera.DepthNear);

                // https://www.sjbaker.org/steve/omniv/love_your_z_buffer.html
                // Fog intensity: [0..1]
                float fogIntensity = CalculateFogIntensity(z);

                Vector3 lerpedColor = Vector3.Lerp(ambientColor, bgColor, fogIntensity);
                Rgb888 color = lerpedColor;

                genPrimitive.GouraudShadingColors[i] = color;
            }
        }
    }
}
