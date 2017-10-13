using UnityEngine;
using Whatwapp;

// oggetto in foreground per intercettare il click sul mazzo prima che vengano toccate le carte
public class DeckTouch : MonoBehaviour {
    
    float lastEventTime = 0.0f;
    float timeBetweenEvents = 0.4f;
   
    void OnMouseDown() {
        
        if (Time.time - lastEventTime < timeBetweenEvents) return;
        lastEventTime = Time.time;

        if (GameManager.Instance.GameState == GameState.PAUSE || GameManager.Instance.Initializing) return;
        GameManager.Instance.MainDeck.OnTouchDeck();
    }
}
