using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;

public partial class GameManager : Node
{
	private Dictionary<GameState, IGameState> _stateMap;
	private WebRtcClient _webRtcClient;
	private NetworkUi _networkUi;
	private CardManager _cardManager;

	public List<Hand> _hands = new();

	public Deck _deck;

	public List<int> peerId_PlayOrder = new();

	private PassPhase _currentPassPhase = PassPhase.Left;

	public PassPhase CurrentPassPhase => _currentPassPhase;
	public void SetPassPhase(PassPhase passPhase) => _currentPassPhase = passPhase;

	public GameManager()
	{
		
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_stateMap = new Dictionary<GameState, IGameState>
		{
			{ GameState.WaitingForPlayers, (IGameState)GetNode<Node>("WaitingForPlayers") },
			{ GameState.DealingCards, (IGameState)GetNode<Node>("DealingCards") },
			{ GameState.PassingCards, (IGameState)GetNode<Node>("PassingCards") },
			{ GameState.PlayingTricks, (IGameState)GetNode<Node>("PlayingTricks") },
			{ GameState.ScoringRound, (IGameState)GetNode<Node>("ScoringRound") },
			{ GameState.PickingModifiers, (IGameState)GetNode<Node>("PickingModifiers") },
			{ GameState.GameOver, (IGameState)GetNode<Node>("GameOver") }
		};

		_webRtcClient = GetParent().GetNode<WebRtcClient>("WebRTCClient");
		_networkUi = GetParent().GetNode<NetworkUi>("NetworkUI");
		_hands.Add(GetParent().GetNode<Hand>("Hand"));
		_hands.Add(GetParent().GetNode<Hand>("Player2Hand"));
		_hands.Add(GetParent().GetNode<Hand>("Player3Hand"));
		_hands.Add(GetParent().GetNode<Hand>("Player4Hand"));

		_deck = GetParent().GetNode<Deck>("Deck");
		_cardManager = GetParent().GetNode<CardManager>("CardManager");

		_currentState = _stateMap[GameState.WaitingForPlayers];
		_currentState.Enter();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_currentState.Execute();
		var transitionResult = _currentState.CheckForTransition();
		if(transitionResult.HasValue)
		{
			_currentState.Exit();
			_currentState = _stateMap[transitionResult.Value];
			_currentState.Enter();
		}
	}

	private IGameState _currentState;

    public IGameState CurrentState => _currentState;

}
