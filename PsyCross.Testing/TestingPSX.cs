namespace PsyCross.Testing {
    public class TestingPSX : PSX {
        uint x = 0;
        uint y = 0;

        public override void UpdateFrame() {
            SetupFakeEnv();
            SetDispMask(1);
            FillRectVram(0x555555, 0, 0, 319, 239);
            FillRectVram(0xFFFFFF, 0, 0, 64, 64);
            CopyRectVram(0, 0, 100, 100, 64, 64);
            FillTri(0xFF_00_00, x, 24, x + 32, 0, x + 64, 24);
            x++;
        }

        // GPU Memory Transfer Commands (GP0): $02 - Fill Rectangle In VRAM
        private void FillRectVram(uint color, uint x, uint y, uint width, uint height) {
            Gpu.WriteGP0((0x02 << 24) | (color & 0xFF_FF_FF));
            Gpu.WriteGP0((y << 16) + (x & 0xFFFF));
            Gpu.WriteGP0((height << 16) + (width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $80 - Copy Rectangle (VRAM To VRAM)
        private void CopyRectVram(uint x1, uint y1, uint x2, uint y2, uint width, uint height) {
            Gpu.WriteGP0((uint)0x80 << 24);
            Gpu.WriteGP0((y1 << 16) + (x1 & 0xFFFF));
            Gpu.WriteGP0((y2 << 16) + (x2 & 0xFFFF));
            Gpu.WriteGP0((height << 16) + (width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $A0 - Copy Rectangle (CPU To VRAM)
        private void CopyRectCpu(uint x, uint y, uint width, uint height) {
            Gpu.WriteGP0((uint)0xA0 << 24);
            Gpu.WriteGP0((y << 16) + (x & 0xFFFF));
            Gpu.WriteGP0((height << 16) + (width & 0xFFFF));
        }

        private void FillTri(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
            Gpu.WriteGP0((0x20 << 24) | (color & 0xFF_FF_FF));
            Gpu.WriteGP0((y1 << 16) + (x1 & 0xFFFF));
            Gpu.WriteGP0((y2 << 16) + (x2 & 0xFFFF));
            Gpu.WriteGP0((y3 << 16) + (x3 & 0xFFFF));
        }

        private void SetupFakeEnv() {
            Gpu.WriteGP0(0xE1_000400);
            Gpu.WriteGP0(0xE3_000000); // Set Drawing Area Top Left X1=0, Y1=0
            Gpu.WriteGP0(0xE4_03BD3F); // Set Drawing Area Bottom Right X2=319, Y2=239
            Gpu.WriteGP0(0xE5_000000);
        }

        private void ResetGpu() { // What is the name of the PsyQ function?
            Gpu.WriteGP1(0x00000000);
        }

        private void SetDispMask(uint v) {
            Gpu.WriteGP1(0x03000001 - (v & 0x1));
        }
    }
}
