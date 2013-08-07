using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ScgiSharp.IO;

namespace ScgiSharp
{
	static class ScgiParser
	{
		public static Task<IEnumerable<KeyValuePair<string, string>>> GetHeaders (ISocket socket)
		{
			var headers = new MemoryStream ();
			return Util.ReadExactly (socket, 10).ContinueWith (t =>
			{
				t.PropagateExceptions ();
				byte[] lenBuffer = t.Result;

				int pos = Array.IndexOf (lenBuffer, (byte)':');

				var lenString = Encoding.UTF8.GetString (lenBuffer, 0, pos);

				var len = int.Parse (lenString) + 1;


				pos++;
				var alreadyReadLen = lenBuffer.Length - pos;
				headers.Write (lenBuffer, pos, alreadyReadLen);

				return Util.ReadDataAsync (socket, headers, len - alreadyReadLen);
			}).Unwrap ().ContinueWith (t2 =>
			{
				t2.PropagateExceptions ();
				var rv = new List<KeyValuePair<string, string>> ();

				var headerPairs = Encoding.UTF8.GetString (headers.ToArray ()).Split ((char)0);
				for (var i = 0; i < headerPairs.Length - 1; i += 2)
					rv.Add (new KeyValuePair<string, string> (headerPairs[i], headerPairs[i + 1]));

				return Util.TaskFromResult ((IEnumerable<KeyValuePair<string, string>>)rv);
			}).Unwrap ();

		}

	}
}