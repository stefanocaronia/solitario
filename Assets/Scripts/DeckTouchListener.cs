using UnityEngine;
using System.Collections;

public class DeckTouchListener : MonoBehaviour {
    
    void OnMouseUp() {
        if (GameManager.Instance.Initializing) return;
        GameManager.Instance.MainDeck.OnTouchCard();
    }
}
