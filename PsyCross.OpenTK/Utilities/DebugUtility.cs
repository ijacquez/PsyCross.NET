using System.Diagnostics;

namespace PsyCross.OpenTK.Utilities {
    using global::OpenTK.Graphics.OpenGL;

    public static class DebugUtility {
        [Conditional("DEBUG")]
        public static void CheckGLError(string title) {
            var error = GL.GetError();
            if (error != ErrorCode.NoError) {
                Debug.Print($"{title}: {error}");
                // System.Console.WriteLine($"{title}: {error}");
                // throw new System.Exception($"{title}: {error}");
            }
        }
    }
}
