using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UbJsharp
{
    public class UbJsonArrayDeserializer : IUbJsonDeserializer
    {
        public object Deserialize(byte[] bytes)
        {
            var pos = 0;
            return Deserialize(bytes, ref pos);
        }

        public object Deserialize(byte[] bytes, ref int pos)
        {
            var type = bytes[pos++];  // account for having just read the type byte

            switch (type)
            {
                case UbJsonCommon.NoopMarker:
                    // the no-op message is purely for the communication protocol and should be handled at that layer, not here
                    throw new InvalidOperationException("No-op has no data representation!");

                case UbJsonCommon.NullMarker:
                case UbJsonCommon.TrueMarker:
                case UbJsonCommon.FalseMarker:
                case UbJsonCommon.Int8Marker:
                case UbJsonCommon.UInt8Marker:
                case UbJsonCommon.Int16Marker:
                case UbJsonCommon.Int32Marker:
                case UbJsonCommon.Int64Marker:
                case UbJsonCommon.Float32Marker:
                case UbJsonCommon.Float64Marker:
                case UbJsonCommon.PrecisionMarker:
                case UbJsonCommon.StringMarker:
                case UbJsonCommon.CharMarker:
                case UbJsonCommon.ArrayStartMarker:
                case UbJsonCommon.ArrayEndMarker:
                case UbJsonCommon.ObjectStartMarker:
                case UbJsonCommon.ObjectEndMarker:
                    return DeserializeAndUpdateContainer(type, bytes, ref pos);

                default:
                    throw new InvalidDataException(string.Format("Unrecognized UBJSON value type: {0} = {1}", pos-1, bytes[pos-1]));
            }
        }

        private object Deserialize(byte type, byte[] bytes, ref int pos)
        {
            object val;
            int len;  // for those types that have variable length
            byte implied;  // for STC types

            switch (type)
            {
                case UbJsonCommon.NullMarker:
                    return null;
                case UbJsonCommon.TrueMarker:
                    return true;
                case UbJsonCommon.FalseMarker:
                    return false;
                case UbJsonCommon.Int8Marker:
                    val = (sbyte)bytes[pos];
                    pos++;
                    break;
                case UbJsonCommon.UInt8Marker:
                    val = (byte)bytes[pos];
                    pos++;
                    break;
                case UbJsonCommon.Int16Marker:
                    val = (short)GetBigEndianNumber(bytes, UbJsonCommon.Int16Size, pos);
                    pos += UbJsonCommon.Int16Size;
                    break;
                case UbJsonCommon.Int32Marker:
                    val = (int)GetBigEndianNumber(bytes, UbJsonCommon.Int32Size, pos);
                    pos += UbJsonCommon.Int32Size;
                    break;
                case UbJsonCommon.Int64Marker:
                    val = (long)GetBigEndianNumber(bytes, UbJsonCommon.Int64Size, pos);
                    pos += UbJsonCommon.Int64Size;
                    break;
                case UbJsonCommon.Float32Marker:
                    val = GetBigEndianFloat(bytes, pos);
                    pos += UbJsonCommon.Float32Size;
                    break;
                case UbJsonCommon.Float64Marker:
                    val = GetBigEndianDouble(bytes, pos);
                    pos += UbJsonCommon.Float64Size;
                    break;
                case UbJsonCommon.PrecisionMarker:
                    len = GetLength(bytes, ref pos);
                    val = Convert.ToDecimal(Encoding.UTF8.GetString(bytes, pos, len));
                    pos += len;
                    break;
                case UbJsonCommon.StringMarker:
                    len = GetLength(bytes, ref pos);
                    val = Encoding.UTF8.GetString(bytes, pos, len);
                    pos += len;
                    break;
                case UbJsonCommon.CharMarker:
                    val = (char)bytes[pos];
                    pos++;
                    break;

                case UbJsonCommon.ArrayStartMarker:
                    // TODO: figure out some way to make this and ObjectStartMarker cases into a common method.
                    // First, check for type marker.
                    if (bytes[pos] == UbJsonCommon.StcTypeMarker)
                    {
                        implied = bytes[++pos];
                        pos++;
                        if (bytes[pos++] != UbJsonCommon.StcCountMarker)
                            throw new InvalidDataException($"Standard array type specified without count at pos {pos - 2}");
                        len = GetLength(bytes, ref pos);
                    }
                    // If no type, we check for count. We do not increment pos because we are reading the same byte as before.
                    else if (bytes[pos] == UbJsonCommon.StcCountMarker)
                    {
                        len = GetLength(bytes, ref pos);
                        implied = 0x0;
                    }
                    // Neither type nor count were specified. This can be treated like a normal array.
                    else
                    {
                        PushCurrentContainer(new List<object>(), null);
                        // updating the final list is done on the closing marker
                        return Deserialize(bytes, ref pos);
                    }
                    // Now, we handle the optimized array.
                    var list = GetListForImpliedType(implied, len);
                    PushCurrentContainer(list, null);
                    // next X subsequent elements will be of this same implied type
                    for (var i = 0; i < len; i++)
                        // there are some special cases for certain markers
                        if (implied == UbJsonCommon.NoopMarker || implied == UbJsonCommon.NullMarker || implied == UbJsonCommon.TrueMarker || implied == UbJsonCommon.FalseMarker)
                            _list[i] = Deserialize(bytes, ref pos);
                        else _list[i] = Deserialize(implied, bytes, ref pos);
                    val = list;
                    PopCurrentContainer();
                    break;

                case UbJsonCommon.ObjectStartMarker:
                    // First, check for type marker.
                    if (bytes[pos] == UbJsonCommon.StcTypeMarker)
                    {
                        implied = bytes[++pos];
                        pos++;
                        if (bytes[pos++] != UbJsonCommon.StcCountMarker)
                            throw new InvalidDataException($"Standard container type specified without count at pos {pos - 2}");
                        len = GetLength(bytes, ref pos);
                    }
                    // If no type, we check for count. We do not increment pos because we are reading the same byte as before.
                    else if (bytes[pos] == UbJsonCommon.StcCountMarker)
                    {
                        len = GetLength(bytes, ref pos);
                        implied = 0x0;
                    }
                    // Neither type nor count were specified. This can be treated like a normal object.
                    else
                    {
                        PushCurrentContainer(null, new Dictionary<string, object>());
                        return ReadDictionaryKeyAndValuePair(bytes, ref pos);
                    }
                    len = GetLength(bytes, ref pos);
                    implied = bytes[pos++];
                    var dict = GetDictionaryForImpliedType(implied, len);
                    PushCurrentContainer(null, dict);
                    // next X subsequent elements will be of this same implied type
                    for (var i = 0; i < len; i++)
                        // FIXME -- have to pass implied type for values!!!
                        ReadDictionaryKeyAndValuePair(bytes, ref pos);
                    val = _dict;
                    PopCurrentContainer();
                    break;
                case UbJsonCommon.ArrayEndMarker:
                    val = _list;
                    PopCurrentContainer();
                    break;
                case UbJsonCommon.ObjectEndMarker:
                    val = _dict;
                    PopCurrentContainer();
                    break;

                default:
                    throw new InvalidOperationException("Getting here means the internal usage of this function is busted");
            }

            return val;
        }

        #region container maintenance

        // currently active container we're adding to
        //  without a generic type on IList/IDictionary, we can allow using value types as well as reference types for STC array/object
        private IList _list;
        private IDictionary _dict;
        private object _dictKey;

        // stack of containers we might be working on underneath
        private readonly Stack<IList> _lists = new Stack<IList>();
        private readonly Stack<IDictionary> _dicts = new Stack<IDictionary>();
        private readonly Stack<object> _dictKeys = new Stack<object>();

        private void PushCurrentContainer(IList list, IDictionary dict)
        {
            _lists.Push(_list);
            _list = list;

            _dicts.Push(_dict);
            _dictKeys.Push(_dictKey);
            _dict = dict;
            _dictKey = null;
        }

        private void PopCurrentContainer()
        {
            _list = _lists.Count > 0 ? _lists.Pop() : null;

            _dict = _dicts.Count > 0 ? _dicts.Pop() : null;
            _dictKey = _dictKeys.Count > 0 ? _dictKeys.Pop() : null;
        }

        private object DeserializeAndUpdateContainer(byte type, byte[] bytes, ref int pos)
        {
            var val = Deserialize(type, bytes, ref pos);
            return UpdateCurrentContainer(val, bytes, ref pos);
        }

        private object ReadDictionaryKeyAndValuePair(byte[] bytes, ref int pos)
        {
            // updating the final dictionary is done on the closing marker
            if (bytes[pos] == UbJsonCommon.ObjectEndMarker) return Deserialize(bytes, ref pos);

            // otherwise, next element will be a key and keys are always strings
            return DeserializeAndUpdateContainer(UbJsonCommon.StringMarker, bytes, ref pos);
        }

        private object UpdateCurrentContainer(object val, byte[] bytes, ref int pos)
        {
            if (_list != null)
            {
                if (_list.IsFixedSize) return val;  // stc processes each element on it's own, no autoprocessing the next value

                _list.Add(val);
                // keep deserializing this list
                return Deserialize(bytes, ref pos);
            }

            if (_dict != null)
            {
                if (_dict.IsFixedSize) return val;  // stc processes each element on it's own, no autoprocessing the next value

                if (_dictKey != null)
                {
                    _dict.Add(_dictKey.ToString(), val);
                    _dictKey = null;

                    return ReadDictionaryKeyAndValuePair(bytes, ref pos);
                }

                _dictKey = val;

                // keep deserializing this dictionary, no implied type on key
                return Deserialize(bytes, ref pos);
            }

            // no list or dictionary being processed, this value is terminal, so send it back already
            return val;
        }

        private static IList GetListForImpliedType(byte type, int len)
        {
            switch (type)
            {
                case UbJsonCommon.NoopMarker:
                case UbJsonCommon.NullMarker:
                    return new List<object>(len);
                case UbJsonCommon.TrueMarker:
                case UbJsonCommon.FalseMarker:
                    return new bool[len];
                case UbJsonCommon.Int8Marker:
                    return new sbyte[len];
                case UbJsonCommon.UInt8Marker:
                    return new byte[len];
                case UbJsonCommon.Int16Marker:
                    return new short[len];
                case UbJsonCommon.Int32Marker:
                    return new int[len];
                case UbJsonCommon.Int64Marker:
                    return new long[len];
                case UbJsonCommon.Float32Marker:
                    return new float[len];
                case UbJsonCommon.Float64Marker:
                    return new double[len];
                case UbJsonCommon.PrecisionMarker:
                    return new decimal[len];
                case UbJsonCommon.StringMarker:
                    return new string[len];
                case UbJsonCommon.CharMarker:
                    return new char[len];
                case UbJsonCommon.ArrayStartMarker:
                case UbJsonCommon.ArrayEndMarker:
                    return new List<IList>(len);
                case UbJsonCommon.ObjectStartMarker:
                case UbJsonCommon.ObjectEndMarker:
                    return new List<IDictionary<string, object>>(len);
                default:
                    throw new InvalidOperationException("Clearly you have found some new kind of UBJSON marker... or something is busted");
            }
        }

        private static IDictionary GetDictionaryForImpliedType(byte type, int len)
        {
            switch (type)
            {
                case UbJsonCommon.NoopMarker:
                case UbJsonCommon.NullMarker:
                    return new Dictionary<string, object>(len);
                case UbJsonCommon.TrueMarker:
                case UbJsonCommon.FalseMarker:
                    return new Dictionary<string, bool>(len);
                case UbJsonCommon.Int8Marker:
                    return new Dictionary<string, sbyte>(len);
                case UbJsonCommon.UInt8Marker:
                    return new Dictionary<string, byte>(len);
                case UbJsonCommon.Int16Marker:
                    return new Dictionary<string, short>(len);
                case UbJsonCommon.Int32Marker:
                    return new Dictionary<string, int>(len);
                case UbJsonCommon.Int64Marker:
                    return new Dictionary<string, long>(len);
                case UbJsonCommon.Float32Marker:
                    return new Dictionary<string, float>(len);
                case UbJsonCommon.Float64Marker:
                    return new Dictionary<string, double>(len);
                case UbJsonCommon.PrecisionMarker:
                    return new Dictionary<string, decimal>(len);
                case UbJsonCommon.StringMarker:
                    return new Dictionary<string, string>(len);
                case UbJsonCommon.CharMarker:
                    return new Dictionary<string, char>(len);
                case UbJsonCommon.ArrayStartMarker:
                case UbJsonCommon.ArrayEndMarker:
                    return new Dictionary<string, IList>(len);
                case UbJsonCommon.ObjectStartMarker:
                case UbJsonCommon.ObjectEndMarker:
                    return new Dictionary<string, IDictionary<string, object>>(len);
                default:
                    throw new InvalidOperationException("Clearly you have found some new kind of UBJSON marker... or something is busted");
            }
        }

        #endregion

        #region processing data values

        private int GetLength(byte[] bytes, ref int pos)
        {
            long length;  // so we can make sure a valid length was given
            var type = bytes[pos++];  // get next type marker to get just value without putting it in current list or dictionary
            var value = Deserialize(type, bytes, ref pos);

            if (!long.TryParse(value.ToString(), out length))
                throw new InvalidDataException($"UBJSON: expecting number at position {pos}, didn't get it.");

            // NOTE: this means only byte lengths up to max signed int32 byte length strings are accepted, not max signed int64, which is ostensiably part of the official UBJSON spec!
            //  http://stackoverflow.com/questions/3106945/are-c-sharp-strings-and-other-net-apis-limited-to-2gb-in-size
            if (length > int.MaxValue)
                throw new InvalidDataException(".NET only allows strings that are up to 2gb number of characters, so to be safe, this library maxes out at that number of bytes");

            return (int)length;
        }

        // TODO -- run time tests to figure out which is faster: BitConverter.ToInt* plus Array.Reverse or manually byte manipulating?
        private static long GetBigEndianNumber(byte[] bytes, byte len, int start)
        {
            var num = 0L;
            for (var i = 0; i < len; i++)
                num += (long)bytes[start + i] << (UbJsonCommon.BitsInByte * (len - i - 1));
            return num;
        }

        private static float GetBigEndianFloat(byte[] bytes, int pos)
        {
            var value = BitConverter.ToSingle(bytes, pos);

            if (BitConverter.IsLittleEndian)
            {
                var b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                return BitConverter.ToSingle(b, 0);
            }

            return value;
        }

        private static double GetBigEndianDouble(byte[] bytes, int pos)
        {
            var value = BitConverter.ToDouble(bytes, pos);

            if (BitConverter.IsLittleEndian)
            {
                var b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                return BitConverter.ToDouble(b, 0);
            }

            return value;
        }

        #endregion
    }
}