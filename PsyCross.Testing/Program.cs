using PsyCross.OpenTK;
using System;

namespace PsyCross.Testing {
    static class Program {
        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            var testing = new Testing();

            Psx.UpdateFrame += testing.Update;

            Window window = new Window();

            window.Run();
        }
    }
}
