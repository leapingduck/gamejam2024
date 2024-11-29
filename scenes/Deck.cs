using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class Deck : Node2D
{
	private List<Card> cards = new List<Card>();
	private PackedScene cardScene = (PackedScene)GD.Load("res://scenes/card.tscn");

	private static Random _random = new Random();

	[Signal]
	public delegate void OnDealCardEventHandler(int peerId, Card card);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	public void GenDeck(){
		Rpc(MethodName.GenerateDeck);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void GenerateDeck(){
		int cardWidth = 256;
		int cardHeight = 356;

		cards.Clear();

		foreach(Suit suit in (Suit[])Enum.GetValues(typeof(Suit))){
			foreach(Rank rank in (Rank[])Enum.GetValues(typeof(Rank))){
				Card card = (Card)cardScene.Instantiate();
				card.Name = $"{rank} of {suit}";
				
				card.Position = GlobalPosition;

				int cardStart_X = (int)rank == 1 ? 0 : ((int)rank - 1) * cardWidth;
				int cardStart_Y = (int)suit == 1 ? 0 : ((int)suit - 1) * cardHeight;

				Rect2 faceRegion = new Rect2(cardStart_X, cardStart_Y, cardWidth, cardHeight);
				Rect2 backRegion = new Rect2(256, 356, cardWidth, cardHeight);
				
				card.Initialize(suit, rank, faceRegion, backRegion);
				cards.Add(card);
				
				GetParent().GetNode<CardManager>("CardManager").AddChild(card);
			}
		}
		
	}

	public bool IsEmpty(){
		return cards.Count == 0;
	}

	public Card DrawCard(){
		Card card = cards[0];
		cards.RemoveAt(0);
		return card;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void DealCard(int peerId, string cardName){
		Card card = cards.Find(c => c.Name == cardName);
		cards.Remove(card);

		EmitSignal(SignalName.OnDealCard, peerId, card);
	}

	public void DealCard(Hand hand){
		Card card = cards[0];// DrawCard();
		Rpc(MethodName.DealCard, hand.PlayerID, card.Name);
		//DealCard(hand.PlayerID, card.Name);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void DealCards(List<Hand> hands){
		int handIndex = 0;

		while(cards.Count > 0){
			if(hands[handIndex].PlayerID == 0){  
				handIndex++;
				if(handIndex >= hands.Count){
					handIndex = 0;
				}
			}
			DealCard(hands[handIndex]);
			handIndex++;
			if(handIndex >= hands.Count){
				handIndex = 0;
			}
			//TODO: Add a delay so the cards are dealt one at a time
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void ShuffleDeck(){
        int n = cards.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1); // Get a random index
            // Swap cards[i] with cards[j]
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
