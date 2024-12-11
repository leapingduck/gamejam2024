using Godot;
using System;

public partial class IGameState : Node
{
	[Export]
	public GameManager _gameManager;

	public virtual void Enter(){}
	public virtual void Execute(){}
	public virtual void Exit(){}
	public virtual GameState? CheckForTransition(){return null;}
}
