/*
using Godot;
using System;
using System.Collections.Generic;


//TODO: Convert this and hook it up. 

public partial class ServerNode : Node
{
    private enum Message
    {
        JOIN,
        ID,
        PEER_CONNECT,
        PEER_DISCONNECT,
        OFFER,
        ANSWER,
        CANDIDATE,
        SEAL,
    }

    // Unresponsive clients time out after this time (in milliseconds).
    private const int TIMEOUT = 1000;

    // A sealed room will be closed after this time (in milliseconds).
    private const int SEAL_TIME = 10000;

    // All alphanumeric characters.
    private const string ALFNUM = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private byte[] _alfnum = System.Text.Encoding.ASCII.GetBytes(ALFNUM);

    private RandomNumberGenerator rand = new RandomNumberGenerator();
    private Dictionary<string, Lobby> lobbies = new Dictionary<string, Lobby>();
    private TCPServer tcpServer = new TCPServer();
    private Dictionary<int, Peer> peers = new Dictionary<int, Peer>();

    private class Peer
    {
        public int Id { get; set; } = -1;
        public string Lobby { get; set; } = "";
        public ulong Time { get; set; } = Time.GetTicksMsec();
        public WebSocketPeer Ws { get; set; } = new WebSocketPeer();

        public Peer(int peerId, StreamPeer tcp)
        {
            Id = peerId;
            Ws.AcceptStream(tcp);
        }

        public bool IsWsOpen() => Ws.GetReadyState() == WebSocketPeer.StateOpen;

        public void Send(int type, int id, string data = "")
        {
            var message = new Dictionary<string, object>
            {
                { "type", type },
                { "id", id },
                { "data", data }
            };
            Ws.SendText(JSON.Stringify(message));
        }
    }

    private class Lobby
    {
        public Dictionary<int, Peer> Peers { get; } = new Dictionary<int, Peer>();
        public int Host { get; private set; }
        public bool Sealed { get; private set; } = false;
        public ulong Time { get; private set; } = 0;
        public bool Mesh { get; private set; } = true;

        public Lobby(int hostId, bool useMesh)
        {
            Host = hostId;
            Mesh = useMesh;
        }

        public bool Join(Peer peer)
        {
            if (Sealed || !peer.IsWsOpen())
                return false;

            peer.Send((int)Message.ID, peer.Id == Host ? 1 : peer.Id, Mesh ? "true" : "");
            foreach (var p in Peers.Values)
            {
                if (!p.IsWsOpen())
                    continue;

                if (!Mesh && p.Id != Host)
                    continue;

                p.Send((int)Message.PEER_CONNECT, peer.Id);
                peer.Send((int)Message.PEER_CONNECT, p.Id == Host ? 1 : p.Id);
            }
            Peers[peer.Id] = peer;
            return true;
        }

        public bool Leave(Peer peer)
        {
            if (!Peers.ContainsKey(peer.Id))
                return false;

            Peers.Remove(peer.Id);
            bool close = peer.Id == Host;

            if (Sealed)
                return close;

            foreach (var p in Peers.Values)
            {
                if (!p.IsWsOpen())
                    continue;

                if (close)
                    p.Ws.Close();
                else
                    p.Send((int)Message.PEER_DISCONNECT, peer.Id);
            }
            return close;
        }

        public bool Seal(int peerId)
        {
            if (Host != peerId)
                return false;

            Sealed = true;
            foreach (var p in Peers.Values)
            {
                if (!p.IsWsOpen())
                    continue;

                p.Send((int)Message.SEAL, 0);
            }
            Time = Time.GetTicksMsec();
            Peers.Clear();
            return true;
        }
    }

    public override void _Process(double delta)
    {
        Poll();
    }

    public void Listen(int port)
    {
        if (OS.HasFeature("web"))
        {
            OS.Alert("Cannot create WebSocket servers in Web exports due to browsers' limitations.");
            return;
        }
        Stop();
        rand.Seed = (ulong)Time.GetUnixTimeFromSystem();
        tcpServer.Listen(port);
    }

    public void Stop()
    {
        tcpServer.Stop();
        peers.Clear();
    }

    private void Poll()
    {
        if (!tcpServer.IsListening())
            return;

        if (tcpServer.IsConnectionAvailable())
        {
            int id = (int)(rand.Randi() % (1 << 31));
            peers[id] = new Peer(id, tcpServer.TakeConnection());
        }

        var toRemove = new List<int>();
        foreach (var peer in peers.Values)
        {
            if (string.IsNullOrEmpty(peer.Lobby) && Time.GetTicksMsec() - peer.Time > TIMEOUT)
                peer.Ws.Close();

            peer.Ws.Poll();
            while (peer.IsWsOpen() && peer.Ws.GetAvailablePacketCount() > 0)
            {
                if (!_ParseMsg(peer))
                {
                    GD.PrintErr($"Parse message failed from peer {peer.Id}");
                    toRemove.Add(peer.Id);
                    peer.Ws.Close();
                    break;
                }
            }

            if (peer.Ws.GetReadyState() == WebSocketPeer.StateClosed)
            {
                GD.Print($"Peer {peer.Id} disconnected from lobby: '{peer.Lobby}'");
                if (lobbies.ContainsKey(peer.Lobby) && lobbies[peer.Lobby].Leave(peer))
                {
                    GD.Print($"Deleted lobby {peer.Lobby}");
                    lobbies.Remove(peer.Lobby);
                }
                toRemove.Add(peer.Id);
            }
        }

        foreach (var lobby in lobbies.Values)
        {
            if (!lobby.Sealed || lobby.Time + SEAL_TIME >= Time.GetTicksMsec())
                continue;

            foreach (var peer in lobby.Peers.Values)
            {
                peer.Ws.Close();
                toRemove.Add(peer.Id);
            }
        }

        foreach (var id in toRemove)
        {
            peers.Remove(id);
        }
    }

    private bool _ParseMsg(Peer peer)
    {
        var pktStr = peer.Ws.GetPacket().GetStringFromUtf8();
        var parsed = JSON.Parse(pktStr).As<Dictionary<string, object>>();

        if (parsed == null || !parsed.ContainsKey("type") || !parsed.ContainsKey("id") || !(parsed["data"] is string data))
            return false;

        int msgType = Convert.ToInt32(parsed["type"]);
        int msgId = Convert.ToInt32(parsed["id"]);

        if (msgType == (int)Message.JOIN)
        {
            if (!string.IsNullOrEmpty(peer.Lobby))
                return false;

            return _JoinLobby(peer, data, msgId == 0);
        }

        if (!lobbies.ContainsKey(peer.Lobby))
            return false;

        var lobby = lobbies[peer.Lobby];

        if (msgType == (int)Message.SEAL)
        {
            return lobby.Seal(peer.Id);
        }

        if (!peers.ContainsKey(msgId) || peers[msgId].Lobby != peer.Lobby)
            return false;

        if (msgType == (int)Message.OFFER || msgType == (int)Message.ANSWER || msgType == (int)Message.CANDIDATE)
        {
            int source = peer.Id == lobby.Host ? MultiplayerPeer.TargetPeerServer : peer.Id;
            peers[msgId].Send(msgType, source, data);
            return true;
        }

        return false;
    }

    private bool _JoinLobby(Peer peer, string lobbyName, bool mesh)
    {
        if (string.IsNullOrEmpty(lobbyName))
        {
            for (int i = 0; i < 32; i++)
            {
                lobbyName += (char)_alfnum[rand.RandiRange(0, ALFNUM.Length - 1)];
            }
            lobbies[lobbyName] = new Lobby(peer.Id, mesh);
        }
        else if (!lobbies.ContainsKey(lobbyName))
        {
            return false;
        }

        lobbies[lobbyName].Join(peer);
        peer.Lobby = lobbyName;
        peer.Send((int)Message.JOIN, 0, lobbyName);
        GD.Print($"Peer {peer.Id} joined lobby: '{lobbyName}'");
        return true;
    }
}
*/