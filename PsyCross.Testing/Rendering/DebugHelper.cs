using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    // XXX: Wrap around conditional debug compilation
    public static class DebugHelper {
        private const int _ColorTableCount = 4096;

        private static System.Random _Random = new System.Random();
        private static Rgb888[] _ColorTable = new Rgb888[_ColorTableCount];

        static DebugHelper() {
            for (int i = 0; i < _ColorTable.Length; i++) {
                _ColorTable[i] = new Rgb888((byte)(_Random.NextDouble() * 255),
                                            (byte)(_Random.NextDouble() * 255),
                                            (byte)(_Random.NextDouble() * 255));
            }
        }

        private static Rgb888 GetRandomColor() =>
            _ColorTable[System.Math.Abs(_Random.Next()) & (_ColorTableCount - 1)];

        private static Rgb888 GetColor(int index) =>
            _ColorTable[System.Math.Abs(index) & (_ColorTableCount - 1)];
    }
}
