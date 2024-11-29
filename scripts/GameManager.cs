using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

public partial class GameManager : Node
{
	private readonly Dictionary<GameState, IGameState> _stateMap;
	private WebRtcClient _webRtcClient;
	private NetworkUi _networkUi;

	public List<Hand> _hands = new();

	public Deck _deck;

	public List<int> peerId_PlayOrder = new();

	public bool isAuthority(){
		return Multiplayer.GetUniqueId() == 1;
	}

	public GameManager()
	{
		_stateMap = new Dictionary<GameState, IGameState>
		{
			{ GameState.WaitingForPlayers, new WaitingForPlayersState(this) },
			{ GameState.DealingCards, new DealingCardsState(this) },
			{ GameState.PassingCards, new PassingCardsState(this) },
			{ GameState.PlayingTricks, new PlayingTricksState(this) },
			{ GameState.ScoringRound, new ScoringRoundState(this) },
			{ GameState.PickingModifiers, new PickingModifiersState(this) },
			{ GameState.GameOver, new GameOverState(this) }
		};
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_webRtcClient = GetParent().GetNode<WebRtcClient>("WebRTCClient");
		_networkUi = GetParent().GetNode<NetworkUi>("NetworkUI");
		_hands.Add(GetParent().GetNode<Hand>("Hand"));
		_hands.Add(GetParent().GetNode<Hand>("Player2Hand"));
		_hands.Add(GetParent().GetNode<Hand>("Player3Hand"));
		_hands.Add(GetParent().GetNode<Hand>("Player4Hand"));

		_deck = GetParent().GetNode<Deck>("Deck");
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

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void AssignHandsInOrder(int[] peers)
    {
		int myPeerId = Multiplayer.GetUniqueId();

		GD.Print($"Assigning hands in order for peer {myPeerId}");
		foreach(var peer in peers)
		{
			GD.Print($"Peer {peer}");
		}

		List<int> peerIds = new List<int>(peers);

        // Sort peerIds so they're always in a consistent order
        peerIds.Sort();
		
        // Find the index of the current client
        int myIndex = peerIds.IndexOf(myPeerId);

        // Assign the hands in a rotating manner
        for (int i = 0; i < peerIds.Count; i++)
        {
            int handIndex = (i - myIndex + peerIds.Count) % peerIds.Count; // Circular index
            _hands[handIndex].PlayerID = peerIds[i];
        }

        // Debug to ensure the hands are assigned correctly
        foreach (var entry in _hands)
        {
            GD.Print($"{entry.Name}: Peer ID {entry.PlayerID}");
        }
    }

	public void AssignHands(List<int> peerIds)
	{
		Rpc(MethodName.AssignHandsInOrder, peerIds.ToArray());
	}

	public void ConnectToServer()
	{
		_networkUi.Visible = true;
		//_webRtcClient.ConnectToUrl("ws://localhost:8888");
	}

	public void HideNetworkUi()
	{
		_networkUi.Visible = false;
		//_webRtcClient.HideNetworkUi();
	}

	public bool IsReadyToStart()
	{
		return _networkUi.IsReadyToStart;
	}

	private IGameState _currentState;

    public IGameState CurrentState => _currentState;

}

public class WaitingForPlayersState : IGameState
{
	private readonly GameManager _gameManager;

	public WaitingForPlayersState(GameManager gameManager)
	{
		_gameManager = gameManager;
			
	}

	public void Enter()
	{
		_gameManager.ConnectToServer();
		
		Console.WriteLine("Entering WaitingForPlayers state...");
	}

	public void Execute()
	{
		Console.WriteLine("Executing WaitingForPlayers state...");
		// Add logic for waiting for players to join.
	}

	public void Exit()
	{
		_gameManager.peerId_PlayOrder.Add(_gameManager.Multiplayer.GetUniqueId());
		_gameManager.peerId_PlayOrder.AddRange(_gameManager.Multiplayer.GetPeers());
		_gameManager.HideNetworkUi();
		Console.WriteLine("Exiting WaitingForPlayers state...");
	}

