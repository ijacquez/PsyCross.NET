namespace PsyCross.Testing {
    public class TestingPSX : PSX {
        public override void UpdateFrame() {
            System.Console.WriteLine($"Hello: {GamepadInputs}");
        }
    }
}
