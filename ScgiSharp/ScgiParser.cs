using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScgiSharp
{
	static class ScgiParser
	{
		public static async Task<IEnumerable<KeyValuePair<string, string>>> GetHeaders (Socket socket)
		{
			byte[] lenBuffer = await Util.ReadExactly (socket, 10);
			
			int pos = Array.IndexOf (lenBuffer, (byte)':');

			var lenString = Encoding.UTF8.GetString (lenBuffer, 0, pos);

			var len = int.Parse (lenString);

			var headers = new MemoryStream ();
			pos++;
			var alreadyReadLen = lenBuffer.Length - pos;
			headers.Write (lenBuffer, pos, alreadyReadLen);

			await Util.ReadDataAsync (socket, headers, len - alreadyReadLen);

			var rv = new List<KeyValuePair<string, string>> ();

			var headerPairs = Encoding.UTF8.GetString (headers.ToArray ()).Split ((char)0);
			for (var i = 0; i < headerPairs.Length - 1; i += 2)
				rv.Add (new KeyValuePair<string, string> (headerPairs[i], headerPairs[i + 1]));


			return rv;
		}

	}
}