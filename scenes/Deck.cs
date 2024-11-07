using Godot;
using System;

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
		foreach(Suit suit in (Suit[])Enum.GetValues(typeof(Suit))){
			foreach(Rank rank in (Rank[])Enum.GetValues(typeof(Rank))){
				cards.Add(new Card(suit, rank));
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
