using Godot;
using System;

public partial class NetworkUi : Control
{
	private Multiplayer_Client Client => (Multiplayer_Client)GetNode<Node>("Client");
    private LineEdit Host => GetNode<LineEdit>("VBoxContainer/Connect/Host");
    private LineEdit Room => GetNode<LineEdit>("VBoxContainer/Connect/RoomSecret");
    private CheckBox Mesh => GetNode<CheckBox>("VBoxContainer/Connect/Mesh");

	private Button StartBtn => GetNode<Button>("VBoxContainer/Connect/Start");
	private Button StopBtn => GetNode<Button>("VBoxContainer/Connect/Stop");
	private Button StartGameBtn => GetNode<Button>("VBoxContainer/HBoxContainer/StartGame");

    [Export]
    public bool IsReadyToStart { get; private set; } = false;

    public override void _Ready()
    {
        // Connect client signals
        Client.LobbyJoined += OnLobbyJoined;
		Client.LobbySealed += OnLobbySealed;
		Client.Connected += OnConnected;
		Client.Disconnected += OnDisconnected;
        

        // Connect multiplayer signals
		Multiplayer.ConnectedToServer += OnMpServerConnected;
		Multiplayer.ConnectionFailed += OnMpServerDisconnected;
		Multiplayer.ServerDisconnected += OnMpServerDisconnected;
        Multiplayer.PeerConnected += OnMpPeerConnected;
		Multiplayer.PeerDisconnected += OnMpPeerDisconnected;

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void Ping(float argument)
    {
        Log($"[Multiplayer] Ping from peer {Multiplayer.GetRemoteSenderId()}: arg: {argument}");
    }

    private void OnMpServerConnected()
    {
        Log($"[Multiplayer] Server connected (I am lost)");
    }

    private void OnMpServerDisconnected()
    {
        Log($"[Multiplayer] Server disconnected (I am {Client.Get("rtc_mp").As<Node>().Get("unique_id")})");
    }

    private void OnMpPeerConnected(long id)
    {
        Log($"[Multiplayer] Peer {id} connected");
        
        if (Client.isAuthority()){
            StartGameBtn.Visible = true;
        }

    }

    private void OnMpPeerDisconnected(long id)
    {
        Log($"[Multiplayer] Peer {id} disconnected");
    }

    private void OnConnected(int id, bool useMesh)
    {
        Log($"[Signaling] Server connected with ID: {id}. Mesh: {useMesh}");
    }

    private void OnDisconnected()
    {
        Log($"[Signaling] Server disconnected: {Client.Get("code")}, Reason: {Client.Get("reason")}");
    }

    private void OnLobbyJoined(string lobby)
    {
        Log($"[Signaling] Joined lobby {lobby}");
    }

    private void OnLobbySealed()
    {
        Log("[Signaling] Lobby has been sealed");
    }

    private void Log(string msg)
    {
        GD.Print(msg);
        var textEdit = GetNode<TextEdit>("VBoxContainer/TextEdit");
        textEdit.Text += msg + "\n";
    }

    private void OnPeersPressed()
    {
        var peerNumbers = Multiplayer.GetPeers();
        
        Log($"[Multiplayer] Peers: {string.Join(", ", peerNumbers)}");
    }

    private void OnPingPressed()
    {
        Rpc(nameof(Ping), GD.Randf());
    }

    private void OnSealPressed()
    {
        Client.SealLobby();
    }

    private void OnStartPressed()
    {
        Client.Call("Start", Host.Text, Room.Text, Mesh.ButtonPressed);
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void StartGame(){
        IsReadyToStart = true;
        GD.Print("Game started");
        Log("[Signaling] Game started");
    }

    private void OnStartGamePressed()
    {
        Rpc(nameof(StartGame));
    }

    private void OnStopPressed()
    {
        Client.Call("Stop");
    }
}
