namespace UbJsharp
{
    public class UbJsonCommon
    {
        // http://ubjson.org/type-reference/
        // http://www.asciitable.com/
        public const byte
            NullMarker = 0x5A, // 'Z'
            NoopMarker = 0x4E, // 'N'
            TrueMarker = 0x54, // 'T'
            FalseMarker = 0x46, // 'F'

            Int8Marker = 0x69, // 'i'
            UInt8Marker = 0x55, // 'U'
            Int16Marker = 0x49, // 'I'
            Int32Marker = 0x6C, // 'l'
            Int64Marker = 0x4C, // 'L'
            Float32Marker = 0x64, // 'd'
            Float64Marker = 0x44, // 'D'

            PrecisionMarker = 0x48, // 'H'
            StringMarker = 0x53, // 'S'
            CharMarker = 0x43, // 'C'

            StcTypeMarker = 0x24, // '#'
            StcCountMarker = 0x23, // '$"
            ArrayStartMarker = 0x5B, // '['
            ArrayEndMarker = 0x5D, // ']'
            ObjectStartMarker = 0x7B, // '{'
            ObjectEndMarker = 0x7D; // '}'

        public const byte
            Int16Size = 2,
            Int32Size = 4,
            Int64Size = 8,
            Float32Size = 4,
            Float64Size = 8,
            BitsInByte = 8;
    }
}
