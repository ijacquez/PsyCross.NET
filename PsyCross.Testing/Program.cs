using PsyCross.OpenTK;
using System;

namespace PsyCross.Testing {
    static class Program {
        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Window window = new Window(new TestingPSX());

            window.Run();
        }
    }
}
