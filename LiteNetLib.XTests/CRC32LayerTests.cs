﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using LiteNetLib.Utils;
using LiteNetLib.Layers;

using Xunit;

namespace LiteNetLib.XTests
{
    public class CRC32LayerTests
    {
        private Crc32cLayer _crc32Layer;
        private IPEndPoint _dummyEndpoint;

        public CRC32LayerTests()
        {
            NetDebug.Logger = null;
            _crc32Layer = new Crc32cLayer();
            _dummyEndpoint = new IPEndPoint(IPAddress.Loopback, 23456);
        }

        [Fact]
        public void ReturnsDataWithoutChecksum()
        {
            byte[] packet = GetTestPacketWithCrc();

            int offset = 0;
            int length = packet.Length;
            _crc32Layer.ProcessInboundPacket(ref _dummyEndpoint, ref packet, ref offset, ref length);

            Assert.Equal(packet.Length - CRC32C.ChecksumSize, length);
        }

        [Fact]
        public void ReturnsNilCountForBadChecksum()
        {
            byte[] packet = GetTestPacketWithCrc();

            //Fake a change to the data to cause data/crc missmatch
            packet[4] = 0;

            int offset = 0;
            int length = packet.Length;
            _crc32Layer.ProcessInboundPacket(ref _dummyEndpoint, ref packet, ref offset, ref length);

            Assert.Equal(0, length);
        }

        [Fact]
        public void ReturnsNilCountForTooShortMessage()
        {
            byte[] packet = new byte[2];

            int offset = 0;
            int length = packet.Length;

            _crc32Layer.ProcessInboundPacket(ref _dummyEndpoint, ref packet, ref offset, ref length);

            Assert.Equal(0, length);
        }

        [Fact]
        public void CanSendAndReceiveSameMessage()
        {
            byte[] message = GetTestMessageBytes();
            //Process outbound adds bytes, so we need a larger array
            byte[] package = new byte[message.Length + CRC32C.ChecksumSize];

            Buffer.BlockCopy(message, 0, package, 0, message.Length);

            int offset = 0;
            int length = message.Length;
            _crc32Layer.ProcessOutBoundPacket(ref _dummyEndpoint, ref package, ref offset, ref length);
            _crc32Layer.ProcessInboundPacket(ref _dummyEndpoint, ref package, ref offset, ref length);
        }

        private static byte[] GetTestPacketWithCrc()
        {
            byte[] testMsg = GetTestMessageBytes();
            uint crc32 = CRC32C.Compute(testMsg, 0, testMsg.Length);

            byte[] packet = new byte[testMsg.Length + CRC32C.ChecksumSize];
            Buffer.BlockCopy(testMsg, 0, packet, 0, testMsg.Length);
            FastBitConverter.GetBytes(packet, testMsg.Length, crc32);
            return packet;
        }

        private static byte[] GetTestMessageBytes()
            => Encoding.ASCII.GetBytes("This is a test string with some length");
    }
}
