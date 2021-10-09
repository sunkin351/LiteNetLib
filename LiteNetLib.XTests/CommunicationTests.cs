using System;
using System.Text;
using System.Threading;
using System.Diagnostics;

using Xunit;

using LiteNetLib.Layers;
using LiteNetLib.Utils;
using System.Net.Sockets;
using System.Collections.Generic;

namespace LiteNetLib.XTests
{
    public class CommunicationTests : IDisposable
    {
        private NetManager _client, _server;
        private EventBasedNetListener _clientListener, _serverListener;

        public CommunicationTests()
        {
            _clientListener = new EventBasedNetListener();
            _serverListener = new EventBasedNetListener();

            _serverListener.ConnectionRequestEvent += _listener_ConnectionRequestEvent;

            _client = CreateNetManager(true);
            _server = CreateNetManager(false);
        }

        private void _listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            request.AcceptIfKey(DefaultAppKey);
        }

        private NetManager CreateNetManager(bool client)
        {
            return new NetManager(client ? _clientListener : _serverListener, new Crc32cLayer());
        }

        public void Dispose()
        {
            if (_client?.IsRunning == true)
                _client.Stop();

            if (_server?.IsRunning == true)
                _server.Stop();
        }

        const int TestTimeout = 4000;
        const string DefaultAppKey = "test_server";
        const string IPv4ConnectionString = "127.0.0.1";
        const string TestTimeoutMessage = "Test Timed Out";

