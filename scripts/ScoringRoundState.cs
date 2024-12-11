using Godot;
using System;

public partial class ScoringRoundState : IGameState
{
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
