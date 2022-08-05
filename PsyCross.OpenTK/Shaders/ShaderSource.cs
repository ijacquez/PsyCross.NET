
namespace PsyCross.OpenTK.Types {
    using global::OpenTK.Graphics.OpenGL4;

    internal struct ShaderSource {
        public ShaderType Type { get; set; }

        public string Source { get; set; }
    }
}
