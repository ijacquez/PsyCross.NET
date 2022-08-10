using System.IO;
using System.Reflection;
using System.Text;
using System;

namespace PsyCross.ResourceManagement {
    public static class ResourceManager {
        private static string _ResourcesPath;

        static ResourceManager() {
            _ResourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                                          "Resources");
        }

        /// <summary>
        ///
        /// </summary>
        public static byte[] GetTimFile(string filePath) {
            using (FileStream fileStream = GetFile(filePath)) {
                Span<byte> buffer = new byte[fileStream.Length];

                // XXX: What if the entire file is not read?
                fileStream.Read(buffer);

                return buffer.ToArray();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static string GetTextFile(string filePath) {
            using (FileStream fileStream = GetFile(filePath)) {
                Span<byte> buffer = new byte[fileStream.Length];

                // XXX: What if the entire file is not read?
                fileStream.Read(buffer);

                return Encoding.ASCII.GetString(buffer);
            }
        }

        private static FileStream GetFile(string filePath) {
            string fullPath = Path.GetFullPath(Path.Combine(_ResourcesPath, filePath));

            return File.Open(fullPath, FileMode.Open);
        }
    }
}
