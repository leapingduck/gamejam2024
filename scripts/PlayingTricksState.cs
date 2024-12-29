using Godot;
using System;
using System.Collections.Generic;

public partial class PlayingTricksState : IGameState
{
	private CardManager _cardManager;
	List<(int, Card)> _cardsPlayed = new();


	public override void Enter()
	{
		_cardManager = _gameManager.GetParent().GetNode<CardManager>("CardManager");

		_cardManager.CardClicked += (Card card) => OnCardClicked(card);
		_cardManager.CardDoubleClicked += (Card card) => OnCardDoubleClicked(card);
		Console.WriteLine("Entering PlayingTricks state...");
	}

	private void OnCardClicked(Card card)
	{
		Console.WriteLine($"Card {card.Name} clicked in PlayingTricks state");
		// Add logic for playing tricks in the game.
	}

	private void OnCardDoubleClicked(Card card)
	{
		Console.WriteLine($"Card {card.Name} double clicked in PlayingTricks state");
		// Add logic for playing tricks in the game.
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
