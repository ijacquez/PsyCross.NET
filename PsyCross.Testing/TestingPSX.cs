namespace PsyCross.Testing {
    public class TestingPSX : PSX {
        int x = 0;
        int y = 0;
        public override void UpdateFrame() {
            System.Console.WriteLine($"Hello: {GamepadInputs}, ({x},{y})");

            VRAM.SetPixel(x, y, 0xFFFFFFFF);
            VRAM.SetPixel(x, y, 0xFFFF);

            if (x < 1023) {
                x++;
            }
            if (y < 511) {
                y++;
            }
        }
    }
}
