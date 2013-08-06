using System;
using System.Threading.Tasks;

namespace ScgiSharp.IO
{
	public interface ISocket : IDisposable
	{
		Task<int> RecieveAsync (byte[] buffer, int offset, int size);
		Task<int> SendAsync (byte[] buffer, int offset, int size);
		void Close ();

	}
}
