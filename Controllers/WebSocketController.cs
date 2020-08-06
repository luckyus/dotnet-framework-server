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
		public HttpResponseMessage GetMessage()
		{
			// ws://localhost:53262/api/websocket
			if (HttpContext.Current.IsWebSocketRequest)
			{
				HttpContext.Current.AcceptWebSocketRequest(async (context) =>
				{
					WebSocket webSocket = context.WebSocket;
					WebSocketManager webSocketManager = WebSocketManager.Instance;

					string connectionID = webSocketManager.AddClient(webSocket);

					var receiveBuffer = new ArraySegment<Byte>(new Byte[100]);

					while (webSocket.State == WebSocketState.Open)
					{
						string message = string.Empty;

						// using stream to read unknown-sized result (200715)
						using (var ms = new MemoryStream())
						{
							WebSocketReceiveResult result;

							do
							{
								result = await webSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
								ms.Write(receiveBuffer.Array, receiveBuffer.Offset, result.Count);

							} while (!result.EndOfMessage);

							ms.Seek(0, SeekOrigin.Begin);

							if (result.MessageType == WebSocketMessageType.Text)
							{
								using (var reader = new StreamReader(ms, Encoding.UTF8)) { message = reader.ReadToEnd(); }
								await webSocketManager.ReceiveFromClient(connectionID, webSocket, message);
							}
							else if (result.MessageType == WebSocketMessageType.Close)
							{
								if (result.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
								{
									// e.g., browser closed
								}

								webSocketManager.RemoveClient(connectionID);
								await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
								return;
							}
						}

					}
				});
			}

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