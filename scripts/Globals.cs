using Godot;
using System;
 
public enum Suit
{
	Clubs = 1,
	Hearts,
	Spades,
	Diamonds
}

public enum Rank
{
	Ace = 1,
	Two = 2,
	Three = 3,
	Four = 4,
	Five = 5,
	Six = 6,
	Seven = 7,
	Eight = 8,
	Nine = 9,
	Ten = 10,
	Jack = 11,
	Queen = 12,
	King = 13
}

public enum CollisionMask
{
	Card = 1,
	CardSlot = 2,
	Deck = 3
}

public enum GameState
{
    WaitingForPlayers,
    DealingCards,
    PassingCards,
    PlayingTricks,
    ScoringRound,
	PickingModifiers,
    GameOver
}

public interface IGameState{
	void Enter();
	void Execute();
	void Exit();
	GameState? CheckForTransition();
}
