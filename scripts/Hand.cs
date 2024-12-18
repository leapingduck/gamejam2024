using System;
using System.Collections.Generic;
using Godot;

public partial class Hand : Node2D
{
	[Export]
	public bool isLocalPlayer = false;

	[Export]
	public HandPosition handPosition = HandPosition.Bottom;

	public int PlayerID = 0;
	
	private PackedScene CARD = (PackedScene)GD.Load("res://scenes/card.tscn");
	private int HAND_COUNT = 5;

	[Export] public Curve HandCurve { get; set; } = new();
	[Export] public Curve RotationCurve { get; set; } = new();
	
	[Export] public float MaxRotationDegrees { get; set; } = 5;
	[Export] public float XSep { get; set; } = -10;
	[Export] public float YMin { get; set; } = 0;
	[Export] public float YMax { get; set; } = 15;

	public List<Card> Cards { get; set; } = new List<Card>();

	private int centerScreenX;

	private Deck deck;

	private CardManager cardManager;

	public bool isReadyToPass = false;

	public override void _Ready()
	{
		centerScreenX = (int)GetViewport().GetVisibleRect().Size.X / 2;
		
		deck = GetParent().GetNode<Deck>("Deck");
		cardManager = GetParent().GetNode<CardManager>("CardManager");

		deck.OnDealCard += AddCardToHand;
		cardManager.CardPassed += OnCardPassed;
		
		RotationCurve = new Curve();
		RotationCurve.AddPoint(new Vector2(0, -25)); // Left-most card
		RotationCurve.AddPoint(new Vector2(0.5f, 0)); // Middle card
		RotationCurve.AddPoint(new Vector2(1, 25)); // Right-most card

	}

	public void DrawCard(){
		var card = deck.DrawCard();
		AddCardToHand(card);
	}

	//TODO: maybe look at merging OnCardPassed method with AddCardToHand method, since they're the same.

	private void OnCardPassed(Card card, int targetPlayerId){
		if(targetPlayerId != PlayerID) return;

		AddCardToHand(card);
	}

	public void AddCardToHand(int peerId, Card card){
		
		if(peerId != PlayerID) return;

		AddCardToHand(card);
	}

	public void AddCardToHand(Card card){
		if(card is null) return;

		if(!Cards.Contains(card)){
			card.SetHand(this);
			Cards.Add(card);
			if(card.isFaceUp){
				card.GetNode<AnimationPlayer>("AnimationPlayer").PlayBackwards("card_flip");
			}
			UpdateHandPosition();			
			if(isLocalPlayer){
				card.GetNode<AnimationPlayer>("AnimationPlayer").Play("card_flip");
			}
			card.isSelected = false;
		}
		else {
			if(card.isFaceUp){
				card.GetNode<AnimationPlayer>("AnimationPlayer").PlayBackwards("card_flip");
			}
			AnimateCardPosition(card, card.HandPosition, card.HandRotation);
		}
	}

	public void UpdateHandPosition(){
		for (int i = 0; i < Cards.Count; i++)
		{
			var newPositionData = CalculateCardPosition(i);
			var card = Cards[i];
			
			card.HandPosition = newPositionData.newPosition;
			card.HandRotation = newPositionData.targetRotation;
			card.ZIndex = i + 1;

			AnimateCardPosition(card, newPositionData.newPosition, newPositionData.targetRotation);
		}
	}

	public void RemoveCardFromHand(Card card){
		if(Cards.Contains(card)){
			Cards.Remove(card);
			UpdateHandPosition();
		}
	}

	private (Vector2 newPosition, float targetRotation) CalculateCardPosition(int index){
		
		float t = Cards.Count == 1 ? 0.5f : (float)index / (Cards.Count - 1); // Normalized value between 0 and 1
		float rotation = 0;// RotationCurve.Sample(t); // Leave off the curve for now
		float spacing = handPosition == HandPosition.Bottom ? 75.0f : 40.0f;
		
		Vector2 handCenter = GlobalPosition; 
		
		float offsetX = spacing * index - (spacing * (Cards.Count - 1) / 2);
		float offsetY = Mathf.Abs(rotation * 2.5f);

		switch(handPosition){
			case HandPosition.Top:
				offsetY = -offsetY;
				break;
			case HandPosition.Bottom:
				break;
			case HandPosition.Left:
				(offsetX, offsetY) = (offsetY, offsetX);
				rotation += 90;
				break;
			case HandPosition.Right:
				(offsetX, offsetY) = (-offsetY, offsetX);
				rotation -= 90;
				break;
		}

		return (handCenter + new Vector2(offsetX, offsetY), rotation);
	}

	private void AnimateCardPosition(Card card, Vector2 targetPosition, float targetRotation){
		var tween = CreateTween();
		
		tween.TweenProperty(card, "position", targetPosition, 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(card, "rotation_degrees", targetRotation, 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	public void Discard()
	{
		GD.Print("Discard a Card");
	}
}


public enum HandPosition
{
	Top,
	Bottom,
	Left,
	Right
}
