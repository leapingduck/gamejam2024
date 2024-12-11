using Godot;
using System;

public partial class GameOverState : IGameState
{
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
