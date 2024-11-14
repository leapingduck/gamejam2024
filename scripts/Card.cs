using Godot;
using System;

public partial class Card : Node2D
{	
 	public void Initialize(Suit suit, Rank rank, Rect2 faceRegion, Rect2 backRegion)
	{
		
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		((CardManager)GetParent()).ConnectCardSignals(this);
		var cardArea = GetNode<Area2D>("Area2D");
		cardArea.MouseEntered += FuckOff;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void FuckOff(){
		GD.Print("Mouseenter");
	}
  
}

