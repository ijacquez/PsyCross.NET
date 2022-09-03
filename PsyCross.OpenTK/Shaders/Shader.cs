using PsyCross.OpenTK.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

namespace PsyCross.OpenTK.Types {
    using global::OpenTK.Graphics.OpenGL4;
    using global::OpenTK.Mathematics;

    internal class Shader {
        public string Name { get; }

        public int Handle { get; private set; }

        public IReadOnlyList<ShaderSource> Sources =>
            Array.AsReadOnly(_sources);

        private readonly Dictionary<string, int> _uniformToLocation =
            new Dictionary<string, int>();
        private readonly Dictionary<string, int> _attribToLocation =
            new Dictionary<string, int>();

        private bool _initialized;

        private readonly ShaderSource[] _sources;

        private Shader() {
        }

        public Shader(string name, params ShaderSource[] shaderSources) {
            Name = name;

            _sources = GetUniqueSources(shaderSources);

            Handle = CreateProgram(name, _sources);

            foreach (UniformFieldInfo fieldInfo in GetUniforms()) {
                _uniformToLocation.Add(fieldInfo.Name, fieldInfo.Location);
            }

            foreach (AttribFieldInfo fieldInfo in GetAttribs()) {
                _attribToLocation.Add(fieldInfo.Name, fieldInfo.Location);
            }
        }

        public void Bind() {
            GL.UseProgram(Handle);
            DebugUtility.CheckGLError($"GL.UseProgram({Handle})");
        }

        public void Dispose() {
            if (_initialized) {
                GL.DeleteProgram(Handle);

                _initialized = false;
            }
        }

        public UniformFieldInfo[] GetUniforms() {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);

            UniformFieldInfo[] uniforms = new UniformFieldInfo[uniformCount];

            for (int i = 0; i < uniformCount; i++) {
                string name = GL.GetActiveUniform(Handle, i, out int Size, out ActiveUniformType Type);

                UniformFieldInfo fieldInfo = new UniformFieldInfo();

                fieldInfo.Location = GL.GetUniformLocation(Handle, name);
                fieldInfo.Name = name;
                fieldInfo.Size = Size;
                fieldInfo.Type = Type;

                uniforms[i] = fieldInfo;
            }

            return uniforms;
        }

        public AttribFieldInfo[] GetAttribs() {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveAttributes, out int attribCount);

            AttribFieldInfo[] attribs = new AttribFieldInfo[attribCount];

            for (int i = 0; i < attribCount; i++) {
                string name = GL.GetActiveAttrib(Handle, i, out int Size, out ActiveAttribType Type);

                AttribFieldInfo fieldInfo = new AttribFieldInfo();

                fieldInfo.Location = GL.GetAttribLocation(Handle, name);
                fieldInfo.Name = name;
                fieldInfo.Size = Size;
                fieldInfo.Type = Type;

                attribs[i] = fieldInfo;
            }

            return attribs;
        }

        public void SetBool(string uniform, bool value) {
            SetInt(uniform, (value) ? 1 : 0);
        }

        public void SetInt(string uniform, int value) {
            if (!_uniformToLocation.TryGetValue(uniform, out int location)) {
                System.Console.WriteLine($"The uniform '{uniform}' does not exist in the shader '{Name}'!");
            } else {
                Bind();

                GL.Uniform1(location, value);
                DebugUtility.CheckGLError($"GL.Uniform1({Name}, {location})");
            }
        }

        public void SetFloat(string uniform, float value) {
            if (!_uniformToLocation.TryGetValue(uniform, out int location)) {
                System.Console.WriteLine($"The uniform '{uniform}' does not exist in the shader '{Name}'!");
            } else {
                Bind();

                GL.Uniform1(location, value);
                DebugUtility.CheckGLError($"GL.Uniform1({Name}, {location})");
            }
        }

        public void SetVector2(string uniform, Vector2 value) {
            if (!_uniformToLocation.TryGetValue(uniform, out int location)) {
                System.Console.WriteLine($"The uniform '{uniform}' does not exist in the shader '{Name}'!");
            } else {
                Bind();

                GL.Uniform2(location, value.X, value.Y);
                DebugUtility.CheckGLError($"GL.Uniform1({Name}, {location})");
            }
        }

        public void SetMatrix4(string uniform, bool transpose, Matrix4 transform) {
            if (!_uniformToLocation.TryGetValue(uniform, out int location)) {
                System.Console.WriteLine($"The uniform '{uniform}' does not exist in the shader '{Name}'!");
            } else {
                Bind();

                GL.UniformMatrix4(location, transpose: transpose, ref transform);
                DebugUtility.CheckGLError($"GL.UniformMatrix4({Name}, {location})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetUniformLocation(string uniform) {
            if (!_uniformToLocation.TryGetValue(uniform, out int location)) {
                System.Console.WriteLine($"The uniform '{uniform}' does not exist in the shader '{Name}'!");
                return -1;
            }

            return GL.GetUniformLocation(Handle, uniform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAttribLocation(string attrib) {
            if (!_attribToLocation.TryGetValue(attrib, out int location)) {
                System.Console.WriteLine($"The attrib '{attrib}' does not exist in the shader '{Name}'!");
            }

            return GL.GetAttribLocation(Handle, attrib);
        }

        private int CreateProgram(string name, IList<ShaderSource> sources) {
            ObjectUtility.CreateProgram(name, out int program);

            int[] shaders = new int[sources.Count];

            for (int i = 0; i < shaders.Length; i++) {
                shaders[i] = CompileShader(name, sources[i].Type, sources[i].Source);
            }

            foreach (var shader in shaders) {
                GL.AttachShader(program, shader);
            }

            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0) {
                string infoLogString = GL.GetProgramInfoLog(program);
                System.Console.WriteLine($"GL.LinkProgram had info log [{name}]:\n{infoLogString}");
            }

            foreach (var shader in shaders) {
                GL.DetachShader(program, shader);
                GL.DeleteShader(shader);
            }

            _initialized = true;

            return program;
        }

        private int CompileShader(string name, ShaderType type, string source) {
            ObjectUtility.CreateShader(type, name, out int shader);

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0) {
                string infoLogString = GL.GetShaderInfoLog(shader);

                System.Console.WriteLine($"GL.CompileShader for shader '{Name}' [{type}] had info log:\n{infoLogString}");
            }

            return shader;
        }

        private static ShaderSource[] GetUniqueSources(ShaderSource[] shaderSources) {
            return shaderSources.Where((x) => !string.IsNullOrWhiteSpace(x.Source))
                                .ToArray();
        }
    }
}
