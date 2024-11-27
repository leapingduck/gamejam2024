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


/*
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

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

public partial class WebRtcClient : Node
{
	
	
	public string LobbyName = "Testlobby"; //Make this settable via UI

	private WebSocketPeer _webSocketPeer = new WebSocketPeer();
	private WebRtcPeerConnection _peerConnection;
	private WebRtcMultiplayerPeer _rtcPeer = new WebRtcMultiplayerPeer();

	private Dictionary<int, WebRtcDataChannel> _dataChannels = new Dictionary<int, WebRtcDataChannel>();

	private bool isSealed = false;

	private WebSocketPeer.State _oldState = WebSocketPeer.State.Closed;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.ConnectedToServer += RTCConnectedToServer;
		Multiplayer.PeerConnected += RTCPeerConnected;
		Multiplayer.PeerDisconnected += RTCPeerDisconnected;

		_webSocketPeer.ConnectToUrl("ws://localhost:8888");
	}

    private void RTCPeerDisconnected(long id)
    {
        GD.Print("Peer disconnected");
    }

    private void RTCPeerConnected(long id)
    {
        GD.Print("Peer connected");
    }

    private void RTCConnectedToServer()
    {
        GD.Print("Connected to server");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		_webSocketPeer.Poll();
		var state = _webSocketPeer.GetReadyState();
		if(state != _oldState && state == WebSocketPeer.State.Open)	
		{
			GD.Print("Connection opened");
			//var myid = Multiplayer.GetUniqueId();
			//SendMessage(Message.ID, myid, "Hello");
		}
		while(state == WebSocketPeer.State.Open && _webSocketPeer.GetAvailablePacketCount() > 0)
		{
			var message = _webSocketPeer.GetPacket();
			var messageString = System.Text.Encoding.UTF8.GetString(message);
			if(!ParseMessage(messageString))
			{
				Console.WriteLine($"Error parsing message {messageString}");
			}
		}
		if(state != _oldState && state == WebSocketPeer.State.Closed)
		{
			Console.WriteLine("Connection closed");	
		}
		_oldState = state;

	}

    public void SetupWebRTC()
    {
        _peerConnection = new WebRtcPeerConnection();
		
		int id = 1;

		_peerConnection.Initialize(new Godot.Collections.Dictionary {
			["iceServers"] = new Godot.Collections.Array {
				new Godot.Collections.Dictionary {
					["urls"] = new Godot.Collections.Array {
						"stun:stun.l.google.com:19302"
					}
				}
			}
		});

		Godot.Collections.Dictionary config = new Godot.Collections.Dictionary {
			["negotiated"] = true,
			["id"] = 1,
			["ordered"] = true
		};

		var dataChannel = _peerConnection.CreateDataChannel("dataChannel", config);
		GD.Print(dataChannel);
		
		
		_dataChannels[dataChannel.GetId()] = dataChannel;

		_peerConnection.IceCandidateCreated += (name, index, candidate) => {
			GD.Print($"IceCandidateCreated: {name} {index} {candidate}");
			SendMessage(Message.CANDIDATE, 0, candidate);
		};

		_peerConnection.SessionDescriptionCreated += (type, sdp) => OnSessionDescriptionCreated(type, sdp);

		_rtcPeer.CreateMesh(id);

		//GD.Print("Created peer connection");
		_rtcPeer.AddPeer(_peerConnection, id);

		var error = _peerConnection.CreateOffer();
		GD.Print($"CreateOffer error: {error}");

    }

    private void OnSessionDescriptionCreated(string type, string sdp)
    {
        GD.Print($"SessionDescriptionCreated: {type} - {sdp}");
			if(type == "offer"){ 
				_peerConnection.SetLocalDescription(type, sdp);
				SendMessage(Message.OFFER, 0, sdp);
			}
			else if(type == "answer"){ 
				_peerConnection.SetLocalDescription(type, sdp);
				SendMessage(Message.ANSWER, 0, sdp);
			}
    }

    private void OnDataChannelOpen()
	{
		GD.Print("Data channel open");
	}

	public void OnDataChannelClosed(){
		GD.Print("Data channel closed");
	}

	public void OnDataChannelDataReceived(string data){
		GD.Print($"Data channel received: {data}");
	}


    private bool ParseMessage(string message)
	{
		GD.Print($"Received message: {message}");
		Console.WriteLine($"Received message: {message}");
		
		SocketMessage socketMessage;

		try{
			socketMessage = JsonSerializer.Deserialize<SocketMessage>(message);
		} catch(Exception ex){
			GD.Print($"Error deserializing message ({message}): {ex.Message}");
			Console.WriteLine($"Error deserializing message ({message}): {ex.Message}");
			return false;
		}

		if(socketMessage is null){
			GD.Print("Error deserializing message");
			Console.WriteLine("Error deserializing message");
			return false;
		}

		GD.Print($"Received message type: {socketMessage.type}");

		switch(socketMessage.type){
			case Message.ID:
				GD.Print($"Received ID: {socketMessage.id}");
				OnConnected(socketMessage.id, socketMessage.data);
				break;
			case Message.PEER_CONNECT:
				OnPeerConnected(socketMessage.id);
				break;
			case Message.PEER_DISCONNECT:
				OnPeerDisconnected(socketMessage.id);
				break;
			case Message.OFFER:
				OnOffer(socketMessage.id, socketMessage.data);
				break;
			case Message.ANSWER:
				OnAnswer(socketMessage.id, socketMessage.data);
				break;
			case Message.CANDIDATE:
				OnCandidate(socketMessage.id, socketMessage.data);
				break;
			case Message.SEAL:
				isSealed = true;
				break;
			default:
				Console.WriteLine($"Unknown message type: {socketMessage.type}");
				break;
		}



		return true;
	}

    private void OnCandidate(int id, string data)
    {
        throw new NotImplementedException();
    }

    private void OnAnswer(int id, string data)
    {
        throw new NotImplementedException();
    }

    private void OnOffer(int id, string data)
    {
        GD.Print($"Received offer from {id}: {data}");

		_peerConnection.SetRemoteDescription("offer", data);
    }

    private void OnPeerDisconnected(int id)
    {
        throw new NotImplementedException();
    }

    private void SendMessage(Message message, int id, string data)
	{
		var messageObject = new { type = message, id = id, data = data };
		var messageJson = JsonSerializer.Serialize(messageObject);
		if(_webSocketPeer.GetReadyState() != WebSocketPeer.State.Open){
			GD.Print("WebSocket not open!");
		}
		
		_webSocketPeer.SendText(messageJson);
	}

	public void JoinLobby(){
		SendMessage(Message.JOIN, 0, LobbyName);
	}

	private void OnConnected(int id, string data)
	{
		GD.Print($"Connected with id: {id} - {data}");
		Console.WriteLine($"Connected with id: {id} - {data}");
		_rtcPeer.CreateMesh(id);
		
		Multiplayer.MultiplayerPeer = _rtcPeer;
	}
	private void OnPeerConnected(int id){
		Console.WriteLine($"Peer connected: {id}");
		//CreatePeerConnection(id);
	}
	
/*
	private WebRtcPeerConnection CreatePeerConnection(int id){
		var peerConnection = new WebRtcPeerConnection();
		
		Godot.Collections.Dictionary config = new Godot.Collections.Dictionary {
			["iceServers"] = new Godot.Collections.Array {
				new Godot.Collections.Dictionary {
				["urls"] = new Godot.Collections.Array {
						"stun:stun.l.google.com:19302"
					}
				}
			}
		};
		
		peerConnection.SessionDescriptionCreated += (type, sdp) => {
			GD.Print($"SessionDescriptionCreated: {type} - {sdp} - {id}");
		};

		peerConnection.IceCandidateCreated += (name, index, candidate) => {
			GD.Print($"IceCandidateCreated: {name} {index} {candidate}");
		};

		peerConnection.Initialize(config);
		_rtcPeer.AddPeer(peerConnection, id);
		if(id < _rtcPeer.GetUniqueId()){
			peerConnection.CreateOffer();
		}

		return peerConnection;
	}



}
*/

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

