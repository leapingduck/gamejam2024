using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PassingCardsState : IGameState
{
	
	private CardManager _cardManager;
	private Button _passButton;

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
