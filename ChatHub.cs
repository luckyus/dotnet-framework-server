using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using dotnet_framework_server.Services;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;


namespace dotnet_framework_server
{
	public class ChatHub : Hub
	{
		public void Hello()
		{
			Clients.All.hello();
		}

		public void Send(string name, string message)
		{
			// Call the broadcastMessage method to update clients.
			Clients.All.broadcastMessage(name, message);

			// broadcase to all websocket clients (200718)
			WebSocketManager webSocketManager = WebSocketManager.Instance;
			WebSocketItem webSocketItem = new WebSocketItem
			{
				Command = WebSocketCommand.Send,
				Name = name,
				Message = message
			};
			string jsonMsg = JsonSerializer.Serialize<WebSocketItem>(webSocketItem);

			foreach (var client in webSocketManager.WebSocketClients)
			{
				client.Value.webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonMsg)), WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}
	}
}