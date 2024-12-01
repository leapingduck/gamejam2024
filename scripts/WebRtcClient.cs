using Godot;
using System;
using System.Text.Json;

public partial class WebRtcClient : Node
{
	/*
    public enum Message
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
	*/
    [Export] public bool Autojoin { get; set; } = true;
    [Export] public string Lobby { get; set; } = ""; // Will create a new lobby if empty.
    [Export] public bool Mesh { get; set; } = true; // Will use the lobby host as relay otherwise.

    private WebSocketPeer _ws = new WebSocketPeer();
    private int _code = 1000;
    private string _reason = "Unknown";
    private WebSocketPeer.State _oldState = WebSocketPeer.State.Closed;

    [Signal] public delegate void LobbyJoinedEventHandler(string lobby);
    [Signal] public delegate void ConnectedEventHandler(int id, bool useMesh);
    [Signal] public delegate void DisconnectedEventHandler();
    [Signal] public delegate void PeerConnectedEventHandler(int id);
    [Signal] public delegate void PeerDisconnectedEventHandler(int id);
    [Signal] public delegate void OfferReceivedEventHandler(int id, string offer);
    [Signal] public delegate void AnswerReceivedEventHandler(int id, string answer);
    [Signal] public delegate void CandidateReceivedEventHandler(int id, string mid, int index, string sdp);
    [Signal] public delegate void LobbySealedEventHandler();

    public void ConnectToUrl(string url)
    {
        Close();
        _code = 1000;
        _reason = "Unknown";
        _ws.ConnectToUrl(url);
    }

    public void Close()
    {
        _ws.Close();
    }

    public override void _Process(double delta)
    {
        _ws.Poll();
        var state = _ws.GetReadyState();

        if (state != _oldState && state == WebSocketPeer.State.Open && Autojoin)
        {
            JoinLobby(Lobby);
        }

        while (state == WebSocketPeer.State.Open && _ws.GetAvailablePacketCount() > 0)
        {
            if (!_ParseMsg())
            {
                GD.Print("Error parsing message from server.");
            }
        }

        if (state != _oldState && state == WebSocketPeer.State.Closed)
        {
            _code = _ws.GetCloseCode();
            _reason = _ws.GetCloseReason();
            EmitSignal(nameof(Disconnected));
        }

        _oldState = state;
    }

    private bool _ParseMsg()
    {
        string jsonPacket = _ws.GetPacket().GetStringFromUtf8();
		
		SocketMessage socketMessage;

        try
        {
            socketMessage = JsonSerializer.Deserialize<SocketMessage>(jsonPacket);
        }
        catch
        {
            return false;
        }

        string data = socketMessage.data;
        switch (socketMessage.type)
        {
            case Message.ID:
                EmitSignal(SignalName.Connected, socketMessage.id, data == "true");
                break;
            case Message.JOIN:
                EmitSignal(SignalName.LobbyJoined, data);
                break;
            case Message.SEAL:
                EmitSignal(SignalName.LobbySealed);
                break;
            case Message.PEER_CONNECT:
                EmitSignal(SignalName.PeerConnected, socketMessage.id);
                break;
            case Message.PEER_DISCONNECT:
                EmitSignal(nameof(PeerDisconnected), socketMessage.id);
                break;
            case Message.OFFER:
                EmitSignal(nameof(OfferReceived), socketMessage.id, data);
                break;
            case Message.ANSWER:
                EmitSignal(nameof(AnswerReceived), socketMessage.id, data);
                break;
            case Message.CANDIDATE:
                var candidate = data.Split('\n');
                if (candidate.Length != 3 || !int.TryParse(candidate[1], out int index))
                {
                    return false;
                }

                EmitSignal(nameof(CandidateReceived), socketMessage.id, candidate[0], index, candidate[2]);
                break;
            default:
                return false;
        }

        return true;
    }

    public Error JoinLobby(string lobby)
    {
        return _SendMsg((int)Message.JOIN, Mesh ? 0 : 1, lobby);
    }

    public Error SealLobby()
    {
        return _SendMsg((int)Message.SEAL, 0);
    }

    public Error SendCandidate(int id, string mid, int index, string sdp)
    {
        string candidateData = $"{mid}\n{index}\n{sdp}";
        return _SendMsg((int)Message.CANDIDATE, id, candidateData);
    }

    public Error SendOffer(int id, string offer)
    {
        return _SendMsg((int)Message.OFFER, id, offer);
    }

    public Error SendAnswer(int id, string answer)
    {
        return _SendMsg((int)Message.ANSWER, id, answer);
    }

    private Error _SendMsg(int type, int id, string data = "")
    {
        string message = JsonSerializer.Serialize(new
        {
            type,
            id,
            data
        });

        return _ws.SendText(message);
    }
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

