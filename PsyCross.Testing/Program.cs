using PsyCross.OpenTK;
using System;

namespace PsyCross.Testing {
    public static class Program {
        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main() {
            var testing = new Testing();

            Psx.UpdateFrame += testing.Update;

            Window window = new Window();

            window.Run();
        }
    }
}
