using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

WebSocketServer server = new();

await server.StartAsync();

/*
ConcurrentDictionary<string, WebSocket> Clients = new();


var port = Environment.GetEnvironmentVariable("PORT") ?? "8888";
var httpListener = new HttpListener();
httpListener.Prefixes.Add($"http://localhost:{port}/");
httpListener.Start();

Console.WriteLine($"Listening on http://localhost:{port}/");

var cts = new CancellationTokenSource();
var token = cts.Token;

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Press Ctrl+C to stop the server");

while (!token.IsCancellationRequested)
{

    /*
    var httpContext = await httpListener.GetContextAsync();
    if (httpContext.Request.IsWebSocketRequest)
    {
        var webSocketContext = await httpContext.AcceptWebSocketAsync(null);
        var socketId = Guid.NewGuid().ToString();
        Console.WriteLine($"Client connected: {socketId}");

        Clients.TryAdd(socketId, webSocketContext.WebSocket);

        

        _ = HandleClient(socketId, webSocketContext.WebSocket);
    }
    else
    {
        httpContext.Response.StatusCode = 400;
        httpContext.Response.Close();
    }
    
}
/*
/*
httpListener.Stop();


async Task HandleClient(string clientId, WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];

    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine($"Client disconnected: {clientId}");
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received from {clientId}: {message}");

            // Relay messages to other clients
            foreach (var kvp in Clients)
            {
                if (kvp.Key != clientId && kvp.Value.State == WebSocketState.Open)
                {
                    await kvp.Value.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error with client {clientId}: {ex.Message}");
    }
    finally
    {
        Clients.TryRemove(clientId, out _);
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }
}

void SendSocketMsg(WebSocket socket, SocketMessage msg){
    var json = JsonSerializer.Serialize(msg);
    socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, CancellationToken.None);
}

public enum Message {
	JOIN,
	ID, 
	PEER_CONNECT,
	PEER_DISCONNECT,
	OFFER,
	ANSWER,
	CANDIDATE,
	SEAL
}

public class SocketMessage{
	public Message type { get; set; }
	public int id { get; set; }
	public string data { get; set; }
}
*/