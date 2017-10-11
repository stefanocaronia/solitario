using UnityEngine;
using Whatwapp;

public class DeckTouchListener : MonoBehaviour {
    
    // ho creato un oggetto in foreground per intercettare il click sul mazzo prima che vengano toccate le carte, mi ha risolto molti problemi
    void OnMouseDown() {
        if (GameManager.Instance.GameState == GameState.PAUSE || GameManager.Instance.Initializing) return;
        GameManager.Instance.MainDeck.OnTouchCard();
    }
}
