using Godot;
using System;

public partial class WaitingForPlayersState : IGameState
{
	
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
