using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Deck : Node2D
{
	private List<Card> cards = new List<Card>();
	private PackedScene cardScene = (PackedScene)GD.Load("res://scenes/card.tscn");
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GenerateDeck();
	}

	private void GenerateDeck(){
		int cardWidth = 256;
		int cardHeight = 356;
		
		Console.WriteLine("Generating deck");

		foreach(Suit suit in (Suit[])Enum.GetValues(typeof(Suit))){
			foreach(Rank rank in (Rank[])Enum.GetValues(typeof(Rank))){
				Card card = (Card)cardScene.Instantiate();
				
				Rect2 faceRegion = new Rect2((int)rank * cardWidth, (int)suit * cardHeight, cardWidth, cardHeight);
				Rect2 backRegion = new Rect2(256, 356, cardWidth, cardHeight);
				
				card.Initialize(suit, rank, faceRegion, backRegion);
				cards.Add(card);
				AddChild(card);
			}
		}
	}

	public void ShuffleDeck(){
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
