using System.Runtime.CompilerServices;

namespace PsyCross.OpenTK.Utilities {
    using global::OpenTK.Graphics.OpenGL4;

    static class ObjectUtility {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name) {
            GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateTexture(TextureTarget target, string Name, out int texture) {
            GL.CreateTextures(target, 1, out texture);
            LabelObject(ObjectLabelIdentifier.Texture, texture, $"texture: {Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateProgram(string Name, out int program) {
            program = GL.CreateProgram();
            LabelObject(ObjectLabelIdentifier.Program, program, $"program: {Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateShader(ShaderType type, string name, out int shader) {
            shader = GL.CreateShader(type);
            LabelObject(ObjectLabelIdentifier.Shader, shader, $"shader: {type}: {name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateBuffer(string name, out int buffer) {
            GL.CreateBuffers(1, out buffer);
            LabelObject(ObjectLabelIdentifier.Buffer, buffer, $"buffer: {name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateVertexBuffer(string Name, out int buffer) => CreateBuffer($"VBO: {Name}", out buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateElementBuffer(string Name, out int buffer) => CreateBuffer($"EBO: {Name}", out buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateVertexArray(string name, out int vao) {
            GL.CreateVertexArrays(1, out vao);
            LabelObject(ObjectLabelIdentifier.VertexArray, vao, $"vao: {name}");
        }
    }
}
