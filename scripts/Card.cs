using Godot;

public partial class Card : Panel
{
	private static readonly Vector2 SIZE = new Vector2(100, 140);

	[Export] public string Text { get; set; }
	
	private Label label;

	public override void _Ready()
	{
		label = GetNode<Label>("Label");
		
		label.Text = Text;
	}
}
