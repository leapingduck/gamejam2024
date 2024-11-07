using Godot;
using System;

public partial class Card : CharacterBody2D
{
	public Suit Suit {get; set;}
	public Rank Rank {get; set;}
	private bool isFaceUp = false;
	private Rect2 faceRegion;
	private Rect2 backRegion;
	private Sprite2D faceSprite;
	private Sprite2D backSprite;
	
	public void Initialize(Suite suit, Rank rank, Rect2 faceRegion, Rect2 backRegion){
		Suit = suit;
		Rank = rank;
		faceSprite.RegionRect = faceRegion;
		backSprite.RegionRect = backRegion;
		this.faceRegion = faceRegsion;
		this.backRegion = backRegion;
		//TODO: Finish adding card and add deck
	}
	
	pubilic void ShowBack(){
		//faceSprite.RegionRect = backRegion;
		faceSprite.RegionEnabled = true;
	}
	
	public void ShowFace(){
		backSprite.RegionEnabled = false;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		faceSprite = GetNode<Sprite2D>("CardFaceImage");
		backSprite = GetNode<Sprite2D>("CardBackImage"); 
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
