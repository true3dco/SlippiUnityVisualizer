using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbJsharp
{
    public class UbJsonReaderDeserializer
    {
        public object Deserialize(Stream stream)
        {
            return Deserialize(new BinaryReader(stream));
        }
        public object Deserialize(BinaryReader bytes)
        {
            if (!bytes.BaseStream.CanSeek)
                throw new ArgumentException("Streams passed to UBJSON must support seeking in order to read optimized container types.");

            var type = bytes.ReadByte();  // account for having just read the type byte

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
                    return DeserializeAndUpdateContainer(type, bytes);

                default:
                    throw new InvalidDataException(string.Format("Unrecognized UBJSON value type: Pos {0} = '{1}'", bytes.BaseStream.Position, (char) type));
            }
        }

        private object Deserialize(byte type, BinaryReader bytes)
        {
            object val;
            int len;  // for those types that have variable length
            byte implied, current;  // for STC types

            switch (type)
            {
                case UbJsonCommon.NullMarker:
                    return null;
                case UbJsonCommon.TrueMarker:
                    return true;
                case UbJsonCommon.FalseMarker:
                    return false;
                case UbJsonCommon.Int8Marker:
                    val = bytes.ReadSByte();
                    break;
                case UbJsonCommon.UInt8Marker:
                    val = bytes.ReadByte();
                    break;
                case UbJsonCommon.Int16Marker:
                    val = bytes.ReadInt16BE();
                    break;
                case UbJsonCommon.Int32Marker:
                    val = bytes.ReadInt32BE();
                    break;
                case UbJsonCommon.Int64Marker:
                    val = bytes.ReadInt64BE();
                    break;
                case UbJsonCommon.Float32Marker:
                    val = bytes.ReadFloatBE();
                    break;
                case UbJsonCommon.Float64Marker:
                    val = bytes.ReadDoubleBE();
                    break;
                case UbJsonCommon.PrecisionMarker:
                    len = GetLength(bytes);
                    val = Convert.ToDecimal(Encoding.UTF8.GetString(bytes.ReadBytes(len)));
                    break;
                case UbJsonCommon.StringMarker:
                    len = GetLength(bytes);
                    val = Encoding.UTF8.GetString(bytes.ReadBytes(len));
                    break;
                case UbJsonCommon.CharMarker:
                    val = bytes.ReadChar();
                    break;

                case UbJsonCommon.ArrayStartMarker:
                    // TODO: figure out some way to make this and ObjectStartMarker cases into a common method.
                    // First, check for type marker.
                    current = bytes.ReadByte();
                    if (current == UbJsonCommon.StcTypeMarker)
                    {
                        implied = bytes.ReadByte();
                        if (bytes.ReadByte() != UbJsonCommon.StcCountMarker)
                            throw new InvalidDataException($"Standard array type specified without count at pos {bytes.BaseStream.Position}");
                        len = GetLength(bytes);
                    }
                    // If no type, we check for count. We do not increment pos because we are reading the same byte as before.
                    else if (current == UbJsonCommon.StcCountMarker)
                    {
                        len = GetLength(bytes);
                        implied = 0x0;
                    }
                    // Neither type nor count were specified. This can be treated like a normal array.
                    else
                    {
                        bytes.BaseStream.Seek(-1, SeekOrigin.Current);
                        PushCurrentContainer(new List<object>(), null);
                        // updating the final list is done on the closing marker
                        return Deserialize(bytes);
                    }
                    // Now, we handle the optimized array.
                    var list = GetListForImpliedType(implied, len);
                    PushCurrentContainer(list, null);
                    // next X subsequent elements will be of this same implied type
                    for (var i = 0; i < len; i++)
                        // there are some special cases for certain markers
                        if (implied == UbJsonCommon.NoopMarker || implied == UbJsonCommon.NullMarker || implied == UbJsonCommon.TrueMarker || implied == UbJsonCommon.FalseMarker)
                            _list[i] = Deserialize(bytes);
                        else _list[i] = Deserialize(implied, bytes);
                    val = list;
                    PopCurrentContainer();
                    break;

                case UbJsonCommon.ObjectStartMarker:
                    // First, check for container type marker.
                    current = bytes.ReadByte();
                    if (current == UbJsonCommon.StcTypeMarker)
                    {
                        implied = bytes.ReadByte();
                        if (bytes.ReadByte() != UbJsonCommon.StcCountMarker)
                            throw new InvalidDataException($"Standard object type specified without count at pos {bytes.BaseStream.Position}");
                        len = GetLength(bytes);
                    }
                    // If no type, we check for count. We do not increment pos because we are reading the same byte as before.
                    else if (current == UbJsonCommon.StcCountMarker)
                    {
                        len = GetLength(bytes);
                        implied = 0x0;
                    }
                    // Neither type nor count were specified. This can be treated like a normal object.
                    else
                    {
                        bytes.BaseStream.Seek(-1, SeekOrigin.Current);
                        PushCurrentContainer(null, new Dictionary<string, object>());
                        return ReadDictionaryKeyAndValuePair(bytes);
                    }
                    len = GetLength(bytes);
                    implied = bytes.ReadByte();
                    var dict = GetDictionaryForImpliedType(implied, len);
                    PushCurrentContainer(null, dict);
                    // next X subsequent elements will be of this same implied type
                    for (var i = 0; i < len; i++)
                        // FIXME -- have to pass implied type for values!!!
                        ReadDictionaryKeyAndValuePair(bytes);
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

        private object DeserializeAndUpdateContainer(byte type, BinaryReader bytes)
        {
            var val = Deserialize(type, bytes);
            return UpdateCurrentContainer(val, bytes);
        }

        private object ReadDictionaryKeyAndValuePair(BinaryReader bytes)
        {
            // updating the final dictionary is done on the closing marker
            var current = bytes.ReadByte();
            bytes.BaseStream.Seek(-1, SeekOrigin.Current);
            if (current == UbJsonCommon.ObjectEndMarker)
            {
                return Deserialize(bytes);
            }
            // otherwise, next element will be a key and keys are always strings
            return DeserializeAndUpdateContainer(UbJsonCommon.StringMarker, bytes);
        }

        private object UpdateCurrentContainer(object val, BinaryReader bytes)
        {
            if (_list != null)
            {
                if (_list.IsFixedSize) return val;  // stc processes each element on it's own, no autoprocessing the next value

                _list.Add(val);
                // keep deserializing this list
                return Deserialize(bytes);
            }

            if (_dict != null)
            {
                if (_dict.IsFixedSize) return val;  // stc processes each element on it's own, no autoprocessing the next value

                if (_dictKey != null)
                {
                    _dict.Add(_dictKey.ToString(), val);
                    _dictKey = null;

                    return ReadDictionaryKeyAndValuePair(bytes);
                }

                _dictKey = val;

                // keep deserializing this dictionary, no implied type on key
                return Deserialize(bytes);
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

        private int GetLength(BinaryReader bytes)
        {
            long length;  // so we can make sure a valid length was given
            var type = bytes.ReadByte();  // get next type marker to get just value without putting it in current list or dictionary
            var value = Deserialize(type, bytes);

            if (!long.TryParse(value.ToString(), out length))
                throw new InvalidDataException($"UBJSON: expecting number at position {bytes.BaseStream.Position}, didn't get it.");

            // NOTE: this means only byte lengths up to max signed int32 byte length strings are accepted, not max signed int64, which is ostensiably part of the official UBJSON spec!
            //  http://stackoverflow.com/questions/3106945/are-c-sharp-strings-and-other-net-apis-limited-to-2gb-in-size
            if (length > int.MaxValue)
                throw new InvalidDataException(".NET only allows strings that are up to 2gb number of characters, so to be safe, this library maxes out at that number of bytes");

            return (int)length;
        }

        #endregion
    }
}
