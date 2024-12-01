using System;
using Godot;

public partial class Card : Node2D
{	
	public Suit Suit { get; set; }
	public Rank Rank { get; set; }

	public Vector2 HandPosition { get; set; } 
	public float HandRotation { get; set; }

	private Hand _currentHand = null;
	public Hand CurrentHand => _currentHand;

	public bool isSelected = false;

	public void SetHand(Hand hand){ _currentHand = hand; }

 	public void Initialize(Suit suit, Rank rank, Rect2 faceRegion, Rect2 backRegion)
	{
		GetNode<Sprite2D>("CardFace").RegionRect = faceRegion;
		this.Suit = suit;
		this.Rank = rank;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		((CardManager)GetParent()).ConnectCardSignals(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
