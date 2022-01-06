using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace LiteNetLib.Utils
{
    [DebuggerDisplay("{Length} bytes written")]
    public class NetDataWriter
    {
        protected byte[] _data;
        protected int _position;
        private const int InitialSize = 64;
        private readonly bool _autoResize;

        public int Capacity => _data.Length;
        public byte[] Data => _data;
        public int Length => _position;

        public ReadOnlySpan<byte> DataSpan => _data.AsSpan(0, _position);

        public NetDataWriter() : this(true, InitialSize)
        {
        }

        public NetDataWriter(bool autoResize) : this(autoResize, InitialSize)
        {
        }

        public NetDataWriter(bool autoResize, int initialSize)
        {
            _data = new byte[initialSize];
            _autoResize = autoResize;
        }

        /// <summary>
        /// Creates NetDataWriter from existing ByteArray
        /// </summary>
        /// <param name="bytes">Source byte array</param>
        /// <param name="copy">Copy array to new location or use existing</param>
        public static NetDataWriter FromBytes(byte[] bytes, bool copy)
        {
            if (copy)
            {
                var netDataWriter = new NetDataWriter(true, bytes.Length);
                netDataWriter.Put(bytes);
                return netDataWriter;
            }
            return new NetDataWriter(true, 0) {_data = bytes, _position = bytes.Length};
        }

        /// <summary>
        /// Creates NetDataWriter from existing ByteArray (always copied data)
        /// </summary>
        /// <param name="bytes">Source byte array</param>
        /// <param name="offset">Offset of array</param>
        /// <param name="length">Length of array</param>
        public static NetDataWriter FromBytes(byte[] bytes, int offset, int length)
        {
            var netDataWriter = new NetDataWriter(true, bytes.Length);
            netDataWriter.Put(bytes, offset, length);
            return netDataWriter;
        }

        public static NetDataWriter FromString(string value)
        {
            var netDataWriter = new NetDataWriter();
            netDataWriter.Put(value);
            return netDataWriter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeIfNeed(int newSize)
        {
            if (_data.Length < newSize)
            {
                Array.Resize(ref _data, Math.Max(newSize, _data.Length * 2));
            }
        }

        public void Reset(int size)
        {
            ResizeIfNeed(size);
            _position = 0;
        }

        public void Reset()
        {
            _position = 0;
        }

        public byte[] CopyData()
        {
            byte[] resultData = new byte[_position];
            Buffer.BlockCopy(_data, 0, resultData, 0, _position);
            return resultData;
        }

        /// <summary>
        /// Sets position of NetDataWriter to rewrite previous values
        /// </summary>
        /// <param name="position">new byte position</param>
        /// <returns>previous position of data writer</returns>
        public int SetPosition(int position)
        {
            int prevPosition = _position;
            _position = position;
            return prevPosition;
        }

        public unsafe void Put(float value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 4);

            int bits = *(int*)&value;

            BinaryPrimitives.WriteInt32LittleEndian(_data.AsSpan(_position), bits);
            _position += 4;
        }

        public void Put(double value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 8);

            long bits = BitConverter.DoubleToInt64Bits(value);

            BinaryPrimitives.WriteInt64LittleEndian(_data.AsSpan(_position), bits);
            _position += 8;
        }

        public void Put(long value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 8);
            BinaryPrimitives.WriteInt64LittleEndian(_data.AsSpan(_position), value);
            _position += 8;
        }

        public void Put(ulong value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 8);
            BinaryPrimitives.WriteUInt64LittleEndian(_data.AsSpan(_position), value);
            _position += 8;
        }

        public void Put(int value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 4);
            BinaryPrimitives.WriteInt32LittleEndian(_data.AsSpan(_position), value);
            _position += 4;
        }

        public void Put(uint value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 4);
            BinaryPrimitives.WriteUInt32LittleEndian(_data.AsSpan(_position), value);
            _position += 4;
        }

        public void Put(char value)
        {
            Put((ushort)value);
        }

        public void Put(ushort value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2);
            BinaryPrimitives.WriteUInt16LittleEndian(_data.AsSpan(_position), value);
            _position += 2;
        }

        public void Put(short value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2);
            BinaryPrimitives.WriteInt16LittleEndian(_data.AsSpan(_position), value);
            _position += 2;
        }

        public void Put(sbyte value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 1);
            _data[_position] = unchecked((byte)value);
            _position++;
        }

        public void Put(byte value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 1);
            _data[_position] = value;
            _position++;
        }

        public void Put(byte[] data, int offset, int length)
        {
            Put(data.AsSpan(offset, length));
        }

        public void Put(byte[] data)
        {
            Put(data.AsSpan());
        }

        public void Put(ReadOnlySpan<byte> data)
        {
            if (_autoResize)
                ResizeIfNeed(_position + data.Length);

            data.CopyTo(_data.AsSpan(_position));
            _position += data.Length;
        }

        public void Put(ReadOnlySequence<byte> data)
        {
            if (data.Length > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Sequence data too big");
            }

            if (_autoResize)
                ResizeIfNeed(_position + checked((int)data.Length));

            var dataSpan = _data.AsSpan(_position);

            data.CopyTo(dataSpan);
            _position += (int)data.Length;
        }

        public void PutSBytesWithLength(sbyte[] data, int offset, int length)
        {
            PutSBytesWithLength(data.AsSpan(offset, length));
        }

        public void PutSBytesWithLength(sbyte[] data)
        {
            PutSBytesWithLength(data.AsSpan());
        }

        public void PutSBytesWithLength(ReadOnlySpan<sbyte> data)
        {
            PutBytesWithLength(MemoryMarshal.AsBytes(data));
        }

        public void PutBytesWithLength(byte[] data, int offset, int length)
        {
            PutBytesWithLength(data.AsSpan(offset, length));
        }

        public void PutBytesWithLength(byte[] data)
        {
            PutBytesWithLength(data.AsSpan());
        }

        public void PutBytesWithLength(ReadOnlySpan<byte> data)
        {
            if (_autoResize)
            {
                ResizeIfNeed(_position + sizeof(int) + data.Length);
            }

            var dataSpan = _data.AsSpan(_position);

            BinaryPrimitives.WriteInt32LittleEndian(dataSpan, data.Length);

            data.CopyTo(dataSpan.Slice(sizeof(int)));

            _position += sizeof(int) + data.Length;
        }

        public void Put(bool value)
        {
            Put((byte)(value ? 1 : 0));
        }

        private void PutArray<T>(T[] arr) where T: unmanaged
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException("Big endian platforms are not supported");
            }

            if (arr is null)
            {
                Put((ushort)0);
                return;
            }

            if (arr.Length > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(arr), arr, "Array length was greater than " + ushort.MaxValue);
            }

            ReadOnlySpan<byte> span = MemoryMarshal.AsBytes(arr.AsSpan());

            if (_autoResize)
            {
                ResizeIfNeed(_position + sizeof(ushort) + span.Length);
            }

            Span<byte> dataSpan = _data.AsSpan(_position);

            BinaryPrimitives.WriteUInt16LittleEndian(dataSpan, (ushort)arr.Length);
            span.CopyTo(dataSpan.Slice(sizeof(ushort)));

            _position += sizeof(ushort) + span.Length;
        }

        public void PutArray(float[] value)
        {
            PutArray<float>(value);
        }

        public void PutArray(double[] value)
        {
            PutArray<double>(value);
        }

        public void PutArray(long[] value)
        {
            PutArray<long>(value);
        }

        public void PutArray(ulong[] value)
        {
            PutArray<ulong>(value);
        }

        public void PutArray(int[] value)
        {
            PutArray<int>(value);
        }

        public void PutArray(uint[] value)
        {
            PutArray<uint>(value);
        }

        public void PutArray(ushort[] value)
        {
            PutArray<ushort>(value);
        }

        public void PutArray(short[] value)
        {
            PutArray<short>(value);
        }

        public void PutArray(bool[] value)
        {
            PutArray<bool>(value);
        }

        public void PutArray(string[] value)
        {
            ushort len = value == null ? (ushort)0 : (ushort)value.Length;
            Put(len);
            for (int i = 0; i < len; i++)
                Put(value[i]);
        }

        public void PutArray(string[] value, int maxLength)
        {
            ushort len = value == null ? (ushort)0 : (ushort)value.Length;
            Put(len);
            for (int i = 0; i < len; i++)
                Put(value[i], maxLength);
        }

        public void Put(IPEndPoint endPoint)
        {
            Put(endPoint.Address.ToString());
            Put(endPoint.Port);
        }

        public void Put(string value)
        {
            Put(value.AsSpan());
        }

        public unsafe void Put(string value, int maxLength)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Max Length less than 0");

            ReadOnlySpan<char> valueSpan = value.AsSpan();

            if (valueSpan.Length > maxLength)
            {
                valueSpan = valueSpan.Slice(0, maxLength);
            }

            Put(valueSpan);
        }

        public unsafe void Put(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                Put(0);
                return;
            }

            Span<byte> dataSpan = _data.AsSpan(_position);
            var encoding = Encoding.UTF8;

            int requiredBytesCount;

            if (_autoResize)
            {
                int totalBytesCount = encoding.GetMaxByteCount(value.Length); //gets max length irrespective of actual length

                ResizeIfNeed(_position + sizeof(int) + totalBytesCount);

                fixed (char* pInput = value)
                fixed (byte* pOutput = dataSpan)
                {
                    requiredBytesCount = encoding.GetBytes(pInput, value.Length, pOutput + 4, dataSpan.Length - 4); // convert string to target
                }

                BinaryPrimitives.WriteInt32LittleEndian(dataSpan, requiredBytesCount);
            }
            else
            {
                fixed (char* pInput = value)
                {
                    requiredBytesCount = encoding.GetByteCount(pInput, value.Length);

                    if (_data.Length < _position + sizeof(int) + requiredBytesCount)
                    {
                        throw new InsufficientMemoryException();
                    }

                    BinaryPrimitives.WriteInt32LittleEndian(dataSpan, requiredBytesCount);

                    fixed (byte* pOutput = dataSpan)
                    {
                        encoding.GetBytes(pInput, value.Length, pOutput + sizeof(int), dataSpan.Length - sizeof(int));
                    }
                }
            }

            _position += sizeof(int) + requiredBytesCount;
        }

        /// <summary>
        /// Write a string builder as a normal string
        /// </summary>
        /// <param name="builder"></param>
        public void Put(StringBuilder builder)
        {
            var arr = ArrayPool<char>.Shared.Rent(builder.Length);
            try
            {
                builder.CopyTo(0, arr, 0, builder.Length);

                Put(arr.AsSpan(0, builder.Length));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(arr);
            }
        }

        public void Put<T>(T obj) where T : INetSerializable
        {
            obj.Serialize(this);
        }
    }
}
