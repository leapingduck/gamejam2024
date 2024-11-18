using System;
using System.Collections.Generic;
using Godot;

public partial class Hand : Node2D
{
	
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

	public override void _Ready()
	{
		centerScreenX = (int)GetViewport().GetVisibleRect().Size.X / 2;
		
		deck = GetParent().GetNode<Deck>("Deck");
		
		RotationCurve = new Curve();
		RotationCurve.AddPoint(new Vector2(0, -25)); // Left-most card
		RotationCurve.AddPoint(new Vector2(0.5f, 0)); // Middle card
		RotationCurve.AddPoint(new Vector2(1, 25)); // Right-most card

		/*
		for (int i = 0; i < HAND_COUNT; i++)
		{
			DrawCard();
		}
		*/
		// Called every time the node is added to the scene.
		// Initialization here
	}

	public void DrawCard(){
		var card = deck.DrawCard();
		AddCardToHand(card);
	}

	public void AddCardToHand(Card card){
		if(card is null) return;

		if(!Cards.Contains(card)){
			Cards.Add(card);
			UpdateHandPosition();
		}
		else {
			card.GetNode<AnimationPlayer>("AnimationPlayer").Play("card_flip");
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
		float rotation = RotationCurve.Sample(t);
		float spacing = 150.0f;
		
		Vector2 handCenter = GlobalPosition; 
		
		float offsetX = spacing * index - (spacing * (Cards.Count - 1) / 2);
		float offsetY = Mathf.Abs(rotation * 2.5f);

		return (handCenter + new Vector2(offsetX, offsetY), rotation);
	}

	private void AnimateCardPosition(Card card, Vector2 targetPosition, float targetRotation){
		var tween = CreateTween();
		
		tween.TweenProperty(card, "position", targetPosition, 0.5f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(card, "rotation_degrees", targetRotation, 0.5f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	public void Draw()
	{
		GD.Print("Draw a card");
		
	}

	public void Discard()
	{
		GD.Print("Discard a Card");
	}
}
