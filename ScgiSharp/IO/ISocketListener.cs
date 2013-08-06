using System.Net;
using System.Threading.Tasks;

namespace ScgiSharp.IO
{
	public interface ISocketListener
	{
		Task<ISocket> AcceptSocket ();
		void Listen (IPAddress address, int port, int backlog);
	}
}
