using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;

namespace LiteNetLib.Utils
{
    public class NetDataReader
    {
        protected byte[] _data;
        protected int _position;
        protected int _dataSize;
        private int _offset;

        public byte[] RawData => _data;
        public int RawDataSize => _dataSize;
        public int UserDataOffset => _offset;
        public int UserDataSize => _dataSize - _offset;
        public bool IsNull => _data == null;
        public int Position => _position;
        public bool EndOfData => _position == _dataSize;
        public int AvailableBytes => _dataSize - _position;

        public void SkipBytes(int count)
        {
            _position += count;
        }

        public void SetSource(NetDataWriter dataWriter)
        {
            _data = dataWriter.Data;
            _position = 0;
            _offset = 0;
            _dataSize = dataWriter.Length;
        }

        public void SetSource(byte[] source)
        {
            _data = source;
            _position = 0;
            _offset = 0;
            _dataSize = source.Length;
        }

        public void SetSource(byte[] source, int offset, int maxSize)
        {
            _data = source;
            _position = offset;
            _offset = offset;
            _dataSize = maxSize;
        }

        public NetDataReader()
        {

        }

        public NetDataReader(NetDataWriter writer)
        {
            SetSource(writer);
        }

        public NetDataReader(byte[] source)
        {
            SetSource(source);
        }

        public NetDataReader(byte[] source, int offset, int maxSize)
        {
            SetSource(source, offset, maxSize);
        }

        protected ReadOnlySpan<byte> UnreadData
        {
            get => _data.AsSpan(_position, _dataSize - _position);
        }

        protected void Advance(int length)
        {
            _position += length;
        }

        #region GetMethods
        public IPEndPoint GetNetEndPoint()
        {
            string host = GetString(1000);
            int port = GetInt();
            return NetUtils.MakeEndPoint(host, port);
        }

        public byte GetByte()
        {
            byte res = _data[_position];
            _position += 1;
            return res;
        }

        public sbyte GetSByte()
        {
            var b = (sbyte)_data[_position];
            _position++;
            return b;
        }

        private unsafe T[] GetArray<T>() where T: unmanaged
        {
            EnsureEndianess();

            var dataSpan = UnreadData;

            ushort size = BinaryPrimitives.ReadUInt16LittleEndian(dataSpan);

            var arrSpan = MemoryMarshal.Cast<byte, T>(dataSpan.Slice(sizeof(ushort), size * sizeof(T)));

            Advance(sizeof(ushort) + size * sizeof(T));

            return arrSpan.ToArray();
        }

        public bool[] GetBoolArray()
        {
            return GetArray<bool>();
        }

        public ushort[] GetUShortArray()
        {
            return GetArray<ushort>();
        }

        public short[] GetShortArray()
        {
            return GetArray<short>();
        }

        public long[] GetLongArray()
        {
            return GetArray<long>();
        }

        public ulong[] GetULongArray()
        {
            return GetArray<ulong>();
        }

        public int[] GetIntArray()
        {
            return GetArray<int>();
        }

        public uint[] GetUIntArray()
        {
            return GetArray<uint>();
        }

        public float[] GetFloatArray()
        {
            return GetArray<float>();
        }

        public double[] GetDoubleArray()
        {
            return GetArray<double>();
        }

        public string[] GetStringArray()
        {
            ushort len = GetUShort();

            var arr = new string[len];

            for (int i = 0; i < len; i++)
            {
                arr[i] = GetString();
            }

            return arr;
        }

        public string[] GetStringArray(int maxStringLength)
        {
            ushort len = GetUShort();

            var arr = new string[len];

            for (int i = 0; i < len; i++)
            {
                arr[i] = GetString(maxStringLength);
            }

            return arr;
        }

        public bool GetBool()
        {
            bool res = _data[_position] != 0;
            _position += 1;
            return res;
        }

        public char GetChar()
        {
            return (char)GetUShort();
        }

        public ushort GetUShort()
        {
            ushort result = BinaryPrimitives.ReadUInt16LittleEndian(UnreadData);
            _position += 2;
            return result;
        }

