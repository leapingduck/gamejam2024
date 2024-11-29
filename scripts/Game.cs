using Godot;

public partial class Game : Control

{
	private Hand hand;
	private CardManager cardManager;

	public override void _Ready()
	{
		hand = GetNode<Hand>("Hand");
	}

	private void OnResetButtonPressed()
	{
		GD.Print("Reset");
		GetTree().ReloadCurrentScene();
	}

	private void OnDiscardCardButtonPressed()
	{
		hand.Discard();
	}
}
