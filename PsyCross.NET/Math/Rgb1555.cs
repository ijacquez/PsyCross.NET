using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Rgb1555 {
        [FieldOffset(0)] public ushort Value;

        public Rgb1555(byte r, byte g, byte b, bool msb) {
            Value = 0;
            R = r;
            G = g;
            B = b;
            Msb = msb;
        }

        public Rgb1555(byte r, byte g, byte b) : this(r, g, b, true) {
        }

        public ushort R {
            get => (ushort)(Value & 0x1F);
            set => Value = (ushort)((Value & 0xFFE0) | (value & 0x1F));
        }

        public ushort G {
            get => (ushort)((Value >> 5) & 0x1F);
            set => Value = (ushort)((Value & 0xFC1F) | ((value & 0x1F) << 5));
        }

        public ushort B {
            get => (ushort)((Value >> 10) & 0x1F);
            set => Value = (ushort)((Value & 0x83FF) | ((value & 0x1F) << 10));
        }

        public bool Msb {
            get => (Value & 0x8000) != 0x8000;
            set => Value = (ushort)((Value & 0x7FFF) | ((value) ? 0x8000 : 0x0000));
        }

        public override string ToString() =>
            $"#{R:X02}{G:X02}{B:X02}";
    }
}