        public short GetShort()
        {
            short result = BinaryPrimitives.ReadInt16LittleEndian(UnreadData);
            _position += 2;
            return result;
        }

        public long GetLong()
        {
            long result = BinaryPrimitives.ReadInt64LittleEndian(UnreadData);
            _position += 8;
            return result;
        }

        public ulong GetULong()
        {
            ulong result = BinaryPrimitives.ReadUInt64LittleEndian(UnreadData);
            _position += 8;
            return result;
        }

        public int GetInt()
        {
            int result = BinaryPrimitives.ReadInt32LittleEndian(UnreadData);
            _position += 4;
            return result;
        }

        public uint GetUInt()
        {
            uint result = BinaryPrimitives.ReadUInt32LittleEndian(UnreadData);
            _position += 4;
            return result;
        }

        public unsafe float GetFloat()
        {
            int tmp = BinaryPrimitives.ReadInt32LittleEndian(UnreadData);

            _position += 4;
            return *(float*)&tmp;
        }

        public double GetDouble()
        {
            double result = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(UnreadData));
            _position += 8;
            return result;
        }

        public string GetString(int maxLength)
        {
            int bytesCount = GetInt();
            if (bytesCount <= 0 || bytesCount > maxLength * 2)
            {
                return string.Empty;
            }

            int charCount = Encoding.UTF8.GetCharCount(_data, _position, bytesCount);
            if (charCount > maxLength)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_data, _position, bytesCount);
            _position += bytesCount;
            return result;
        }

        public string GetString()
        {
            int bytesCount = GetInt();
            if (bytesCount <= 0)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_data, _position, bytesCount);
            _position += bytesCount;
            return result;
        }

        public ArraySegment<byte> GetRemainingBytesSegment()
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_data, _position, AvailableBytes);
            _position = _data.Length;
            return segment;
        }

        public T Get<T>() where T : INetSerializable, new()
        {
            var obj = new T();
            obj.Deserialize(this);
            return obj;
        }

        public byte[] GetRemainingBytes()
        {
            byte[] outgoingData = UnreadData.ToArray();
            _position = _data.Length;
            return outgoingData;
        }

        public void GetBytes(byte[] destination, int start, int count)
        {
            GetBytes(destination.AsSpan(start, count));
        }

        public void GetBytes(byte[] destination, int count)
        {
            GetBytes(destination.AsSpan(0, count));
        }

        public void GetBytes(Span<byte> span)
        {
            _data.AsSpan(_position, span.Length).CopyTo(span);
            _position += span.Length;
        }

        public sbyte[] GetSBytesWithLength()
        {
            int length = GetInt();
            sbyte[] outgoingData = new sbyte[length];

            GetBytes(MemoryMarshal.AsBytes((Span<sbyte>)outgoingData));

            return outgoingData;
        }

        public byte[] GetBytesWithLength()
        {
            int length = GetInt();
            byte[] outgoingData = new byte[length];

            GetBytes(outgoingData);

            return outgoingData;
        }

        public ReadOnlySpan<byte> GetBytesWithLengthAsSpan()
        {
            int length = GetInt();

            var span = _data.AsSpan(_position, length);

            Advance(length);

            return span;
        }

        public ReadOnlySpan<sbyte> GetSBytesWithLengthAsSpan()
        {
            int length = GetInt();

            var span = _data.AsSpan(_position, length);

            Advance(length);

            return MemoryMarshal.Cast<byte, sbyte>(span);
        }
        #endregion

        #region PeekMethods

        public byte PeekByte()
        {
            return _data[_position];
        }

        public sbyte PeekSByte()
        {
            return (sbyte)_data[_position];
        }

        public bool PeekBool()
        {
            return _data[_position] != 0;
        }

        public char PeekChar()
        {
            return (char)BinaryPrimitives.ReadUInt16BigEndian(UnreadData);
        }

        public ushort PeekUShort()
        {
            return BinaryPrimitives.ReadUInt16BigEndian(UnreadData);
        }

        public short PeekShort()
        {
            return BinaryPrimitives.ReadInt16LittleEndian(UnreadData);
        }

        public long PeekLong()
        {
            return BinaryPrimitives.ReadInt64LittleEndian(UnreadData);
        }

        public ulong PeekULong()
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(UnreadData);
        }

        public int PeekInt()
        {
            return BinaryPrimitives.ReadInt32LittleEndian(UnreadData);
        }

        public uint PeekUInt()
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(UnreadData);
        }

        public unsafe float PeekFloat()
        {
            int tmp = BinaryPrimitives.ReadInt32LittleEndian(UnreadData);
            return *(float*)&tmp;
        }

        public double PeekDouble()
        {
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(UnreadData));
        }

        public string PeekString(int maxLength)
        {
            int bytesCount = BitConverter.ToInt32(_data, _position);
            if (bytesCount <= 0 || bytesCount > maxLength * 2)
            {
                return string.Empty;
            }

            int charCount = Encoding.UTF8.GetCharCount(_data, _position + 4, bytesCount);
            if (charCount > maxLength)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_data, _position + 4, bytesCount);
            return result;
        }

        public string PeekString()
        {
            int bytesCount = BitConverter.ToInt32(_data, _position);
            if (bytesCount <= 0)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_data, _position + 4, bytesCount);
            return result;
        }
        #endregion

        #region TryGetMethods
        public bool TryGetByte(out byte result)
        {
            if (AvailableBytes >= 1)
            {
                result = GetByte();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetSByte(out sbyte result)
        {
            if (AvailableBytes >= 1)
            {
                result = GetSByte();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetBool(out bool result)
        {
            if (AvailableBytes >= 1)
            {
                result = GetBool();
                return true;
            }
            result = false;
            return false;
        }

        public bool TryGetChar(out char result)
        {
            if (AvailableBytes >= 2)
            {
                result = GetChar();
                return true;
            }
            result = '\0';
            return false;
        }

        public bool TryGetShort(out short result)
        {
            if (AvailableBytes >= 2)
            {
                result = GetShort();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetUShort(out ushort result)
        {
            if (AvailableBytes >= 2)
            {
                result = GetUShort();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetInt(out int result)
        {
            if (AvailableBytes >= 4)
            {
                result = GetInt();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetUInt(out uint result)
        {
            if (AvailableBytes >= 4)
            {
                result = GetUInt();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetLong(out long result)
        {
            if (AvailableBytes >= 8)
            {
                result = GetLong();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetULong(out ulong result)
        {
            if (AvailableBytes >= 8)
            {
                result = GetULong();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetFloat(out float result)
        {
            if (AvailableBytes >= 4)
            {
                result = GetFloat();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetDouble(out double result)
        {
            if (AvailableBytes >= 8)
            {
                result = GetDouble();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryGetString(out string result)
        {
            if (AvailableBytes >= 4)
            {
                var bytesCount = PeekInt();
                if (AvailableBytes >= bytesCount + 4)
                {
                    result = GetString();
                    return true;
                }
            }
            result = null;
            return false;
        }

        public bool TryGetStringArray(out string[] result)
        {
            ushort size;
            if (!TryGetUShort(out size))
            {
                result = null;
                return false;
            }

            result = new string[size];
            for (int i = 0; i < size; i++)
            {
                if (!TryGetString(out result[i]))
                {
                    result = null;
                    return false;
                }
            }

            return true;
        }

        public bool TryGetBytesWithLength(out byte[] result)
        {
            if (AvailableBytes >= 4)
            {
                var length = PeekInt();
                if (length >= 0 && AvailableBytes >= length + 4)
                {
                    result = GetBytesWithLength();
                    return true;
                }
            }
            result = null;
            return false;
        }
        #endregion

        public void Clear()
        {
            _position = 0;
            _dataSize = 0;
            _data = null;
        }

        private static void EnsureEndianess()
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
