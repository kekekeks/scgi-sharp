using System;
using System.Threading.Tasks;
using System.IO;
using ScgiSharp.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ScgiSharp.OarsIo
{
	public class OarsSocket : ISocket
	{
		IntPtr _socket;
		OarsSocketListener _host;

		public OarsSocket (IntPtr socket, OarsSocketListener host)
		{
			_socket = socket;
			_host = host;
		}



		Task<int> DoSocketOpAsync (SocketIO.SocketOp op, Oars.Events events, byte[] buffer, int offset, int size)
		{
			if (_socket == IntPtr.Zero)
				throw new InvalidOperationException ("Socket is closed or disposed");
		
			var segment = new ArraySegment<byte> (buffer, offset, size);
			var read = op (_socket, segment, SocketIO.MSG_DONTWAIT);
			if (read == -1)
			{
				var errno = Marshal.GetLastWin32Error ();
				if (errno != Errno.EAGAIN)
				{
					if (errno == Errno.EPIPE)
						return Task<int>.FromResult (0);
					throw new IOException ("Error #" + errno + " on socket");
				}

				var tcs = new TaskCompletionSource<int> ();
				CallMeOnEvent (events, () =>
				{
					read = op (_socket, segment, SocketIO.MSG_DONTWAIT);
					if (read != -1)
					{
						tcs.SetResult (read);
						return;
					}
					errno = Marshal.GetLastWin32Error ();
					if (errno == Errno.EPIPE)
						tcs.SetResult (0);
					else
						tcs.SetException (new IOException ("Error #" + errno + " on socket"));
				});
				return tcs.Task;
			}
			else
				return Task<int>.FromResult (read);
		}

		public Task<int> RecieveAsync (byte[] buffer, int offset, int size)
		{
			return DoSocketOpAsync (SocketIO.Recv, Oars.Events.EV_READ, buffer, offset, size);
		}

		public Task<int> SendAsync (byte[] buffer, int offset, int size)
		{
			return DoSocketOpAsync (SocketIO.Send, Oars.Events.EV_WRITE, buffer, offset, size);
		}

		List<Oars.Event> _activeEvents = new List<Oars.Event> ();

		void CallMeOnEvent (Oars.Events events, Action act)
		{
			_host.Sync (() =>
			{
				var ev = new Oars.Event (_host._eventBase, _socket, events);
				_activeEvents.Add (ev);
				ev.Activated += () =>
				{
					ev.Delete ();
					ev.Dispose ();
					act ();
					_activeEvents.Remove (ev);
				};
				ev.Add (new TimeSpan (7, 1, 1, 1));
			});

		}

		public void Close ()
		{
			Dispose ();
		}

		public void Dispose ()
		{
			if (_socket == IntPtr.Zero)
				return;
			var socket = _socket;
			_socket = IntPtr.Zero;
			_host.Sync (() =>
			{
				foreach (var e in _activeEvents)
				{
					e.Delete ();
					e.Dispose ();
				}
				SocketIO.Close (socket);
			});
		}
	}
}