        [Fact]
        public void ConnectionByIpV4()
        {
            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();

                return _server.ConnectedPeersCount > 0 && _client.ConnectedPeersCount > 0;
            });

            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void P2PConnect()
        {
            _clientListener.ConnectionRequestEvent += _listener_ConnectionRequestEvent;

            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);
            _server.Connect(IPv4ConnectionString, _client.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                return _server.ConnectedPeersCount > 0 && _client.ConnectedPeersCount > 0;
            });

            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void ConnectionByIPv4Unsynced()
        {
            _server.UnsyncedEvents = true;
            _client.UnsyncedEvents = true;

            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() => _server.ConnectedPeersCount > 0 && _client.ConnectedPeersCount > 0);

            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void DeliveryTest()
        {
            bool msgDelivered = false;
            bool msgReceived = false;
            const int testSize = 250 * 1024;

            _clientListener.DeliveryEvent += (peer, obj) =>
            {
                Assert.Equal(5, (int)obj);
                msgDelivered = true;
            };

            _clientListener.PeerConnectedEvent += (peer) =>
            {
                int testData = 5;
                byte[] arr = new byte[testSize];
                arr[0] = 196;
                arr[7000] = 32;
                arr[12499] = 200;
                arr[testSize - 1] = 254;
                peer.SendWithDeliveryEvent(arr, 0, DeliveryMethod.ReliableUnordered, testData);
            };

            _serverListener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                Assert.Equal(testSize, reader.UserDataSize);
                Assert.Equal(196, reader.RawData[reader.UserDataOffset]);
                Assert.Equal(32, reader.RawData[reader.UserDataOffset + 7000]);
                Assert.Equal(200, reader.RawData[reader.UserDataOffset + 12499]);
                Assert.Equal(254, reader.RawData[reader.UserDataOffset + testSize - 1]);
                msgReceived = true;
            };

            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                return _server.ConnectedPeersCount > 0 && _client.ConnectedPeersCount > 0 && msgDelivered && msgReceived;
            });

            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void PeerNotFoundTest()
        {
            DisconnectInfo? disconnectInfo = null;

            _clientListener.PeerDisconnectedEvent += (peer, info) => disconnectInfo = info;

            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();

                return _server.ConnectedPeersCount > 0 && _client.ConnectedPeersCount > 0;
            });

            int originalPort = _server.LocalPort;
            _server.Stop();
            _server.Start(originalPort);

            SpinWait.SpinUntil(() => _client.ConnectedPeersCount == 0);

            _client.PollEvents();

            Assert.Equal(0, _server.ConnectedPeersCount);
            Assert.Equal(0, _client.ConnectedPeersCount);
            Assert.True(disconnectInfo.HasValue);
            Assert.Equal(DisconnectReason.RemoteConnectionClose, disconnectInfo.Value.Reason);
        }

        [Fact]
        public void ConnectionFailedTest()
        {
            DisconnectInfo disconnectInfo = default;
            bool eventfired = false;

            _clientListener.PeerDisconnectedEvent += (peer, info) =>
            {
                disconnectInfo = info;
                eventfired = true;
            };

            _client.Start();

            _client.Connect("127.0.0.2", 9050, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _client.PollEvents();

                return eventfired;
            });

            Assert.True(eventfired);
            Assert.Equal(DisconnectReason.ConnectionFailed, disconnectInfo.Reason);
        }

        [Fact]
        public void NetPeerDisconnectTimeout()
        {
            _server.DisconnectTimeout = 1000;

            _server.Start();
            _client.Start();

            var netPeer = _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                return netPeer.ConnectionState == ConnectionState.Connected;
            });

            Assert.Equal(ConnectionState.Connected, netPeer.ConnectionState);
            Assert.Equal(1, _server.ConnectedPeersCount);

            _clientListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Assert.Equal(netPeer, peer);
                Assert.Equal(DisconnectReason.Timeout, info.Reason);
            };

            _server.Stop();

            Assert.Equal(0, _server.ConnectedPeersCount);
            SpinWait.SpinUntil(() => _client.ConnectedPeersCount == 0);
        }

        [Fact]
        public void ReconnectTest()
        {
            int connectCount = 0;
            bool reconnected = false;

            _serverListener.PeerConnectedEvent += (peer) =>
            {
                if (connectCount == 0)
                {
                    byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    for (int i = 0; i < 1000; i++)
                    {
                        peer.Send(data, DeliveryMethod.ReliableOrdered);
                    }
                }
                connectCount += 1;
            };

            _server.Start();

            _client.Start(10123);
            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            while (connectCount < 2)
            {
                if (connectCount == 1 && !reconnected)
                {
                    _client.Stop();
                    Thread.Sleep(500);
                    _client.Start(10123);
                    _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);
                    reconnected = true;
                }
                _client.PollEvents();
                _server.PollEvents();
                Thread.Sleep(15);
            }

            Assert.Equal(2, connectCount);
        }

        [Fact]
        public void RejectTest()
        {
            bool rejectReceived = false;

            _serverListener.ClearConnectionRequestEvent();
            _serverListener.ConnectionRequestEvent += request =>
            {
                request.Reject(Encoding.UTF8.GetBytes("reject_test"));
            };

            _clientListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Assert.Equal(DisconnectReason.ConnectionRejected, info.Reason);
                Assert.Equal("reject_test", Encoding.UTF8.GetString(info.AdditionalData.GetRemainingBytes()));
                rejectReceived = true;
            };

            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                return rejectReceived;
            });

            Assert.Equal(0, _server.ConnectedPeersCount);
            Assert.Equal(0, _client.ConnectedPeersCount);
        }

        [Fact]
        public void RejectForceTest()
        {
            bool rejectReceived = false;

            _serverListener.ClearConnectionRequestEvent();
            _serverListener.ConnectionRequestEvent += request =>
            {
                request.RejectForce(Encoding.UTF8.GetBytes("reject_test"));
            };

            _clientListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Assert.Equal(DisconnectReason.ConnectionRejected, info.Reason);
                Assert.Equal("reject_test", Encoding.UTF8.GetString(info.AdditionalData.GetRemainingBytes()));
                rejectReceived = true;
            };

            _server.Start();
            _client.Start();

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                return rejectReceived;
            });

            Assert.Equal(0, _server.ConnectedPeersCount);
            Assert.Equal(0, _client.ConnectedPeersCount);
        }

        [Fact]
        public void NetPeerDisconnectAll()
        {
            var client2 = CreateNetManager(true);

            _server.Start();

            _client.Start();
            client2.Start();

            var peers = new[]
            {
                _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey),
                client2.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey)
            };

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();
                client2.PollEvents();

                return Array.TrueForAll(peers, peer => peer.ConnectionState == ConnectionState.Connected);
            }, TestTimeout));

            Assert.Equal(2, _server.GetPeersCount(ConnectionState.Connected));

            var data = new byte[] { 1, 2, 3, 4 };

            _clientListener.PeerDisconnectedEvent += (peer, info) =>
            {
                byte[] bytes = info.AdditionalData.GetRemainingBytes();
                Assert.Equal(data, bytes);

                Assert.Contains(peer, peers);

                Assert.Equal(DisconnectReason.RemoteConnectionClose, info.Reason);
            };

            _server.DisconnectAll(data);

            Assert.Equal(0, _server.GetPeersCount(ConnectionState.Connected));

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _client.PollEvents();
                client2.PollEvents();
                _server.PollEvents();

                return Array.TrueForAll(peers, peer => peer.ConnectionState != ConnectionState.Connected);
            }, TestTimeout));

            Thread.Sleep(100);

            Assert.Equal(0, _server.ConnectedPeersCount);
            Assert.All(peers, peer =>
            {
                Assert.Equal(ConnectionState.Disconnected, peer.ConnectionState);
            });
        }

        [Fact]
        public void DisconnectFromServerTest()
        {
            bool clientDisconnect = false, serverDisconnect = false;

            _serverListener.PeerDisconnectedEvent += (peer, info) => serverDisconnect = true;
            _clientListener.PeerDisconnectedEvent += (peer, info) => clientDisconnect = true;

            _server.Start();
            _client.Start();

            var clientPeer = _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                Assert.NotEqual(ConnectionState.Disconnected, clientPeer.ConnectionState);
                Assert.InRange(_server.ConnectedPeersCount, 0, 1);

                return _server.ConnectedPeersCount > 0 && clientPeer.ConnectionState == ConnectionState.Connected;
            }, 1000), TestTimeoutMessage);

            _server.DisconnectPeer(_server.FirstPeer);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _client.PollEvents();
                _server.PollEvents();

                return clientDisconnect && serverDisconnect;
            }, TestTimeout), TestTimeoutMessage);

            Assert.Equal(0, _server.ConnectedPeersCount);
            Assert.Equal(0, _client.ConnectedPeersCount);
        }

        [Fact]
        public void EncryptTest()
        {
            _client = new NetManager(_clientListener, new XorEncryptLayer("secret_key"));
            _server = new NetManager(_serverListener, new XorEncryptLayer("secret_key"));

            Assert.True(_server.Start());
            Assert.True(_client.Start());

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();

                Assert.InRange(_server.ConnectedPeersCount, 0, 1);

                return _server.ConnectedPeersCount > 0;
            });

            Thread.Sleep(200);
            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void ConnectAfterDisconnectWithSamePort()
        {
            Assert.True(_server.Start());
            Assert.True(_client.Start());

            int clientPort = _client.LocalPort;

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                Assert.InRange(_server.ConnectedPeersCount, 0, 1);

                return _server.ConnectedPeersCount > 0;
            }, TestTimeout), TestTimeoutMessage);

            _client.Stop();

            //var connected = false;
            //_clientListener.PeerConnectedEvent += (peer) =>
            //{
            //    connected = true;
            //};

            Assert.True(_client.Start(clientPort));
            var peer = _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                Assert.NotEqual(ConnectionState.Disconnected, peer.ConnectionState);
                _client.PollEvents();
                _server.PollEvents();

                return peer.ConnectionState == ConnectionState.Connected;

            }, TestTimeout), TestTimeoutMessage);

            //Assert.True(SpinWait.SpinUntil(() =>
            //{
            //    _server.PollEvents();
            //    _client.PollEvents();

            //    //  return connected;
            //    return peer.ConnectionState == ConnectionState.Connected;
            //}, TestTimeout));

            //Assert.True(connected);
            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void DisconnectFromClientTest()
        {
            bool clientDisconnect = false, serverDisconnect = false;

            _serverListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Assert.Equal(DisconnectReason.RemoteConnectionClose, info.Reason);
                serverDisconnect = true;
            };
            _clientListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Assert.Equal(DisconnectReason.DisconnectPeerCalled, info.Reason);
                Assert.Equal(0, _client.ConnectedPeersCount);
                clientDisconnect = true;
            };

            Assert.True(_server.Start());
            Assert.True(_client.Start());

            var serverPeer = _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();

                return _server.ConnectedPeersCount > 0;
            }, TestTimeout), TestTimeoutMessage);

            serverPeer.Disconnect();

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();
                _client.PollEvents();

                return serverDisconnect && clientDisconnect;
            }, TestTimeout), TestTimeoutMessage);

            Assert.Equal(0, _server.ConnectedPeersCount);
            Assert.Equal(0, _client.ConnectedPeersCount);
        }

        [Fact]
        public void ChannelsTest()
        {
            const int channelsCount = 64;
            _server.ChannelsCount = channelsCount;
            _client.ChannelsCount = channelsCount;

            NetDataWriter writer = new NetDataWriter();
            var methods = new[]
            {
                DeliveryMethod.Unreliable,
                DeliveryMethod.Sequenced,
                DeliveryMethod.ReliableOrdered,
                DeliveryMethod.ReliableSequenced,
                DeliveryMethod.ReliableUnordered
            };

            int messagesReceived = 0;
            _clientListener.PeerConnectedEvent += (peer) =>
            {
                for (int i = 0; i < channelsCount; i++)
                {
                    foreach (var deliveryMethod in methods)
                    {
                        writer.Reset();
                        writer.Put((byte)deliveryMethod);
                        if (deliveryMethod == DeliveryMethod.ReliableOrdered ||
                            deliveryMethod == DeliveryMethod.ReliableUnordered)
                            writer.Put(new byte[506]);
                        peer.Send(writer, (byte)i, deliveryMethod);
                    }
                }
            };
            _serverListener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                Assert.Equal((DeliveryMethod)reader.GetByte(), method);
                messagesReceived++;
            };

            Assert.True(_server.Start());
            Assert.True(_client.Start());

            _client.Connect(IPv4ConnectionString, _server.LocalPort, DefaultAppKey);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _client.PollEvents();
                _server.PollEvents();

                Assert.InRange(messagesReceived, 0, methods.Length * channelsCount);

                return messagesReceived == methods.Length * channelsCount;
            }, TestTimeout), TestTimeoutMessage);

            Assert.Equal(methods.Length * channelsCount, messagesReceived);
            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void ConnectionByIpV6()
        {
            Assert.True(_server.Start());
            Assert.True(_client.Start());

            _client.Connect("::1", _server.LocalPort, DefaultAppKey);

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.PollEvents();

                Assert.InRange(_client.ConnectedPeersCount, 0, 1);
                Assert.InRange(_server.ConnectedPeersCount, 0, 1);

                return _client.ConnectedPeersCount > 0 && _server.ConnectedPeersCount > 0;
            }, TestTimeout), TestTimeoutMessage);

            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void DiscoveryBroadcastTest()
        {
            var server = _server;

            server.BroadcastReceiveEnabled = true;
            _serverListener.NetworkReceiveUnconnectedEvent += (point, reader, type) =>
            {
                if (type == UnconnectedMessageType.Broadcast)
                {
                    var serverWriter = new NetDataWriter();
                    serverWriter.Put("Server response");
                    server.SendUnconnectedMessage(serverWriter, point);
                }
            };

            Assert.True(server.Start());

            NetManager[] clientList = new NetManager[10];

            for (int i = 0; i < clientList.Length; ++i)
            {
                var cache = i;
                var listener = new EventBasedNetListener();

                listener.NetworkReceiveUnconnectedEvent += (point, reader, type) =>
                {
                    if (point.AddressFamily == AddressFamily.InterNetworkV6)
                        return;
                    Assert.Equal(UnconnectedMessageType.BasicMessage, type);
                    Assert.Equal("Server response", reader.GetString());
                    clientList[cache].Connect(point, DefaultAppKey);
                };

                var manager = clientList[i] = new NetManager(listener, new Crc32cLayer())
                {
                    UnconnectedMessagesEnabled = true
                };

                Assert.True(manager.Start());
            }

            var writer = new NetDataWriter();
            writer.Put("Client request");

            foreach (var client in clientList)
            {
                client.SendBroadcast(writer, server.LocalPort);
            }

            Assert.True(SpinWait.SpinUntil(() =>
            {
                server.PollEvents();

                foreach (var client in clientList)
                {
                    client.PollEvents();
                }

                Assert.InRange(server.ConnectedPeersCount, 0, clientList.Length);

                return server.ConnectedPeersCount == clientList.Length;
            }, TestTimeout), TestTimeoutMessage);

            Assert.All(clientList, client =>
            {
                Assert.Equal(1, client.ConnectedPeersCount);
            });
        }

        [Fact]
        public void ManualMode()
        {
            const int serverPort = 9050;

            _server.StartInManualMode(serverPort);
            _client.Start();

            _client.Connect(IPv4ConnectionString, serverPort, DefaultAppKey);

            var sw = Stopwatch.StartNew();

            Assert.True(SpinWait.SpinUntil(() =>
            {
                _server.ManualUpdate((int)sw.ElapsedMilliseconds);
                sw.Restart();

                _server.PollEvents();

                Assert.InRange(_server.ConnectedPeersCount, 0, 1);
                Assert.InRange(_client.ConnectedPeersCount, 0, 1);

                return _server.ConnectedPeersCount > 0 && _client.ConnectedPeersCount > 0;
            }, TestTimeout), TestTimeoutMessage);

            Assert.Equal(1, _server.ConnectedPeersCount);
            Assert.Equal(1, _client.ConnectedPeersCount);
        }

        [Fact]
        public void SendRawDataToAll()
        {
            var server = _server;

            Assert.True(server.Start());

            NetManager[] clientList = new NetManager[10];

            try
            {
                for (ushort i = 0; i < clientList.Length; i++)
                {
                    var client = clientList[i] = CreateNetManager(true);

                    Assert.True(client.Start());

                    client.Connect(IPv4ConnectionString, server.LocalPort, DefaultAppKey);
                }

                Assert.True(SpinWait.SpinUntil(() =>
                {
                    server.PollEvents();

                    Assert.InRange(server.ConnectedPeersCount, 0, clientList.Length);

                    return server.ConnectedPeersCount == clientList.Length && Array.TrueForAll(clientList, (client) =>
                    {
                        Assert.InRange(client.ConnectedPeersCount, 0, 1);
                        return client.ConnectedPeersCount == 1;
                    });
                }, TestTimeout), TestTimeoutMessage);

                var dataStack = new Stack<byte[]>(clientList.Length);

                _clientListener.NetworkReceiveEvent += (peer, reader, type) => dataStack.Push(reader.GetRemainingBytes());

                var data = Encoding.Default.GetBytes("TextForTest");
                server.SendToAll(data, DeliveryMethod.ReliableUnordered);

                Assert.True(SpinWait.SpinUntil(() =>
                {
                    Array.ForEach(clientList, (client) =>
                    {
                        client.PollEvents();
                    });

                    Assert.InRange(dataStack.Count, 0, clientList.Length);

                    return dataStack.Count == clientList.Length;
                }, TestTimeout), TestTimeoutMessage);

                Assert.Equal(clientList.Length, dataStack.Count);
                Assert.Equal(clientList.Length, server.ConnectedPeersCount);

                Assert.All(clientList, (client) =>
                {
                    Assert.Equal(1, client.ConnectedPeersCount);
                });

                Assert.All(dataStack, (stackData) =>
                {
                    Assert.Equal(data, stackData);
                });
            }
            finally
            {
                Array.ForEach(clientList, client => client?.Stop());
            }
        }
    }
}
