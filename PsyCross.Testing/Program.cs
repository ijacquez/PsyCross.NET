using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using System;
using PsyCross.OpenTK;

namespace PsyCross.Testing {
    static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            TestingPSX psx = new TestingPSX();
            Window window = new Window(psx);

            window.Run();
        }
    }
}