	public GameState? CheckForTransition()
	{
		if(_gameManager.IsReadyToStart()) {return GameState.DealingCards; }
		
		return null;
	}
}

public class DealingCardsState : IGameState
{
	private readonly GameManager _gameManager;

	public DealingCardsState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public void Enter()
	{
		if(_gameManager.isAuthority()){
			_gameManager.AssignHands(_gameManager.peerId_PlayOrder);
		}

		Console.WriteLine("Entering DealingCards state...");
	}

	public void Execute()
	{
		if(_gameManager._deck is null){
			Console.WriteLine("Deck is null");
			return;
		}
		
		if(_gameManager.isAuthority()){
			_gameManager._deck.GenDeck();
			_gameManager._deck.ShuffleDeck();
			_gameManager._deck.DealCards(_gameManager._hands);
		}
		
		Console.WriteLine("Executing DealingCards state...");
		// Add logic for dealing cards to players.
	}

	public void Exit()
	{
		Console.WriteLine("Exiting DealingCards state...");
	}

	public GameState? CheckForTransition()
	{
		if(_gameManager._deck.IsEmpty())
		{
			return GameState.PassingCards;
		}
		// Add logic to check for transition conditions.
		return null;
	}
}

public class PassingCardsState : IGameState
{
	private readonly GameManager _gameManager;
	private CardManager _cardManager;

	public PassingCardsState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public void Enter()
	{
		_cardManager = _gameManager.GetParent().GetNode<CardManager>("CardManager");
		Console.WriteLine("Entering PassingCards state...");
		_cardManager.CardClicked += (Card card) =>  OnCardClicked(card);
	}

	private void OnCardClicked(Card card)
	{
		//Select the card to be passed and highlight it if 3 cards selected then pass and mark as passed
		// Add logic for passing cards between players.
	}

	public void Execute()
	{
		Console.WriteLine("Executing PassingCards state...");
		// Add logic for passing cards between players.
	}

	public void Exit()
	{
		Console.WriteLine("Exiting PassingCards state...");
		_cardManager.CardClicked -= (Card card) =>  OnCardClicked(card);
	}

	public GameState? CheckForTransition()
	{
		//Once all players have passed cards then move to the next state
		return null;
	}
}

public class PlayingTricksState : IGameState
{
	private readonly GameManager _gameManager;

	public PlayingTricksState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public void Enter()
	{
		Console.WriteLine("Entering PlayingTricks state...");
	}

	public void Execute()
	{
		Console.WriteLine("Executing PlayingTricks state...");
		// Add logic for playing tricks in the game.
	}

	public void Exit()
	{
		Console.WriteLine("Exiting PlayingTricks state...");
	}

	public GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public class ScoringRoundState : IGameState
{
	private readonly GameManager _gameManager;

	public ScoringRoundState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public void Enter()
	{
		Console.WriteLine("Entering ScoringRound state...");
	}

	public void Execute()
	{
		Console.WriteLine("Executing ScoringRound state...");
		// Add logic for scoring the round.
	}

	public void Exit()
	{
		Console.WriteLine("Exiting ScoringRound state...");
	}

	public GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public class PickingModifiersState : IGameState
{
	private readonly GameManager _gameManager;

	public PickingModifiersState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public void Enter()
	{
		Console.WriteLine("Entering PickingModifiers state...");
	}

	public void Execute()
	{
		Console.WriteLine("Executing PickingModifiers state...");
		// Add logic for picking modifiers for the game.
	}

	public void Exit()
	{
		Console.WriteLine("Exiting PickingModifiers state...");
	}

	public GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public class GameOverState : IGameState
{
	private readonly GameManager _gameManager;

	public GameOverState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public void Enter()
	{
		Console.WriteLine("Entering GameOver state...");
	}

	public void Execute()
	{
		Console.WriteLine("Executing GameOver state...");
		// Add logic for ending the game.
	}

	public void Exit()
	{
		Console.WriteLine("Exiting GameOver state...");
	}

	public GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public enum PassPhase
{
	Left,
	Right,
	Across,
	None
}