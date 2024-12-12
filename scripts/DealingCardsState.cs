using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DealingCardsState : IGameState
{
	public override void Enter()
	{
		if(Multiplayer.GetUniqueId() == 1){
			AssignHands(_gameManager.peerId_PlayOrder);
		}

		Console.WriteLine("Entering DealingCards state...");
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void AssignHandsInOrder(int[] peers){
		int myPeerId = Multiplayer.GetUniqueId();

		List<int> peerIds = new List<int>(peers);

        // Sort peerIds so they're always in a consistent order
        peerIds.Sort();
		
        // Find the index of the current client
        int myIndex = peerIds.IndexOf(myPeerId);

		//If only two select hand across from each other.
		if(peerIds.Count == 2){
			_gameManager._hands.Find(h => h.handPosition == HandPosition.Bottom).PlayerID = peerIds[myIndex];
			_gameManager._hands.Find(h => h.handPosition == HandPosition.Top).PlayerID = peerIds.FirstOrDefault(x => x != peerIds[myIndex]);
			return;
		}

        // Assign the hands in a rotating manner
        for (int i = 0; i < peerIds.Count; i++)
        {
            int handIndex = (i - myIndex + peerIds.Count) % peerIds.Count; // Circular index
            _gameManager._hands[handIndex].PlayerID = peerIds[i];
        }
	}
	private void AssignHands(List<int> peerIds){
		Rpc(MethodName.AssignHandsInOrder, peerIds.ToArray());
	}

	public override void Execute()
	{
		if(_gameManager._deck is null){
			Console.WriteLine("Deck is null");
			return;
		}
		
		if(Multiplayer.GetUniqueId() == 1){
			_gameManager._deck.GenDeck();
			_gameManager._deck.ShuffleDeck();
			_gameManager._deck.ShuffleDeck();
			_gameManager._deck.DealCards(_gameManager._hands);
		}
		
		Console.WriteLine("Executing DealingCards state...");
	}

	public override void Exit()
	{
		Console.WriteLine("Exiting DealingCards state...");
	}

	public override GameState? CheckForTransition()
	{
		if(_gameManager._deck.IsEmpty())
		{
			return GameState.PassingCards;
		}
		// Add logic to check for transition conditions.
		return null;
	}
}
