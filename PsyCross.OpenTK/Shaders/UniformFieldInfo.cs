
namespace PsyCross.OpenTK.Types {
    using global::OpenTK.Graphics.OpenGL4;

    internal struct UniformFieldInfo {
        public int Location { get; set; }

        public string Name { get; set; }

        public int Size { get; set; }

        public ActiveUniformType Type { get; set; }
    }
}
