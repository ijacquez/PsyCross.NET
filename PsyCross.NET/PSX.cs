using ProjectPSX.Devices;
using PsyCross.Devices.Input;
using System;

namespace PsyCross {
    public static class Psx {
        public static Gpu Gpu { get; } = new Gpu();

        public static JoyPad Input { get; private set; }

        public static Time Time { get; } = new Time();

        public static Action UpdateFrame;

        public static void OnUpdateFrame(double time) {
            Time.UpdateTime(time);

            UpdateFrame?.Invoke();
        }

        public static void OnJoyPadUp(JoyPad inputMask) {
            Input &= ~inputMask;
        }

        public static void OnJoyPadDown(JoyPad inputMask) {
            Input |= inputMask;
        }
    }
}
