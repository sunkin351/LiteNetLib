using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LiteNetLib.Utils;

using Xunit;

namespace LiteNetLib.XTests
{
    public class ReaderWriterSimpleDataTest
    {
        [Fact]
        public void WriteReadBool()
        {
            var ndw = new NetDataWriter();
            ndw.Put(true);

            var ndr = new NetDataReader(ndw.Data);
            Assert.True(ndr.GetBool());
        }

        [Fact]
        public void WriteReadBoolArray()
        {
            var arr = new[] { true, false, true, false, false };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);

            Assert.Equal(arr, ndr.GetBoolArray());
        }

        [Fact]
        public void WriteReadByte()
        {
            var ndw = new NetDataWriter();
            ndw.Put((byte)8);

            var ndr = new NetDataReader(ndw.Data);
            var readByte = ndr.GetByte();

            Assert.Equal((byte)8, readByte);
        }

        [Fact]
        public void WriteReadByteArray()
        {
            var arr = new byte[] { 1, 2, 4, 8, 16, byte.MaxValue, byte.MinValue };

            var ndw = new NetDataWriter();
            ndw.Put(arr);

            var ndr = new NetDataReader(ndw.Data);
            var readByteArray = new byte[7];
            ndr.GetBytes(readByteArray, 7);

            Assert.Equal(arr, readByteArray);
        }

        [Fact]
        public void WriteReadDouble()
        {
            var ndw = new NetDataWriter();
            ndw.Put(3.1415);

            var ndr = new NetDataReader(ndw.Data);
            var readDouble = ndr.GetDouble();

            Assert.Equal(3.1415, readDouble);
        }

        [Fact]
        public void WriteReadDoubleArray()
        {
            var arr = new[] { 1.1, 2.2, 3.3, 4.4, double.MaxValue, double.MinValue };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);

            Assert.Equal(arr, ndr.GetDoubleArray());
        }

        [Fact]
        public void WriteReadFloat()
        {
            var ndw = new NetDataWriter();
            ndw.Put(3.1415f);

            var ndr = new NetDataReader(ndw.Data);
            var readFloat = ndr.GetFloat();

            Assert.Equal(3.1415f, readFloat);
        }

        [Fact]
        public void WriteReadFloatArray()
        {
            var arr = new[] { 1.1f, 2.2f, 3.3f, 4.4f, float.MaxValue, float.MinValue };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);
            Assert.Equal(arr, ndr.GetFloatArray());
        }

        [Fact]
        public void WriteReadInt()
        {
            var ndw = new NetDataWriter();
            ndw.Put(32);

            var ndr = new NetDataReader(ndw.Data);
            var readInt = ndr.GetInt();

            Assert.Equal(32, readInt);
        }

        [Fact]
        public void WriteReadIntArray()
        {
            var arr = new[] { 1, 2, 3, 4, 5, 6, 7, int.MaxValue, int.MinValue };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);
            Assert.Equal(arr, ndr.GetIntArray());
        }

        [Fact]
        public void WriteReadLong()
        {
            var ndw = new NetDataWriter();
            ndw.Put(64L);

            var ndr = new NetDataReader(ndw.Data);
            var readLong = ndr.GetLong();

            Assert.Equal(64L, readLong);
        }

        [Fact]
        public void WriteReadLongArray()
        {
            var arr = new[] { 1L, 2L, 3L, 4L, long.MaxValue, long.MinValue };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);
            Assert.Equal(arr, ndr.GetLongArray());
        }

        [Fact]
        public void WriteReadNetEndPoint()
        {
            var ndw = new NetDataWriter();
            ndw.Put(NetUtils.MakeEndPoint("127.0.0.1", 7777));

            var ndr = new NetDataReader(ndw.Data);
            var readNetEndPoint = ndr.GetNetEndPoint();

            Assert.Equal(NetUtils.MakeEndPoint("127.0.0.1", 7777), readNetEndPoint);
        }

        [Fact]
        public void WriteReadSByte()
        {
            var ndw = new NetDataWriter();
            ndw.Put((sbyte)8);

            var ndr = new NetDataReader(ndw.Data);
            var readSByte = ndr.GetSByte();

            Assert.Equal((sbyte)8, readSByte);
        }

        [Fact]
        public void WriteReadShort()
        {
            var ndw = new NetDataWriter();
            ndw.Put((short)16);

            var ndr = new NetDataReader(ndw.Data);
            var readShort = ndr.GetShort();

            Assert.Equal(readShort, (short)16);
        }

        [Fact]
        public void WriteReadShortArray()
        {
            var arr = new short[] { 1, 2, 3, 4, 5, 6, short.MaxValue, short.MinValue };
            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);
            Assert.Equal(arr, ndr.GetShortArray());
        }

        [Fact]
        public void WriteReadString()
        {
            var ndw = new NetDataWriter();
            ndw.Put("String", 10);

            var ndr = new NetDataReader(ndw.Data);
            var readString = ndr.GetString(10);

            Assert.Equal("String", readString);
        }

        [Fact]
        public void WriteReadStringArray()
        {
            var ndw = new NetDataWriter();
            ndw.PutArray(new[] { "First", "Second", "Third", "Fourth" });

            var ndr = new NetDataReader(ndw.Data);
            var readStringArray = ndr.GetStringArray(10);

            Assert.Equal(new[] { "First", "Second", "Third", "Fourth" }, readStringArray);
        }

        [Fact]
        public void WriteReadUInt()
        {
            var ndw = new NetDataWriter();
            ndw.Put(34U);

            var ndr = new NetDataReader(ndw.Data);
            var readUInt = ndr.GetUInt();

            Assert.Equal(34U, readUInt);
        }

        [Fact]
        public void WriteReadUIntArray()
        {
            var arr = new[] { 1U, 2U, 3U, 4U, 5U, 6U, uint.MaxValue, uint.MinValue };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);
            Assert.Equal(arr, ndr.GetUIntArray());
        }

        [Fact]
        public void WriteReadULong()
        {
            var ndw = new NetDataWriter();
            ndw.Put(64UL);

            var ndr = new NetDataReader(ndw.Data);
            var readULong = ndr.GetULong();

            Assert.Equal(64UL, readULong);
        }

        [Fact]
        public void WriteReadULongArray()
        {
            var arr = new[] { 1UL, 2UL, 3UL, 4UL, 5UL, ulong.MaxValue, ulong.MinValue };

            var ndw = new NetDataWriter();
            ndw.PutArray(arr);

            var ndr = new NetDataReader(ndw.Data);
            Assert.Equal(arr, ndr.GetULongArray());
        }

        [Fact]
        public void WriteReadUShort()
        {
            var ndw = new NetDataWriter();
            ndw.Put((ushort)16);

            var ndr = new NetDataReader(ndw.Data);
            var readUShort = ndr.GetUShort();

            Assert.Equal((ushort)16, readUShort);
        }
    }
}
