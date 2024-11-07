using Godot;

public partial class Hand : Node
{
	
	private PackedScene CARD = (PackedScene)GD.Load("res://scenes/card.tscn");

	[Export] public Curve HandCurve { get; set; }
	[Export] public Curve RotationCurve { get; set; }
	
	[Export] public float MaxRotationDegrees { get; set; } = 5;
	[Export] public float XSep { get; set; } = -10;
	[Export] public float YMin { get; set; } = 0;
	[Export] public float YMax { get; set; } = 15;

	public void Draw()
	{
		GD.Print("Draw a card");
		
	}

	public void Discard()
	{
		GD.Print("Discard a Card");
	}
}
