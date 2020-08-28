using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web;
using System.Threading;
using System.Text;
using System.IO;
using dotnet_framework_server.Services;

namespace dotnet_framework_server.Controllers
{
	public class WebSocketController : ApiController
	{
		[HttpGet]
		public HttpResponseMessage Get()
		{
			// wss://localhost:53262/api/websocket
			if (!HttpContext.Current.IsWebSocketRequest)
			{
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			}

			HttpContext.Current.AcceptWebSocketRequest(async (context) =>
			{
				WebSocket webSocket = context.WebSocket;
				WebSocketManager webSocketManager = WebSocketManager.Instance;
				await webSocketManager.AddClient(webSocket);
			});

			return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
		}

		// GET api/<controller>
		//public IEnumerable<string> Get()
		//{
		//	return new string[] { "value1", "value2" };
		//}

		// GET api/<controller>/5
		public string Get(int id)
		{
			return "value";
		}

		// POST api/<controller>
		public void Post([FromBody] string value)
		{
		}

		// PUT api/<controller>/5
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/<controller>/5
		public void Delete(int id)
		{
		}
	}
}