using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScgiSharp.IO
{
	public class DefaultSocketListener : ISocketListener
	{
		TcpListener _listener;
		public Task<ISocket> AcceptSocket ()
		{
			return FromAsync ((cb, s) => _listener.BeginAcceptSocket (cb, s), r => _listener.EndAcceptSocket (r)).ContinueWith (t =>
				{
					t.PropagateExceptions ();
					return (ISocket)new DefaultSocket (t.Result);
				});

		}

		public void Listen (IPAddress address, int port, int backlog)
		{
			_listener = new TcpListener (address, port);
			_listener.Start (backlog);
		}


		class DefaultSocket : ISocket
		{
			private readonly Socket _socket;

			public DefaultSocket (Socket socket)
			{
				_socket = socket;
			}

			public static Task<int> RecieveAsync (Socket socket, byte[] buffer, int offset, int size)
			{
				return FromAsync ((cb, state) => socket.BeginReceive (buffer, offset, size, SocketFlags.None, cb, state), socket.EndReceive);
			}

			public static Task<int> SendAsync (Socket socket, byte[] buffer, int offset, int size)
			{
				return FromAsync ((cb, state) => socket.BeginSend (buffer, offset, size, SocketFlags.None, cb, state), socket.EndSend);
			}

			public void Dispose ()
			{
				_socket.Dispose ();
			}

			public Task<int> RecieveAsync (byte[] buffer, int offset, int size)
			{
				return RecieveAsync (_socket, buffer, offset, size);
			}

			public Task<int> SendAsync (byte[] buffer, int offset, int size)
			{
				return SendAsync (_socket, buffer, offset, size);
			}

			public void Close ()
			{
				_socket.Shutdown (SocketShutdown.Send);
				_socket.Dispose ();
			}
		}

		static Task<T> FromAsync<T> (Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, T> end)
		{
			return Task.Factory.FromAsync<T> (begin, end, null);
		}
	}
}
