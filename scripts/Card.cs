using System;
using Godot;

public partial class Card : Node2D
{	
	public Suit Suit { get; set; }
	public Rank Rank { get; set; }

	private Sprite2D CardFace = null;
	private Sprite2D borderSprite = null;

	public Vector2 HandPosition { get; set; } 
	public float HandRotation { get; set; }

	private Hand _currentHand = null;
	public Hand CurrentHand => _currentHand;

	public bool isSelected = false;

	public void SetHand(Hand hand){ _currentHand = hand; }

 	public void Initialize(Suit suit, Rank rank, Rect2 faceRegion, Rect2 backRegion)
	{
		this.Suit = suit;
		this.Rank = rank;

		CardFace = GetNode<Sprite2D>("CardFace");
		CardFace.RegionRect = faceRegion;

		borderSprite = new Sprite2D();
        borderSprite.Texture = CardFace.Texture;
        borderSprite.SelfModulate = new Color(0, 0, 0); // Black color
        borderSprite.Scale = new Vector2(1.05f, 1.05f); // Slightly larger scale
		borderSprite.RegionEnabled = true;
		borderSprite.RegionRect = faceRegion;
        borderSprite.ZIndex = -1; // Ensure it's rendered behind
		borderSprite.Position = CardFace.Position;
		borderSprite.ZAsRelative = true;
        AddChild(borderSprite);

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
