using System;
using System.Runtime.InteropServices;

namespace ScgiSharp.OarsIo
{
	public static class SocketIO
	{
		public static int Close (this IntPtr fd)
		{
			return close (fd);
		}

		public static int Recv (this IntPtr fd, ArraySegment<byte> buffer, int flags)
		{
			var handle = GCHandle.Alloc (buffer.Array, GCHandleType.Pinned);
			var ptr = handle.AddrOfPinnedObject () + buffer.Offset;
			var rv = recv (fd, ptr, buffer.Count, flags);
			handle.Free ();
			return rv;
		}

		public static int Send (this IntPtr fd, ArraySegment<byte> buffer, int flags)
		{
			var handle = GCHandle.Alloc (buffer.Array, GCHandleType.Pinned);
			var ptr = handle.AddrOfPinnedObject () + buffer.Offset;
			var rv = send (fd, ptr, buffer.Count, flags);
			handle.Free ();
			return rv;
		}

		[DllImport("libc", SetLastError = true)]
		static extern int close (IntPtr fd);

		[DllImport("libc", SetLastError = true)]
		static unsafe extern int send (IntPtr fd, IntPtr buffer, int length, int flags);

		[DllImport("libc", SetLastError = true)]
		static unsafe extern int recv (IntPtr fd, IntPtr buffer, int length, int flags);
	
		
		public const int MSG_DONTWAIT = 0x40;
		public delegate int SocketOp (IntPtr socket,ArraySegment<byte> segment,int flags);
	}

	public static class Errno
	{
		public const int EAGAIN = 11;
		public const int EPIPE = 32;
	}
}

