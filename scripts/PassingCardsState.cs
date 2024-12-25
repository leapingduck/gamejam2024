using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PassingCardsState : IGameState
{
	
	private CardManager _cardManager;
	private Button _passButton;

	private bool isPassing = false;
	private bool passComplete = false;

	private Dictionary<int, bool> _playerPassStatus = new Dictionary<int, bool>();

	public override void Enter()
	{
		Console.WriteLine("Entering PassingCards state...");
		passComplete = false;
		_cardManager = _gameManager.GetParent().GetNode<CardManager>("CardManager");
		_passButton = _gameManager.GetParent().GetNode<Button>("Temp/ReadyToPass");
		_cardManager.CardClicked += (Card card) => OnCardClicked(card);
		
		_passButton.Pressed += OnPassButtonClicked;
		_passButton.Disabled = true;
		_passButton.Text = $"Pass {_gameManager.CurrentPassPhase}";
		
		if(_gameManager.CurrentPassPhase != PassPhase.None){
			_passButton.Visible = true;
		}
		foreach(var peerId in _gameManager.peerId_PlayOrder){
			_playerPassStatus.Add(peerId, false);
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
		_passButton.Disabled = card.CurrentHand.Cards.Count(x => x.isSelected) != 3;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void NotifyClicked(bool isReadyToPass, int peerId){
		_playerPassStatus[peerId] = isReadyToPass;
		/*
		if(_playerPassStatus.Values.All(x => x)){
			_passButton.Disabled = false;
		}
		*/
	}

	private void OnPassButtonClicked()
	{

		Rpc(MethodName.NotifyClicked, _passButton.ButtonPressed, Multiplayer.GetUniqueId());
		// Add logic for passing cards between players.
	}

	//Find out why passing isn't appearing to work
	public override void Execute()
	{
		if(Multiplayer.GetUniqueId() == 1){
			Console.WriteLine("Executing PassingCards state on auth...");
			if(_playerPassStatus.Values.All(x => x == true) && !isPassing){
				isPassing = true;

				_gameManager._hands.ForEach(hand => {
					var selectedCards = hand.Cards.Where(x => x.isSelected).ToList();
					selectedCards.ForEach(card => {
						int targetPlayerId = PlayerIdToPassTo(hand.PlayerID);
						_cardManager.CallPassCardToPlayer(card.Name, targetPlayerId);
					});
				});
				isPassing = false;
				Rpc(MethodName.completePhase);
			}
		}
	}

	private int PlayerIdToPassTo(int playerId){
		int index = _gameManager.peerId_PlayOrder.IndexOf(playerId);
		int nextIndexSeed = _gameManager.CurrentPassPhase switch {
			PassPhase.Left => index + 1,
			PassPhase.Right => index - 1,
			PassPhase.Across => _gameManager.peerId_PlayOrder.Count == 4 ? index + 2 : index + 1,
			_ => index
		};
		int nextIndex = nextIndexSeed % _gameManager.peerId_PlayOrder.Count;
		return _gameManager.peerId_PlayOrder[nextIndex];
	}


	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void completePhase(){
		passComplete = true;
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting PassingCards state...");
		_cardManager.CardClicked -= (Card card) =>  OnCardClicked(card);
		_passButton.Pressed -= OnPassButtonClicked;
		_passButton.Visible = false;
		_passButton.ButtonPressed = false;
		
		var passPhases = Enum.GetValues(typeof(PassPhase)).Cast<PassPhase>().ToList();
		int currentIndex = passPhases.IndexOf(_gameManager.CurrentPassPhase);
		int nextIndex = (currentIndex + 1) % passPhases.Count;
		_gameManager.SetPassPhase(passPhases[nextIndex]);
	}

	public override GameState? CheckForTransition()
	{
		return passComplete ? GameState.PlayingTricks : null;
	}
}
