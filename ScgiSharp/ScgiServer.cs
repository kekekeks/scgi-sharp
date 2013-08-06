using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ScgiSharp.IO;

namespace ScgiSharp
{
	public class ScgiServer
	{
		readonly ISocketListener _listener;

		public ScgiServer (ISocketListener listener)
		{
			_listener = listener;
		}

		public ScgiServer ()
			: this (new DefaultSocketListener ())
		{

		}


		public void Listen (IPAddress address, int port, int backlog)
		{
			_listener.Listen (address, port, backlog);
		}

		
		public async Task<ScgiConnection> AcceptConnectionAsync ()
		{
			var client = await _listener.AcceptSocket ();
			return new ScgiConnection (client);
		}
	}
}
