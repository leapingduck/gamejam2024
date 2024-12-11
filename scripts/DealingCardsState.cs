using Godot;
using System;

public partial class DealingCardsState : IGameState
{
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
