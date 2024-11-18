using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class CardManager : Node2D
{

	Rect2 ScreenSize;
	Card CardBeingDragged = null;
	Hand PlayerHand = null;

	bool isHoveringOnCard = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect();
		PlayerHand = GetParent().GetNode<Hand>("Hand");
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
					var card = (Card)raycastCheckForCard();
					if(card is not null){
						StartDrag(card);
					}
				}
				if(!mouseEvent.Pressed){
					EndDrag(CardBeingDragged);
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

	private void StartDrag(Card card){
		CardBeingDragged = card;

	}

	private void EndDrag(Card card){
		var cardSlot = raycastCheckForCardSlot();
		if(cardSlot is not null){
			CardBeingDragged.GlobalPosition = cardSlot.GlobalPosition;
			var tween = CreateTween();
			tween.TweenProperty(CardBeingDragged, "rotation_degrees", 0, 0.25f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		}

		if(cardSlot is null){
			PlayerHand.AddCardToHand(card);
		}

		CardBeingDragged = null;
	}

	private Card raycastCheckForCard(){
		var spaceState = GetWorld2D().DirectSpaceState;
		var parameters = new PhysicsPointQueryParameters2D(){
			Position = GetGlobalMousePosition(),
			CollideWithAreas = true,
			CollisionMask = (int)CollisionMask.Card
		};
		var result = spaceState.IntersectPoint(parameters);
		if(result.Count > 0){
			//var cardItems = result.Where(x => ((Area2D)x["collider"]).GetParent().GetType() == typeof(Card));
			
			//if(cardItems.Count() == 0) return null;

			var topItem = result.OrderByDescending(x => ((Area2D)x["collider"]).ZIndex).First();
			
			if(topItem is null) return null;
			
			var cardArea = (Area2D)topItem["collider"];
			if(cardArea is not null){
				var card = (Card)cardArea.GetParent();
				return card;
			}
		}
		return null;
	}

	private Node2D raycastCheckForCardSlot(){
		var spaceState = GetWorld2D().DirectSpaceState;
		var parameters = new PhysicsPointQueryParameters2D(){
			Position = GetGlobalMousePosition(),
			CollideWithAreas = true,
			CollisionMask = (int)CollisionMask.CardSlot
		};
		var result = spaceState.IntersectPoint(parameters);
		if(result.Count > 0){
			var topItem = result.OrderByDescending(x => ((Area2D)x["collider"]).ZIndex).First();
			
			if(topItem is null) return null;
			
			var slotArea = (Area2D)topItem["collider"];
			if(slotArea is not null){
				var slot = (Node2D)slotArea.GetParent();
				return slot;
			}
		}
		return null;
	}


	public void ConnectCardSignals(Card card){
		
		var cardArea = card.GetNode<Area2D>("Area2D");
		
		cardArea.MouseEntered += () => CardHovered(card);
		cardArea.MouseExited += () => CardExited(card);
	}

	public void CardHovered(Card card){
		if(!isHoveringOnCard){
			isHoveringOnCard = true;
			hightlightCard(card, true);
		}
	}

	public void CardExited(Card card){
		hightlightCard(card, false);

		Card hovercard = raycastCheckForCard();
		if(hovercard is null){
			isHoveringOnCard = false;
		}
		if(hovercard is not null && hovercard != card){
			hightlightCard(hovercard, true);
		}
	}

	private void hightlightCard(Card card, bool highlight){
		if(highlight){
			card.Scale = new Vector2(1.2f, 1.2f);
			card.ZIndex = 2;
		}
		else {
			card.Scale = new Vector2(1, 1);
			card.ZIndex = 1;
		}
		
	}

}
