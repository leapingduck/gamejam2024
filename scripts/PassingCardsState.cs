using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PassingCardsState : IGameState
{
	
	private CardManager _cardManager;
	private Button _passButton;

	private Dictionary<int, bool> _playerPassStatus = new Dictionary<int, bool>();

	public override void Enter()
	{
		Console.WriteLine("Entering PassingCards state...");
		_cardManager = _gameManager.GetParent().GetNode<CardManager>("CardManager");
		_passButton = _gameManager.GetParent().GetNode<Button>("Temp/ReadyToPass");
		_cardManager.CardClicked += (Card card) => OnCardClicked(card);
		
		_passButton.Pressed += () => OnPassButtonClicked();
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
		
		// Add logic for passing cards between players.
	}

	
	public override void Execute()
	{
		if(_playerPassStatus.Values.All(x => x == true)){
			//TODO: Do the card passing here.
		}
		Console.WriteLine("Executing PassingCards state...");
		
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
		return _playerPassStatus.Values.All(x => x == true) ? GameState.PlayingTricks : null;
	}
}
