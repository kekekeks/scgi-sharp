using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScgiSharp
{
	public class ScgiServer
	{
		TcpListener _listener;
		public void Listen (IPAddress address, int port, int backlog)
		{
			_listener = new TcpListener (address, port);
			_listener.Start (backlog);
		}

		public async Task<ScgiConnection> AcceptConnectionAsync ()
		{
			var client = await _listener.AcceptSocketAsync ();
			return new ScgiConnection (client);
		}
	}
}
