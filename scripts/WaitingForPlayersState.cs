using Godot;
using System;

public partial class WaitingForPlayersState : IGameState
{
	private NetworkUi _networkUi;

	public override void Enter()
	{
		_networkUi = _gameManager.GetParent().GetNode<NetworkUi>("NetworkUi");
		_networkUi.Visible = true;
		
		Console.WriteLine("Entering WaitingForPlayers state...");
	}

	public override void Execute()
	{
		Console.WriteLine("Executing WaitingForPlayers state...");
		// Add logic for waiting for players to join.
	}

	public override void Exit()
	{
		_gameManager.peerId_PlayOrder.Add(Multiplayer.GetUniqueId());
		_gameManager.peerId_PlayOrder.AddRange(Multiplayer.GetPeers());
		_networkUi.Visible = false;
		Console.WriteLine("Exiting WaitingForPlayers state...");
	}

	public override GameState? CheckForTransition()
	{
		if(_networkUi.IsReadyToStart) {return GameState.DealingCards; }
		
		return null;
	}
}
