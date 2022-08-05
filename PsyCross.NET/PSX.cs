using ProjectPSX.Devices;
using PsyCross.Devices.Input;

namespace PsyCross {
    public abstract class PSX {
        public Gpu Gpu { get; } = new Gpu();

        public GamepadInputsEnum GamepadInputs { get; private set; }

        public abstract void UpdateFrame();

        public void JoyPadUp(GamepadInputsEnum gamepadInputs) {
            GamepadInputs = gamepadInputs;
        }

        public void JoyPadDown(GamepadInputsEnum gamepadInputs) {
            GamepadInputs = gamepadInputs;
        }
    }
}
