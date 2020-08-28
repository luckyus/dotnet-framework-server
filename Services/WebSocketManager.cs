using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace dotnet_framework_server.Services
{
	public class WebSocketClient
	{
		public string name;
		public WebSocket webSocket;
	}

	public class WebSocketItem
	{
		public WebSocketCommand Command { get; set; }
		public string Name { get; set; }
		public string Message { get; set; }
	}

	public enum WebSocketCommand
	{
		Init = 0,
		AckMsg = 1,
		ChatMsg = 2,
		Error = 99
	}

	public sealed class WebSocketManager
	{
		// singleton
		private static readonly Lazy<WebSocketManager> _instance = new Lazy<WebSocketManager>(() => new WebSocketManager(GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients));

		private static IHubConnectionContext<dynamic> Clients { get; set; }

		public static WebSocketManager Instance
		{
			get { return _instance.Value; }
		}

		private WebSocketManager(IHubConnectionContext<dynamic> clients)
		{
			Clients = clients;
		}

		public ConcurrentDictionary<string, WebSocketClient> WebSocketClients { get; } = new ConcurrentDictionary<string, WebSocketClient>();

		public async Task AddClient(WebSocket webSocket)
		{
			string connectionID = Guid.NewGuid().ToString();

			WebSocketClient webSocketClient = new WebSocketClient
			{
				webSocket = webSocket,
				name = connectionID
			};

			WebSocketClients.TryAdd(connectionID, webSocketClient);

			var jsonMsg = JsonConvert.SerializeObject(new { command = WebSocketCommand.ChatMsg, name = "iGuardPayroll", message = "ID: " + connectionID });
			await SendClientAsync(webSocket, jsonMsg);
			await ReceiveClientAsync(webSocket, connectionID);
		}

		public void RemoveClient(string connectionID)
		{
			WebSocketClients.TryRemove(connectionID, out _);
		}

		private async Task ReceiveClientAsync(WebSocket webSocket, string connectionID)
		{
			var receiveBuffer = new ArraySegment<Byte>(new Byte[100]);

			try
			{
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
							await BroadcastJsonMessage(message);
						}
						else if (result.MessageType == WebSocketMessageType.Close)
						{
							if (result.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
							{
							}

							RemoveClient(connectionID);
							await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
							await BroadcastMessage(connectionID, "disconnected!");
						}
					}
				}
			}
			catch (WebSocketException ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.WebSocketErrorCode);
				RemoveClient(connectionID);
				await BroadcastMessage(connectionID, "disconnected!");
			}
		}

		public Task SendClientAsync(WebSocket webSocket, string message)
		{
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
			return webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public Task BroadcastMessage(string name, string message)
		{
			WebSocketItem webSocketItem = new WebSocketItem
			{
				Command = WebSocketCommand.ChatMsg,
				Name = name,
				Message = message
			};

			// ref: https://www.newtonsoft.com/json/help/html/NamingStrategyCamelCase.htm (200828)
			DefaultContractResolver contractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			};

			string jsonStr = JsonConvert.SerializeObject(webSocketItem, new JsonSerializerSettings
			{
				ContractResolver = contractResolver,
				Formatting = Formatting.Indented
			});

			return BroadcastJsonMessage(jsonStr);
		}

		public async Task BroadcastJsonMessage(string jsonMessage)
		{
			try
			{
				WebSocketItem webSocketItem = JsonConvert.DeserializeObject<WebSocketItem>(jsonMessage);
				Clients.All.broadcastMessage(webSocketItem.Name, webSocketItem.Message);

				foreach (var client in WebSocketClients)
				{
					await client.Value.webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonMessage)), WebSocketMessageType.Text, true, CancellationToken.None);
				}
			}
			catch (Exception)
			{
			}
		}
	}
}