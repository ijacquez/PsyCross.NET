using System;
using ProjectPSX.Devices;
using PsyCross.Devices.Input;

namespace PsyCross {
    public static class Psx {
        public static Gpu Gpu { get; } = new Gpu();

        public static JoyPad Input { get; private set; }

        public static Action UpdateFrame;

        public static void OnUpdateFrame() {
            UpdateFrame?.Invoke();
        }

        public static void OnJoyPadUp(JoyPad input) {
            Input = input;
        }

        public static void OnJoyPadDown(JoyPad input) {
            Input = input;
        }
    }
}
