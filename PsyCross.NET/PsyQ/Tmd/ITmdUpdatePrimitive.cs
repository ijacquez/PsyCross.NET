namespace PsyCross {
    public static partial class PsyQ {
        internal interface ITmdUpdatePrimitive : ITmdPrimitive {
            new int IndexV0 { get; set; }
            new int IndexV1 { get; set; }
            new int IndexV2 { get; set; }
            new int IndexV3 { get; set; }

            new int IndexN0 { get; set; }
            new int IndexN1 { get; set; }
            new int IndexN2 { get; set; }
            new int IndexN3 { get; set; }
        }
    }
}
