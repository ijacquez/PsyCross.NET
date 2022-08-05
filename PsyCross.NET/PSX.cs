using PsyCross.Devices.GPU;
using PsyCross.Devices.Input;

namespace PsyCross {
    public abstract class PSX {
        public VRAM VRAM { get; } = new VRAM(1024, 512);
        public VRAM1555 VRAM1555 { get; } = new VRAM1555(1024, 512);

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
