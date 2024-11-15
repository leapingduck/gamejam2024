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
		
		for (int i = 0; i < HAND_COUNT; i++)
		{
			DrawCard();
		}
		// Called every time the node is added to the scene.
		// Initialization here
	}

	public void DrawCard(){
		var card = deck.DrawCard();
		Cards.Add(card);
		UpdateHandPosition();
	}

	public void UpdateHandPosition(){
		for (int i = 0; i < Cards.Count; i++)
		{
			CalculateCardPosition(i);
		}
	}

	private void CalculateCardPosition(int index){
		float t = (float)index / (HAND_COUNT - 1);
		float x = HandCurve.Sample(t) * centerScreenX;
		float y = YMin + RotationCurve.Sample(t) * YMax;
		float rotation = MaxRotationDegrees * RotationCurve.Sample(t);
		AnimateCardPosition(index, new Vector2(x, y), rotation);
	}

	private void AnimateCardPosition(int index, Vector2 targetPosition, float targetRotation){
		var card = Cards[index];
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
