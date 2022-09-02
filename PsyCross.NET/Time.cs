namespace PsyCross {
    public class Time {
        public float ElapsedTime { get; private set; }

        public float DeltaTime { get; private set; }

        internal Time() {
        }

        internal void UpdateTime(double time) {
            DeltaTime = (float)time;
            ElapsedTime += DeltaTime;
        }
    }
}
