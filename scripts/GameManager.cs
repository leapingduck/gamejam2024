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

	private Button _passButton;

	public List<Hand> _hands = new();

	public Deck _deck;

	public List<int> peerId_PlayOrder = new();

	public bool isAuthority(){
		return Multiplayer.GetUniqueId() == 1;
	}

	private PassPhase _currentPassPhase = PassPhase.Left;

	public PassPhase CurrentPassPhase => _currentPassPhase;
	public void SetPassPhase(PassPhase passPhase) => _currentPassPhase = passPhase;

	public GameManager()
	{
		/*
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
		*/
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

		_passButton = GetParent().GetNode<Button>("Temp/ReadyToPass");

		_deck = GetParent().GetNode<Deck>("Deck");
		_cardManager = GetParent().GetNode<CardManager>("CardManager");
		//_cardManager.CardClicked += OnCardClicked;

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

		

		List<int> peerIds = new List<int>(peers);

        // Sort peerIds so they're always in a consistent order
        peerIds.Sort();
		
        // Find the index of the current client
        int myIndex = peerIds.IndexOf(myPeerId);

		//If only two select hand across from each other.
		if(peerIds.Count == 2){
			_hands.Find(h => h.handPosition == HandPosition.Bottom).PlayerID = peerIds[myIndex];
			_hands.Find(h => h.handPosition == HandPosition.Top).PlayerID = peerIds.FirstOrDefault(x => x != peerIds[myIndex]);
			return;
		}

        // Assign the hands in a rotating manner
        for (int i = 0; i < peerIds.Count; i++)
        {
            int handIndex = (i - myIndex + peerIds.Count) % peerIds.Count; // Circular index
            _hands[handIndex].PlayerID = peerIds[i];
        }
       
    }

	public void AssignHands(List<int> peerIds)
	{
		Rpc(MethodName.AssignHandsInOrder, peerIds.ToArray());
	}

	public void ConnectToServer()
	{
		_networkUi.Visible = true;
	}

	public void HideNetworkUi()
	{
		_networkUi.Visible = false;
	}

	public bool IsReadyToStart()
	{
		return _networkUi.IsReadyToStart;
	}

	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void CardClicked(string cardName, bool isSelected, int peerId){
		//TODO: Figure this out.
		GD.Print($"Card {cardName} clicked by peer {peerId} in gamemanager");
		_cardManager.selectCard(cardName, isSelected);		
	}

	private void OnCardClicked(Card card)
	{
		GD.Print($"Reporting Card {card.Name} clicked");
		Rpc(MethodName.CardClicked, card.Name, !card.isSelected, Multiplayer.GetUniqueId());		
	}

	private IGameState _currentState;

    public IGameState CurrentState => _currentState;

}
/*
public partial class WaitingForPlayersState : IGameState
{
	private readonly GameManager _gameManager;

	public WaitingForPlayersState(GameManager gameManager)
	{
		_gameManager = gameManager;
			
	}

	public override void Enter()
	{
		_gameManager.ConnectToServer();
		
		Console.WriteLine("Entering WaitingForPlayers state...");
	}

	public override void Execute()
	{
		Console.WriteLine("Executing WaitingForPlayers state...");
		// Add logic for waiting for players to join.
	}

	public override void Exit()
	{
		_gameManager.peerId_PlayOrder.Add(_gameManager.Multiplayer.GetUniqueId());
		_gameManager.peerId_PlayOrder.AddRange(_gameManager.Multiplayer.GetPeers());
		_gameManager.HideNetworkUi();
		Console.WriteLine("Exiting WaitingForPlayers state...");
	}

	public override GameState? CheckForTransition()
	{
		if(_gameManager.IsReadyToStart()) {return GameState.DealingCards; }
		
		return null;
	}
}
*/
/*
public partial class DealingCardsState : IGameState
{
	private readonly GameManager _gameManager;

	public DealingCardsState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public override void Enter()
	{
		if(_gameManager.isAuthority()){
			_gameManager.AssignHands(_gameManager.peerId_PlayOrder);
		}

		Console.WriteLine("Entering DealingCards state...");
	}

	public override void Execute()
	{
		if(_gameManager._deck is null){
			Console.WriteLine("Deck is null");
			return;
		}
		
		if(_gameManager.isAuthority()){
			_gameManager._deck.GenDeck();
			_gameManager._deck.ShuffleDeck();
			_gameManager._deck.ShuffleDeck();
			_gameManager._deck.DealCards(_gameManager._hands);
		}
		
		Console.WriteLine("Executing DealingCards state...");
		// Add logic for dealing cards to players.
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting DealingCards state...");
	}

	public override GameState? CheckForTransition()
	{
		if(_gameManager._deck.IsEmpty())
		{
			return GameState.PassingCards;
		}
		// Add logic to check for transition conditions.
		return null;
	}
}


public partial class PassingCardsState : IGameState
{
	private readonly GameManager _gameManager;
	private CardManager _cardManager;
	private Button _passButton;

	public PassingCardsState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public override void Enter()
	{
		Console.WriteLine("Entering PassingCards state...");
		_cardManager = _gameManager.GetParent().GetNode<CardManager>("CardManager");
		_passButton = _gameManager.GetParent().GetNode<Button>("Temp/ReadyToPass");
		_cardManager.CardClicked += (Card card) =>  OnCardClicked(card);
		
		_passButton.Pressed += () => OnPassButtonClicked();
		_passButton.Disabled = true;
		_passButton.Text = $"Pass {_gameManager.CurrentPassPhase}";
		
		if(_gameManager.CurrentPassPhase != PassPhase.None){
			_passButton.Visible = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void CardClicked(string cardName, bool isSelected, int peerId){
		//TODO: Figure this out.
		GD.Print($"Card {cardName} clicked by peer {peerId} in pass state");
		_cardManager.selectCard(cardName, isSelected);		
	}

	private void OnCardClicked(Card card)
	{
		GD.Print($"Reporting Card {card.Name} clicked from State");
		Rpc(MethodName.CardClicked, card.Name, !card.isSelected, Multiplayer.GetUniqueId());		
	}

	private void OnPassButtonClicked()
	{
		
		// Add logic for passing cards between players.
	}

	
	public override void Execute()
	{
		Console.WriteLine("Executing PassingCards state...");
		// Add logic for passing cards between players.
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting PassingCards state...");
		_cardManager.CardClicked -= (Card card) =>  OnCardClicked(card);
		_passButton.Pressed -= () => OnPassButtonClicked();
		_passButton.Visible = false;
		
		var passPhases = Enum.GetValues(typeof(PassPhase)).Cast<PassPhase>().ToList();
		int currentIndex = passPhases.IndexOf(_gameManager.CurrentPassPhase);
		int nextIndex = (currentIndex + 1) % passPhases.Count;
		_gameManager.SetPassPhase(passPhases[nextIndex]);

	}

	public override GameState? CheckForTransition()
	{
		//Once all players have passed cards then move to the next state


		return null;
	}
}

public partial class PlayingTricksState : IGameState
{
	private readonly GameManager _gameManager;

	public PlayingTricksState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public override void Enter()
	{
		Console.WriteLine("Entering PlayingTricks state...");
	}

	public override void Execute()
	{
		Console.WriteLine("Executing PlayingTricks state...");
		// Add logic for playing tricks in the game.
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting PlayingTricks state...");
	}

	public override GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public partial class ScoringRoundState : IGameState
{
	private readonly GameManager _gameManager;

	public ScoringRoundState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public override void Enter()
	{
		Console.WriteLine("Entering ScoringRound state...");
	}

	public override void Execute()
	{
		Console.WriteLine("Executing ScoringRound state...");
		// Add logic for scoring the round.
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting ScoringRound state...");
	}

	public override GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public partial class PickingModifiersState : IGameState
{
	private readonly GameManager _gameManager;

	public PickingModifiersState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public override void Enter()
	{
		Console.WriteLine("Entering PickingModifiers state...");
	}

	public override void Execute()
	{
		Console.WriteLine("Executing PickingModifiers state...");
		// Add logic for picking modifiers for the game.
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting PickingModifiers state...");
	}

	public override GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}

public partial class GameOverState : IGameState
{
	private readonly GameManager _gameManager;

	public GameOverState(GameManager gameManager)
	{
		_gameManager = gameManager;
	}

	public override void Enter()
	{
		Console.WriteLine("Entering GameOver state...");
	}

	public override void Execute()
	{
		Console.WriteLine("Executing GameOver state...");
		// Add logic for ending the game.
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting GameOver state...");
	}

	public override GameState? CheckForTransition()
	{
		// Add logic to check for transition conditions.
		return null;
	}
}
*/
