using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour {

    Button PauseButton;
    Button UndoButton;
    Button HintButton;
    
    void Awake() {
        PauseButton = transform.Find("Button Pause").GetComponent<Button>();
        UndoButton = transform.Find("Button Undo").GetComponent<Button>();
        HintButton = transform.Find("Button Hint").GetComponent<Button>();  
    }
    
    void Start() {
        PauseButton.onClick.AddListener(OnPauseButton);
        UndoButton.onClick.AddListener(OnUndoButton);
        HintButton.onClick.AddListener(OnHintButton);
    }
    
    void OnPauseButton() {
        Debug.Log("PAUSE");
    }

    void OnUndoButton() {
        Debug.Log("UNDO");
    }

    void OnHintButton() {
        Debug.Log("HINT");
    }
}
