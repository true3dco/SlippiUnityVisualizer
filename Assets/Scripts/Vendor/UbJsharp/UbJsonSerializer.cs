using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UbJsharp
{
    public class UbJsonSerializer : IUbJsonSerializer
    {
        private static readonly byte[]
            NullBytes = GetBytes(UbJsonCommon.NullMarker),
            TrueBytes = GetBytes(UbJsonCommon.TrueMarker),
            FalseBytes = GetBytes(UbJsonCommon.FalseMarker),
            NoopBytes = GetBytes(UbJsonCommon.NoopMarker);

        private static byte[] GetBytes(byte marker, params byte[] data)
        {
            var bytes = new byte[data.Length + 1];
            bytes[0] = marker;
            for (var i = 0; i < data.Length; i++)
                bytes[i + 1] = data[i];
            return bytes;
        }

        private static byte[] GetBigEndianBytes (byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        // TODO -- run time tests to figure out which is faster: BitConverter.GetBytes plus Array.Reverse or manually byte manipulating?
        private static byte[] GetBigEndianBytes (long num, byte len)
        {
            var bytes = new byte[len];
            for (var i = 0; i < len; i++)
                bytes[i] = (byte)(num >> (UbJsonCommon.BitsInByte * (len - i - 1)));
            return bytes;
        }

        public byte[] Serialize(object o)
        {
            // explicit UBJSON value types
            if (o is bool) return Serialize((bool)o);
            // byte is unsigned, make sure we use the signed byte (int8) type!
            if (o is sbyte) return Serialize((sbyte)o);
            if (o is byte) return Serialize((byte)o);
            if (o is short) return Serialize((short)o);
            if (o is int) return Serialize((int)o);
            if (o is long) return Serialize((long)o);
            if (o is float) return Serialize((float)o);
            if (o is double) return Serialize((double)o);
            if (o is char) return Serialize((char)o);

            // language value types not directly support by UBJSON
            if (o is ushort) return Serialize((ushort)o);
            if (o is uint) return Serialize((uint)o);
            if (o is decimal) return Serialize((decimal)o);
            if (o is ulong) return Serialize((ulong)o);

            // nullable value types
            var s = o as string;
            if (s != null) return Serialize(s);

            var d = o as IDictionary;
            if (d != null) return Serialize(d);

            var a = o as IEnumerable;
            if (a != null) return Serialize(a);

            return NullBytes;
        }

        public byte[] SerializeNoop ()
        {
            return NoopBytes;
        }

        public byte[] SerializePrecision (string o)
        {
            return SerializeString(UbJsonCommon.PrecisionMarker, o);
        }

        private static byte[] Serialize(bool o)
        {
            return o ? TrueBytes : FalseBytes;
        }

        private static byte[] Serialize (sbyte o)
        {
            return GetBytes(UbJsonCommon.Int8Marker, (byte)o);
        }

        private static byte[] Serialize (byte o)
        {
            return GetBytes(UbJsonCommon.UInt8Marker, o);
        }

        private static byte[] Serialize(short o)
        {
            return o <= sbyte.MaxValue ? Serialize((sbyte)o)
                : GetBytes(UbJsonCommon.Int16Marker, GetBigEndianBytes(o, 2));
        }

        private static byte[] Serialize(int o)
        {
            return o <= short.MaxValue ? Serialize((short)o)
                : GetBytes(UbJsonCommon.Int32Marker, GetBigEndianBytes(o, 4));
        }

        private static byte[] Serialize(long o)
        {
            return o <= int.MaxValue ? Serialize((int)o)
                : GetBytes(UbJsonCommon.Int64Marker, GetBigEndianBytes(o, 8));
        }

        private static byte[] Serialize (ulong o)
        {
            return o <= long.MaxValue ? Serialize((long)o)
                : SerializeString(UbJsonCommon.PrecisionMarker, o.ToString());
        }

        private static byte[] Serialize (decimal o)
        {
            return SerializeString(UbJsonCommon.PrecisionMarker, o.ToString());
        }

        private static byte[] Serialize(float o)
        {
            return GetBytes(UbJsonCommon.Float32Marker, GetBigEndianBytes(BitConverter.GetBytes(o)));
        }

        private static byte[] Serialize(double o)
        {
            return GetBytes(UbJsonCommon.Float64Marker, GetBigEndianBytes(BitConverter.GetBytes(o)));
        }

        private static byte[] Serialize(char o)
        {
            return o > sbyte.MaxValue ? Serialize((short)o)
                : GetBytes(UbJsonCommon.CharMarker, (byte)o);
        }

        private static byte[] Serialize(string o)
        {
            return SerializeString(UbJsonCommon.StringMarker, o);
        }

        private static byte[] GetLengthBytes(int length)
        {
            return length <= sbyte.MaxValue ? Serialize((sbyte)length)
                : length <= short.MaxValue ? Serialize((short)length)
                : Serialize(length);
        }

        private static byte[] SerializeString(byte marker, string o)
        {
            var lengthBytes = GetLengthBytes(o.Length);
            // http://stackoverflow.com/questions/472906/net-string-to-byte-array-c-sharp
            // http://stackoverflow.com/questions/3833693/isnt-on-big-endian-machines-utf-8s-byte-order-different-than-on-little-endian
            var dataBytes = Encoding.UTF8.GetBytes(o);
            var bytes = new byte[1 + lengthBytes.Length + dataBytes.Length];
            bytes[0] = marker;
            Buffer.BlockCopy(lengthBytes, 0, bytes, 1, lengthBytes.Length);
            Buffer.BlockCopy(dataBytes, 0, bytes, 1 + lengthBytes.Length, dataBytes.Length);
            return bytes;
        }

        // can't use IEnumerable<object> because <object> does not allow for value types, e.g. List<int>
        private byte[] Serialize(IEnumerable o)
        {
            var bytes = new List<byte> { UbJsonCommon.ArrayStartMarker };
            foreach (var v in o)
                bytes.AddRange(Serialize(v));
            bytes.Add(UbJsonCommon.ArrayEndMarker);
            return bytes.ToArray();
        }

        private byte[] Serialize(IDictionary o)
        {
            var bytes = new List<byte> { UbJsonCommon.ObjectStartMarker };
            foreach (DictionaryEntry kv in o)
            {
                bytes.AddRange(Serialize(kv.Key));
                bytes.AddRange(Serialize(kv.Value));
            }
            bytes.Add(UbJsonCommon.ObjectEndMarker);
            return bytes.ToArray();
        }
    }
}
