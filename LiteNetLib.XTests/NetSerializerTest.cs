﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLib.XTests
{
    public class NetSerializerTest
    {
        public NetSerializerTest()
        {
            _samplePacket = new SamplePacket
            {
                SomeFloat = 3.42f,
                SomeIntArray = new[] { 6, 5, 4 },
                SomeString = "Test String",
                SomeVector2 = new SomeVector2(4, 5),
                SomeVectors = new[] { new SomeVector2(1, 2), new SomeVector2(3, 4) },
                SomeEnum = TestEnum.B,
                SomeByteArray = new byte[] { 255, 1, 0 },
                TestObj = new SampleNetSerializable { Value = 5 },
                TestArray = new[] { new SampleNetSerializable { Value = 6 }, new SampleNetSerializable { Value = 15 } },
                SampleClassArray = new[] { new SampleClass { Value = 6 }, new SampleClass { Value = 15 } },
                SampleClassList = new List<SampleClass> { new SampleClass { Value = 1 }, new SampleClass { Value = 5 } },
                VectorList = new List<SomeVector2> { new SomeVector2(-1, -2), new SomeVector2(700, 800) },
                IgnoreMe = 1337
            };

            _packetProcessor = new NetPacketProcessor();
            _packetProcessor.RegisterNestedType<SampleNetSerializable>();
            _packetProcessor.RegisterNestedType(() => new SampleClass());
            _packetProcessor.RegisterNestedType(SomeVector2.Serialize, SomeVector2.Deserialize);
        }

        private SamplePacket _samplePacket;
        private NetPacketProcessor _packetProcessor;

        private struct SomeVector2
        {
            public int X;
            public int Y;

            public SomeVector2(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static void Serialize(NetDataWriter writer, SomeVector2 vector)
            {
                writer.Put(vector.X);
                writer.Put(vector.Y);
            }

            public static SomeVector2 Deserialize(NetDataReader reader)
            {
                var res = new SomeVector2();
                res.X = reader.GetInt();
                res.Y = reader.GetInt();
                return res;
            }
        }

        private struct SampleNetSerializable : INetSerializable
        {
            public int Value;

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Value);
            }

            public void Deserialize(NetDataReader reader)
            {
                Value = reader.GetInt();
            }
        }

        private class SampleClass : INetSerializable
        {
            public int Value;

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Value);
            }

            public void Deserialize(NetDataReader reader)
            {
                Value = reader.GetInt();
            }

            public override bool Equals(object obj)
            {
                return ((SampleClass)obj).Value == Value;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private enum TestEnum
        {
            A = 1,
            B = 7,
            C = 13
        }

        private class SamplePacket
        {
            public string EmptyString { get; set; }
            public float SomeFloat { get; set; }
            public int[] SomeIntArray { get; set; }
            public byte[] SomeByteArray { get; set; }
            public string SomeString { get; set; }
            public SomeVector2 SomeVector2 { get; set; }
            public SomeVector2[] SomeVectors { get; set; }
            public TestEnum SomeEnum { get; set; }
            public SampleNetSerializable TestObj { get; set; }
            public SampleNetSerializable[] TestArray { get; set; }
            public SampleClass[] SampleClassArray { get; set; }
            public List<SampleClass> SampleClassList { get; set; }
            public List<SomeVector2> VectorList { get; set; }
            [IgnoreDataMember]
            public int IgnoreMe { get; set; }
        }

        private static bool AreSame(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            {
                return true;
            }
            return s1 == s2;
        }

        [Fact]
        public void CustomPackageTest()
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, _samplePacket);

            var reader = new NetDataReader(writer);
            SamplePacket readPackage = null;

            _packetProcessor.SubscribeReusable<SamplePacket>(
                packet =>
                {
                    readPackage = packet;
                });

            _packetProcessor.ReadAllPackets(reader);

            Assert.NotNull(readPackage);
            Assert.True(AreSame(_samplePacket.EmptyString, readPackage.EmptyString));
            Assert.Equal(_samplePacket.SomeFloat, readPackage.SomeFloat);
            Assert.Equal(_samplePacket.SomeIntArray, readPackage.SomeIntArray);
            Assert.True(AreSame(_samplePacket.SomeString, readPackage.SomeString));
            Assert.Equal(_samplePacket.SomeVector2, readPackage.SomeVector2);
            Assert.Equal(_samplePacket.SomeVectors, readPackage.SomeVectors);
            Assert.Equal(_samplePacket.SomeEnum, readPackage.SomeEnum);
            Assert.Equal(_samplePacket.TestObj.Value, readPackage.TestObj.Value);
            Assert.Equal(_samplePacket.TestArray, readPackage.TestArray);
            Assert.Equal(_samplePacket.SomeByteArray, readPackage.SomeByteArray);
            Assert.Equal(_samplePacket.SampleClassArray, readPackage.SampleClassArray);
            Assert.Equal(0, readPackage.IgnoreMe); // expect 0 because it should be ignored

            Assert.Equal(_samplePacket.SampleClassList, readPackage.SampleClassList);
            Assert.Equal(_samplePacket.VectorList, readPackage.VectorList);

            //remove test
            _samplePacket.SampleClassList.RemoveAt(0);
            _samplePacket.SampleClassArray = new[] { new SampleClass { Value = 1 } };
            _samplePacket.VectorList.RemoveAt(0);

            writer.Reset();
            _packetProcessor.Write(writer, _samplePacket);
            reader.SetSource(writer);
            _packetProcessor.ReadAllPackets(reader);

            Assert.Equal(_samplePacket.SampleClassArray, readPackage.SampleClassArray);
            Assert.Equal(_samplePacket.SampleClassList, readPackage.SampleClassList);

            //add test
            _samplePacket.SampleClassList.Add(new SampleClass { Value = 152 });
            _samplePacket.SampleClassList.Add(new SampleClass { Value = 154 });
            _samplePacket.SampleClassArray = new[] { new SampleClass { Value = 1 }, new SampleClass { Value = 2 }, new SampleClass { Value = 3 } };
            _samplePacket.VectorList.Add(new SomeVector2(500, 600));

            writer.Reset();
            _packetProcessor.Write(writer, _samplePacket);
            reader.SetSource(writer);
            _packetProcessor.ReadAllPackets(reader);

            Assert.Equal(_samplePacket.SampleClassArray, readPackage.SampleClassArray);
            Assert.Equal(_samplePacket.SampleClassList, readPackage.SampleClassList);
        }
    }
}
