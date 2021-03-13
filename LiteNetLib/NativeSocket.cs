﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace LiteNetLib
{
    internal readonly struct NativeAddr
    {
        //common parts
        public readonly long Part1; //family, port, etc
        public readonly long Part2;
        //ipv6 parts
        public readonly long Part3;
        public readonly int Part4;

        private readonly int _hash;

        public NativeAddr(byte[] address, int len)
        {
            Part1 = BitConverter.ToInt64(address, 0);
            Part2 = BitConverter.ToInt64(address, 8);
            if (len > 16)
            {
                Part3 = BitConverter.ToInt64(address, 16);
                Part4 = BitConverter.ToInt32(address, 24);
            }
            else
            {
                Part3 = 0;
                Part4 = 0;
            }
            _hash = (int)(Part1 >> 32) ^ (int)Part1 ^
                    (int)(Part2 >> 32) ^ (int)Part2 ^
                    (int)(Part3 >> 32) ^ (int)Part3 ^
                    Part4;
        }

        public override int GetHashCode()
        {
            return _hash;
        }
    }

    internal class NativeEndPoint : IPEndPoint
    {
        public readonly byte[] NativeAddress;

        public NativeEndPoint(byte[] address) : base(IPAddress.Any, 0)
        {
            NativeAddress = address;
            short family = BitConverter.ToInt16(address, 0);
            Port = (ushort)((address[2] << 8) | address[3]);

            if ((NativeSocket.UnixMode && family == NativeSocket.AF_INET6) || (!NativeSocket.UnixMode && (AddressFamily)family == AddressFamily.InterNetworkV6))
            {
#if (NETCOREAPP || NETSTANDARD2_1)
                uint scope = unchecked((uint)(
                    (address[27] << 24) +
                    (address[26] << 16) +
                    (address[25] << 8) +
                    (address[24])));
                Address = new IPAddress(new ReadOnlySpan<byte>(address, 8, 16), scope);
#else
                var addrBuffer = new byte[16];
                Buffer.BlockCopy(address, 8, addrBuffer, 0, 16);
                uint scope = unchecked((uint)(
                    (address[27] << 24) +
                    (address[26] << 16) +
                    (address[25] << 8) +
                    (address[24])));
                Address = new IPAddress(addrBuffer, scope);
#endif
            }
            else //IPv4
            {
                long ipv4Addr = unchecked((uint)((address[4] & 0x000000FF) |
                                                 (address[5] << 8 & 0x0000FF00) |
                                                 (address[6] << 16 & 0x00FF0000) |
                                                 (address[7] << 24)));
                Address = new IPAddress(ipv4Addr);
            }
        }
    }

    internal class NativeAddrComparer : IEqualityComparer<NativeAddr>
    {
        public bool Equals(NativeAddr x, NativeAddr y)
        {
            return x.Part1 == y.Part1 &&
                   x.Part2 == y.Part2 &&
                   x.Part3 == y.Part3 &&
                   x.Part4 == y.Part4;
        }

        public int GetHashCode(NativeAddr obj)
        {
            return obj.GetHashCode();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeTimeValue
    {
        public int Seconds;
        public int Microseconds;
    }

    internal static class NativeSocket
    {
        internal static
#if LITENETLIB_UNSAFE
        unsafe
#endif
        class WinSock
        {
            private const string LibName = "ws2_32.dll";

            [DllImport(LibName, SetLastError = true)]
            public static extern int recvfrom(
                IntPtr socketHandle,
#if LITENETLIB_UNSAFE
                byte* pinnedBuffer,
#else
                [In, Out] byte[] pinnedBuffer,
#endif
                [In] int len,
                [In] SocketFlags socketFlags,
                [Out] byte[] socketAddress,
                [In, Out] ref int socketAddressSize);

            [DllImport(LibName, SetLastError = true)]
            internal static extern int sendto(
                IntPtr socketHandle,
#if LITENETLIB_UNSAFE
                byte* pinnedBuffer,
#else
                [In] byte[] pinnedBuffer,
#endif
                [In] int len,
                [In] SocketFlags socketFlags,
                [In] byte[] socketAddress,
                [In] int socketAddressSize);

            [DllImport(LibName, SetLastError = true)]
            internal static extern int select(
                [In] int ignoredParameter,
#if LITENETLIB_UNSAFE
                IntPtr* readfds,
                IntPtr* writefds,
                IntPtr* exceptfds,
#else
                [In, Out] IntPtr[] readfds,
                [In, Out] IntPtr[] writefds,
                [In, Out] IntPtr[] exceptfds,
#endif
                [In] ref NativeTimeValue timeout);
        }

        internal static
#if LITENETLIB_UNSAFE
        unsafe
#endif
        class UnixSock
        {
            private const string LibName = "libc";

            [DllImport(LibName)]
            public static extern int recvfrom(
                IntPtr socketHandle,
#if LITENETLIB_UNSAFE
                byte* pinnedBuffer,
#else
                [In, Out] byte[] pinnedBuffer,
#endif
                [In] int len,
                [In] SocketFlags socketFlags,
                [Out] byte[] socketAddress,
                [In, Out] ref int socketAddressSize);

            [DllImport(LibName)]
            internal static extern int sendto(
                IntPtr socketHandle,
#if LITENETLIB_UNSAFE
                byte* pinnedBuffer,
#else
                [In] byte[] pinnedBuffer,
#endif
                [In] int len,
                [In] SocketFlags socketFlags,
                [In] byte[] socketAddress,
                [In] int socketAddressSize);

            [DllImport(LibName, SetLastError = true)]
            internal static extern int select(
                [In] int ignoredParameter,
#if LITENETLIB_UNSAFE
                IntPtr* readfds,
                IntPtr* writefds,
                IntPtr* exceptfds,
#else
                [In, Out] IntPtr[] readfds,
                [In, Out] IntPtr[] writefds,
                [In, Out] IntPtr[] exceptfds,
#endif
                [In] ref NativeTimeValue timeout);
        }

        public static readonly bool IsSupported;
        public static readonly bool UnixMode;

#if !LITENETLIB_UNSAFE
        [ThreadStatic] private static byte[] SendToBuffer;
#endif
        [ThreadStatic] private static byte[] EndPointBuffer;

        public const int MaxAddrSize = 28;
        public const int AF_INET = 2;
        public const int AF_INET6 = 10;

        internal enum UnixSocketError
        {
            SUCCESS          = 0,
            EACCES           = 0x10002,
            EADDRINUSE       = 0x10003,
            EADDRNOTAVAIL    = 0x10004,
            EAFNOSUPPORT     = 0x10005,
            EAGAIN           = 0x10006,
            EALREADY         = 0x10007,
            EBADF            = 0x10008,
            ECANCELED        = 0x1000B,
            ECONNABORTED     = 0x1000D,
            ECONNREFUSED     = 0x1000E,
            ECONNRESET       = 0x1000F,
            EDESTADDRREQ     = 0x10011,
            EFAULT           = 0x10015,
            EHOSTUNREACH     = 0x10017,
            EINPROGRESS      = 0x1001A,
            EINTR            = 0x1001B,
            EINVAL           = 0x1001C,
            EISCONN          = 0x1001E,
            EMFILE           = 0x10021,
            EMSGSIZE         = 0x10023,
            ENETDOWN         = 0x10026,
            ENETRESET        = 0x10027,
            ENETUNREACH      = 0x10028,
            ENFILE           = 0x10029,
            ENOBUFS          = 0x1002A,
            ENOENT           = 0x1002D,
            ENOPROTOOPT      = 0x10033,
            ENOTCONN         = 0x10038,
            ENOTSOCK         = 0x1003C,
            ENOTSUP          = 0x1003D,
            ENXIO            = 0x1003F,
            EPERM            = 0x10042,
            EPIPE            = 0x10043,
            EPROTONOSUPPORT  = 0x10045,
            EPROTOTYPE       = 0x10046,
            ETIMEDOUT        = 0x1004D,
            ESOCKTNOSUPPORT  = 0x1005E,
            EPFNOSUPPORT     = 0x10060,
            ESHUTDOWN        = 0x1006C,
            EHOSTDOWN        = 0x10070,
            ENODATA          = 0x10071
        }
        
        private static readonly Dictionary<UnixSocketError, SocketError> NativeErrorToSocketError = new Dictionary<UnixSocketError, SocketError>(42)
        {
            { UnixSocketError.EACCES, SocketError.AccessDenied },
            { UnixSocketError.EADDRINUSE, SocketError.AddressAlreadyInUse },
            { UnixSocketError.EADDRNOTAVAIL, SocketError.AddressNotAvailable },
            { UnixSocketError.EAFNOSUPPORT, SocketError.AddressFamilyNotSupported },
            { UnixSocketError.EAGAIN, SocketError.WouldBlock },
            { UnixSocketError.EALREADY, SocketError.AlreadyInProgress },
            { UnixSocketError.EBADF, SocketError.OperationAborted },
            { UnixSocketError.ECANCELED, SocketError.OperationAborted },
            { UnixSocketError.ECONNABORTED, SocketError.ConnectionAborted },
            { UnixSocketError.ECONNREFUSED, SocketError.ConnectionRefused },
            { UnixSocketError.ECONNRESET, SocketError.ConnectionReset },
            { UnixSocketError.EDESTADDRREQ, SocketError.DestinationAddressRequired },
            { UnixSocketError.EFAULT, SocketError.Fault },
            { UnixSocketError.EHOSTDOWN, SocketError.HostDown },
            { UnixSocketError.ENXIO, SocketError.HostNotFound },
            { UnixSocketError.EHOSTUNREACH, SocketError.HostUnreachable },
            { UnixSocketError.EINPROGRESS, SocketError.InProgress },
            { UnixSocketError.EINTR, SocketError.Interrupted },
            { UnixSocketError.EINVAL, SocketError.InvalidArgument },
            { UnixSocketError.EISCONN, SocketError.IsConnected },
            { UnixSocketError.EMFILE, SocketError.TooManyOpenSockets },
            { UnixSocketError.EMSGSIZE, SocketError.MessageSize },
            { UnixSocketError.ENETDOWN, SocketError.NetworkDown },
            { UnixSocketError.ENETRESET, SocketError.NetworkReset },
            { UnixSocketError.ENETUNREACH, SocketError.NetworkUnreachable },
            { UnixSocketError.ENFILE, SocketError.TooManyOpenSockets },
            { UnixSocketError.ENOBUFS, SocketError.NoBufferSpaceAvailable },
            { UnixSocketError.ENODATA, SocketError.NoData },
            { UnixSocketError.ENOENT, SocketError.AddressNotAvailable },
            { UnixSocketError.ENOPROTOOPT, SocketError.ProtocolOption },
            { UnixSocketError.ENOTCONN, SocketError.NotConnected },
            { UnixSocketError.ENOTSOCK, SocketError.NotSocket },
            { UnixSocketError.ENOTSUP, SocketError.OperationNotSupported },
            { UnixSocketError.EPERM, SocketError.AccessDenied },
            { UnixSocketError.EPIPE, SocketError.Shutdown },
            { UnixSocketError.EPFNOSUPPORT, SocketError.ProtocolFamilyNotSupported },
            { UnixSocketError.EPROTONOSUPPORT, SocketError.ProtocolNotSupported },
            { UnixSocketError.EPROTOTYPE, SocketError.ProtocolType },
            { UnixSocketError.ESOCKTNOSUPPORT, SocketError.SocketNotSupported },
            { UnixSocketError.ESHUTDOWN, SocketError.Disconnecting },
            { UnixSocketError.SUCCESS, SocketError.Success },
            { UnixSocketError.ETIMEDOUT, SocketError.TimedOut },
        };

        static
#if LITENETLIB_UNSAFE
            unsafe
#endif
            NativeSocket()
        {
            int temp = 0;
            IntPtr p = IntPtr.Zero;
            
            try
            {
                WinSock.recvfrom(p, null, 0, 0, null, ref temp);
                IsSupported = true;
            }
            catch
            {
                UnixMode = true;
            }

            if (UnixMode)
            {
                try
                {
                    UnixSock.recvfrom(p, null, 0, 0, null, ref temp);
                    IsSupported = true;
                }
                catch
                {
                    //do nothing
                }
            }
        }

        public static SocketError GetSocketError()
        {
            int error = Marshal.GetLastWin32Error();
            if (UnixMode)
                return NativeErrorToSocketError.TryGetValue((UnixSocketError)error, out var err) ? err : SocketError.SocketError;
            return (SocketError)error;
        }

        public static
#if LITENETLIB_UNSAFE
            unsafe
#endif
            int SendTo(Socket s, byte[] buffer, int start, int size, IPEndPoint endPoint)
        {
            byte[] socketAddress;

            if (endPoint is NativeEndPoint nep)
            {
                socketAddress = nep.NativeAddress;
            }
            else //Convert endpoint to raw
            {
                if (EndPointBuffer == null)
                    EndPointBuffer = new byte[MaxAddrSize];
                socketAddress = EndPointBuffer;

                bool ipv4 = endPoint.AddressFamily == AddressFamily.InterNetwork;
                short addressFamily = UnixMode ? (short) (ipv4 ? AF_INET : AF_INET6) : (short) endPoint.AddressFamily;
                socketAddress[0] = (byte) (addressFamily);
                socketAddress[1] = (byte) (addressFamily >> 8);
                socketAddress[2] = (byte) (endPoint.Port >> 8);
                socketAddress[3] = (byte) (endPoint.Port);

                if (ipv4)
                {
#pragma warning disable 618
                    long addr = endPoint.Address.Address;
#pragma warning restore 618
                    socketAddress[4] = (byte) (addr);
                    socketAddress[5] = (byte) (addr >> 8);
                    socketAddress[6] = (byte) (addr >> 16);
                    socketAddress[7] = (byte) (addr >> 24);
                }
                else
                {
#if (NETCOREAPP || NETSTANDARD2_1)
                    endPoint.Address.TryWriteBytes(new Span<byte>(socketAddress, 8, 16), out _);
#else
                    byte[] addrBytes = endPoint.Address.GetAddressBytes();
                    Buffer.BlockCopy(addrBytes, 0, socketAddress, 8, 16);
#endif
                }
            }

#if LITENETLIB_UNSAFE
            fixed (byte* data = &buffer[start])
            {
                int bytesSent = UnixMode
                    ? UnixSock.sendto(s.Handle, data, size, 0, socketAddress, socketAddress.Length)
                    : WinSock.sendto(s.Handle, data, size, 0, socketAddress, socketAddress.Length);
            }
#else
            if (start > 0)
            {
                if (SendToBuffer == null)
                    SendToBuffer = new byte[NetConstants.MaxPacketSize];
                Buffer.BlockCopy(buffer, start, SendToBuffer, 0, size);
                buffer = SendToBuffer;
            }
            int bytesSent = UnixMode
                ? UnixSock.sendto(s.Handle, buffer, size, 0, socketAddress, socketAddress.Length)
                : WinSock.sendto(s.Handle, buffer, size, 0, socketAddress, socketAddress.Length);
#endif
            if (bytesSent == -1)
                throw new SocketException((int)GetSocketError());
            return bytesSent;
        }
    }
}
