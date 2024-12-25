using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketServer
{
    private const int MAX_PEERS = 4096;
    private const int MAX_LOBBIES = 1024;
    private const int PORT = 8888;
    private const string ALFNUM = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private const int NO_LOBBY_TIMEOUT = 1000;
    private const int SEAL_CLOSE_TIMEOUT = 10000;
    private const int PING_INTERVAL = 10000;

    private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();
    private static int PeersCount = 0;

    public async Task StartAsync()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{PORT}/");
        listener.Start();

        Console.WriteLine($"WebSocket server started on port {PORT}");

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
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var webSocket = await context.AcceptWebSocketAsync(null);
                _ = HandleConnection(webSocket.WebSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private async Task HandleConnection(WebSocket ws)
    {
        if (PeersCount >= MAX_PEERS)
        {
            await CloseSocket(ws, WebSocketCloseStatus.PolicyViolation, "Too many peers connected");
            return;
        }

        Interlocked.Increment(ref PeersCount);
        var peer = new Peer(ws);

        try
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessage(peer, message);
                }
                else
                {
                    await CloseSocket(ws, WebSocketCloseStatus.InvalidMessageType, "Invalid transfer mode, must be text");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await HandleDisconnect(peer);
        }
    }

    private async Task HandleMessage(Peer peer, string message)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<ProtoMessage>(message);
            if (msg == null) throw new Exception("Invalid message format");

            var lobby = Lobbies.TryGetValue(peer.Lobby, out var l) ? l : null;
            if(lobby == null && msg.type != Command.JOIN){
                throw new Exception("No lobby assigned");
            }

            switch (msg.type)
            {
                case Command.JOIN:
                    await JoinLobby(peer, msg.data, msg.id == 0);
                    break;
                case Command.SEAL:
                    await SealLobby(peer);
                    break;
                case Command.OFFER:
                case Command.ANSWER:
                case Command.CANDIDATE:
                    var destid = msg.id;
                    if(msg.id == 1){
                        destid = lobby.Host;
                    }
                    var destPeer = lobby.Peers.FirstOrDefault(p => p.Id == destid);
                    if(destPeer == null){
                        throw new Exception("Peer not found");
                    }
                    await SendMessage(destPeer.WebSocket, new ProtoMessage { type = msg.type, id = lobby.getPeerId(peer), data = msg.data });
                    break;
                default:
                    throw new Exception("Invalid command");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling message: {ex.Message}");
            await CloseSocket(peer.WebSocket, WebSocketCloseStatus.InvalidPayloadData, ex.Message);
        }
    }

    private async Task JoinLobby(Peer peer, string lobbyName, bool isMesh)
    {
        Console.WriteLine($"Peer {peer.Id} joining lobby {lobbyName}");
        if (string.IsNullOrEmpty(lobbyName))
        {
            if (Lobbies.Count >= MAX_LOBBIES)
                throw new Exception("Too many lobbies open");

            lobbyName = GenerateRandomSecret();
            Lobbies[lobbyName] = new Lobby(lobbyName, peer.Id, isMesh);
        }

        if (!Lobbies.TryGetValue(lobbyName, out var lobby))
            throw new Exception("Lobby does not exist");

        if (lobby.Sealed)
            throw new Exception("Lobby is sealed");

        await lobby.Join(peer);
        Console.WriteLine($"Peer {peer.Id} joined lobby {lobbyName} with {lobby.Peers.Count} peers");
        var joinMessage = new ProtoMessage { type = Command.JOIN, data = lobbyName };
        await SendMessage(peer.WebSocket, joinMessage);
    }

    private async Task SealLobby(Peer peer)
    {
        if (!Lobbies.TryGetValue(peer.Lobby, out var lobby))
            throw new Exception("Lobby does not exist");

        lobby.Seal(peer);
        foreach (var p in lobby.Peers)
        {
            await CloseSocket(p.WebSocket, WebSocketCloseStatus.NormalClosure, "Seal complete");
        }
    }

    private async Task HandleDisconnect(Peer peer)
    {
        Interlocked.Decrement(ref PeersCount);
        if (Lobbies.TryGetValue(peer.Lobby, out var lobby))
        {
            await lobby.Leave(peer);
            if (lobby.Peers.Count == 0)
            {
                Lobbies.TryRemove(peer.Lobby, out _);
                Console.WriteLine($"Deleted lobby {lobby.Name}");
                Console.WriteLine($"Lobbies count: {Lobbies.Count}");
            }
        }
    }

    private async Task CloseSocket(WebSocket ws, WebSocketCloseStatus status, string reason)
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(status, reason, CancellationToken.None);
        }
    }

    private async Task SendMessage(WebSocket ws, ProtoMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private string GenerateRandomSecret()
    {
        var random = new Random();
        return new string(Enumerable.Range(0, 16).Select(_ => ALFNUM[random.Next(ALFNUM.Length)]).ToArray());
    }

}

