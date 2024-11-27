using Godot;
using System;

public partial class Multiplayer_Client : WebRtcClient
{
	private WebRtcMultiplayerPeer rtcMp = new WebRtcMultiplayerPeer();
	
    private bool sealedLobby = false;

    public override void _Ready()
    {
		Connected += OnConnected;
		Disconnected += OnDisconnected;
        
		OfferReceived += OnOfferReceived;
		AnswerReceived += OnAnswerReceived;
		CandidateReceived += OnCandidateReceived;

		LobbyJoined += OnLobbyJoined;
		LobbySealed += OnLobbySealed;
		PeerConnected += OnPeerConnected;
		PeerDisconnected += OnPeerDisconnected;
    }

    public void Start(string url, string lobby = "", bool mesh = true)
    {
        Stop();
        sealedLobby = false;
        Mesh = mesh;
        Lobby = lobby;
        ConnectToUrl(url);
    }

    public void Stop()
    {
        Multiplayer.MultiplayerPeer = null;
        rtcMp.Close();
        Close();
    }

    public bool isAuthority(){
        return rtcMp.GetUniqueId() == 1;
    }

    private WebRtcPeerConnection CreatePeer(int id)
    {
        var peer = new WebRtcPeerConnection();

		peer.Initialize(new Godot.Collections.Dictionary {
			["iceServers"] = new Godot.Collections.Array {
				new Godot.Collections.Dictionary {
					["urls"] = new Godot.Collections.Array {
						"stun:stun.l.google.com:19302"
					}
				}
			}
		});

        // Use a public STUN server for moderate NAT traversal.
		/*
        peer.Initialize(new Dictionary
        {
            { "iceServers", new Array { new Dictionary { { "urls", new Array { "stun:stun.l.google.com:19302" } } } } }
        });
		*/
        peer.SessionDescriptionCreated += (type, data) => OnOfferCreated(type, data, id);
        peer.IceCandidateCreated += (mid, index, sdp) => OnNewIceCandidate(mid, (int)index, sdp, id);

        rtcMp.AddPeer(peer, id);

        if (id < rtcMp.GetUniqueId())
        {
            // So lobby creator never creates offers.
            peer.CreateOffer();
        }

        return peer;
    }

    private void OnNewIceCandidate(string midName, int indexName, string sdpName, int id)
    {
        SendCandidate(id, midName, indexName, sdpName);
    }

    private void OnOfferCreated(string type, string data, int id)
    {
        if (!rtcMp.HasPeer(id))
            return;

        GD.Print($"Created {type}");
        var peers = rtcMp.GetPeers();
        Console.WriteLine($"Looking for peer id {id}");
        Console.WriteLine($"Our peers are: {string.Join(", ", peers)}");
        var connection = rtcMp.GetPeer(id);
        //TODO: Why does it error here?
        ((WebRtcPeerConnection)connection["connection"]).SetLocalDescription(type, data);

        if (type == "offer")
        {
            SendOffer(id, data);
        }
        else
        {
            SendAnswer(id, data);
        }
    }

    private void OnConnected(int id, bool useMesh)
    {
        GD.Print($"Connected {id}, mesh: {useMesh}");
        Console.WriteLine($"Connected {id}, mesh: {useMesh} is the message we're hoping to get.");

        if (useMesh)
        {
            rtcMp.CreateMesh(id);
            Console.WriteLine("Creating mesh");
        }
        else if (id == 1)
        {
            rtcMp.CreateServer();
            Console.WriteLine("Creating server");
        }
        else
        {
            rtcMp.CreateClient(id);
            Console.WriteLine("Creating client");
        }

        Multiplayer.MultiplayerPeer = rtcMp;
    }

    private void OnLobbyJoined(string lobby)
    {
        Console.WriteLine($"Joined lobby: {lobby}");
        Lobby = lobby;
    }

    private void OnLobbySealed()
    {
        sealedLobby = true;
    }

    private void OnDisconnected()
    {
        GD.Print($"Disconnected");

        if (!sealedLobby)
        {
            Stop(); // Unexpected disconnect
        }
    }

    private void OnPeerConnected(int id)
    {
        GD.Print($"Peer connected: {id}");
        CreatePeer(id);
    }

    private void OnPeerDisconnected(int id)
    {
        if (rtcMp.HasPeer(id))
        {
            rtcMp.RemovePeer(id);
        }
    }

    private void OnOfferReceived(int id, string offer)
    {
        GD.Print($"Got offer: {id}");
        if (rtcMp.HasPeer(id))
        {
            ((WebRtcPeerConnection)rtcMp.GetPeer(id)["connection"]).SetRemoteDescription("offer", offer);
        }
    }

    private void OnAnswerReceived(int id, string answer)
    {
        GD.Print($"Got answer: {id}");
        if (rtcMp.HasPeer(id))
        {
            ((WebRtcPeerConnection)rtcMp.GetPeer(id)["connection"]).SetRemoteDescription("answer", answer);
        }
    }

    private void OnCandidateReceived(int id, string mid, int index, string sdp)
    {
        if (rtcMp.HasPeer(id))
        {
            ((WebRtcPeerConnection)rtcMp.GetPeer(id)["connection"]).AddIceCandidate(mid, index, sdp);
        }
    }
}
