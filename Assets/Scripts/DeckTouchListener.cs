using UnityEngine;
using Whatwapp;

public class DeckTouchListener : MonoBehaviour {
    
    float lastEventTime = 0.0f;
    float timeBetweenEvents = 0.4f;

    // ho creato un oggetto in foreground per intercettare il click sul mazzo prima che vengano toccate le carte
    void OnMouseDown() {
        
        if (Time.time - lastEventTime < timeBetweenEvents) return;
        lastEventTime = Time.time;

        if (GameManager.Instance.GameState == GameState.PAUSE || GameManager.Instance.Initializing) return;
        GameManager.Instance.MainDeck.OnTouchDeck();
    }
}