public class Peer
{
    public int Id { get; }
    public WebSocket WebSocket { get; }
    public string Lobby { get; set; } = string.Empty;

    public Peer(WebSocket ws)
    {
        Id = RandomId();
        WebSocket = ws;
    }
    private int RandomId()
    {
        var randomBytes = new byte[4];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        // Convert bytes to a signed 32-bit integer
        int randomInt = BitConverter.ToInt32(randomBytes, 0);
        
        // Ensure it's positive
        return Math.Abs(randomInt);
    }
}

public class Lobby
{
    public string Name { get; }
    public int Host { get; }
    public bool IsMesh { get; }
    public bool Sealed { get; private set; }
    public List<Peer> Peers { get; }

    public Lobby(string name, int host, bool isMesh)
    {
        Name = name;
        Host = host;
        IsMesh = isMesh;
        Peers = new List<Peer>();
    }

    public int getPeerId(Peer peer){
        if(peer.Id == Host){
            return 1;
        }
        return peer.Id;
    }

    public async Task Join(Peer peer)
    {
        var assigned = getPeerId(peer);
        peer.Lobby = Name;
        var peerIDMsg = new ProtoMessage { type = Command.ID, id = assigned, data = IsMesh ? "true" : "" };
        await SendMessage(peer, peerIDMsg);
        
        Peers.Add(peer);
        
        foreach (var p in Peers)
        {
            if(p == peer){
                continue;
            }
            await SendMessage(p, new ProtoMessage { type = Command.PEER_CONNECT, id = assigned });
            await SendMessage(peer, new ProtoMessage { type = Command.PEER_CONNECT, id = getPeerId(p)});
        }

        Console.WriteLine($"Peer {peer.Id} joined lobby {Name}");
    }

    public async Task<bool> Leave(Peer peer)
    {
        if(!Peers.Contains(peer)){
            return false;
        }
        var assigned = getPeerId(peer);
        bool close = assigned == 1;

        foreach (var p in Peers)
        {
            await SendMessage(p, new ProtoMessage { type = Command.PEER_DISCONNECT, id = assigned });
        }

        Peers.Remove(peer);
        
        return close;
    }

    public async void Seal(Peer peer)
    {
        if (peer.Id != Host)
            throw new Exception("Only host can seal the lobby");

        Sealed = true;

        foreach (var p in Peers)
        {
            await SendMessage(p, new ProtoMessage { type = Command.SEAL, id = 0 });
        }

        Console.WriteLine($"Lobby {Name} sealed");

    }

    private async Task SendMessage(Peer peer, ProtoMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);
        Console.WriteLine($"Sending message to {peer.Id}: {json}");
        await peer.WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

public class ProtoMessage
{
    public Command type { get; set; }
    public int id { get; set; }
    public string? data { get; set; }
}

public enum Command
{
    JOIN,
	ID, 
	PEER_CONNECT,
	PEER_DISCONNECT,
	OFFER,
	ANSWER,
	CANDIDATE,
	SEAL
}