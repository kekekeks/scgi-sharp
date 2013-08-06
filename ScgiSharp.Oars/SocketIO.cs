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
			unsafe
			{
				fixed (byte* ptr = &(buffer.Array[buffer.Offset]))
					return recv (fd, ptr, buffer.Count, flags);
			}
		}

		public static int Send (this IntPtr fd, ArraySegment<byte> buffer, int flags)
		{
			unsafe
			{
				fixed (byte *ptr = &(buffer.Array[buffer.Offset]))
					return send (fd, ptr, buffer.Count, flags);
			}
		}

		[DllImport("libc", SetLastError = true)]
		static extern int close (IntPtr fd);

		[DllImport("libc", SetLastError = true)]
		static unsafe extern int send (IntPtr fd, byte* buffer, int length, int flags);

		[DllImport("libc", SetLastError = true)]
		static unsafe extern int recv (IntPtr fd, byte* buffer, int length, int flags);
	
		
		public const int MSG_DONTWAIT = 0x40;
		public delegate int SocketOp (IntPtr socket,ArraySegment<byte> segment,int flags);
	}

	public static class Errno
	{
		public const int EAGAIN = 11;
		public const int EPIPE = 32;
	}
}

