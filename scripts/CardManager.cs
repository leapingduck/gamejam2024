using Godot;
using System;
using System.Linq;

public partial class CardManager : Node2D
{

	Rect2 ScreenSize;
	Node2D CardBeingDragged = null;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(CardBeingDragged is not null){
			var mousePosition = GetGlobalMousePosition();
			
			CardBeingDragged.GlobalPosition = new Vector2( 
				Math.Clamp(mousePosition.X, 0, ScreenSize.Position.X + ScreenSize.Size.X),
				Math.Clamp(mousePosition.Y, 0, ScreenSize.Position.Y + ScreenSize.Size.Y)
			);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if(@event is InputEventMouseButton mouseEvent){
			if(mouseEvent.ButtonIndex == MouseButton.Left){
				if(mouseEvent.Pressed){
					var card = raycastCheckForCard();
					if(card is not null){
						CardBeingDragged = card;
					}
				}
				if(!mouseEvent.Pressed){
					CardBeingDragged = null;
				}
			}
			/*
			if(mouseEvent.ButtonIndex == MouseButton.Right){
				if(mouseEvent.Pressed){
					// Get the node under the mouse
					Node2D node = GetNodeAtPosition(GetGlobalMousePosition());
					if(node is Card card){
						card.ShowBack();
					}
				}
			}
			*/
		}
	}

	private Node2D raycastCheckForCard(){
		var spaceState = GetWorld2D().DirectSpaceState;
		var parameters = new PhysicsPointQueryParameters2D(){
			Position = GetGlobalMousePosition(),
			CollideWithAreas = true,
			CollisionMask = 1
		};
		var result = spaceState.IntersectPoint(parameters);
		if(result.Count > 0){
			var cardArea = (Area2D)result[0]["collider"];
			if(cardArea is not null){
				var card = (Node2D)cardArea.GetParent();
				return card;
			}
		}
		return null;
	}

	public void ConnectCardSignals(Card card){
		
		var cardArea = card.GetNode<Area2D>("Area2D");
		
		cardArea.MouseEntered += () => CardHovered(card);
		cardArea.MouseExited += () => CardExited(card);
	}

	public void MouseEnter(){
		Console.WriteLine("Mouseenter");
	}

	public void MouseExit(){
		Console.WriteLine("Mouseexit");
	}


	public void CardHovered(Card card){
		card.Scale = new Vector2(1.2f, 1.2f);
		Console.WriteLine("Card hovered");	
	}

	public void CardExited(Card card){
		card.Scale = new Vector2(1, 1);
		Console.WriteLine("Card exited");
	}

}
