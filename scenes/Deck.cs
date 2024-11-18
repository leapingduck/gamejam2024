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

		foreach(Suit suit in (Suit[])Enum.GetValues(typeof(Suit))){
			foreach(Rank rank in (Rank[])Enum.GetValues(typeof(Rank))){
				Card card = (Card)cardScene.Instantiate();
				card.Name = $"{rank} of {suit}";
				card.Position = GlobalPosition;
				GetParent().GetNode<CardManager>("CardManager").AddChild(card);

				Rect2 faceRegion = new Rect2((int)rank * cardWidth, (int)suit * cardHeight, cardWidth, cardHeight);
				Rect2 backRegion = new Rect2(256, 356, cardWidth, cardHeight);
				
				card.Initialize(suit, rank, faceRegion, backRegion);
				cards.Add(card);
			}
		}
		
	}

	public Card DrawCard(){
		Card card = cards[0];
		cards.RemoveAt(0);
		return card;
	}

	public void DealCard(Hand hand){
		Card card = DrawCard();
		hand.AddCardToHand(card);
	}

	public void ShuffleDeck(){
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
