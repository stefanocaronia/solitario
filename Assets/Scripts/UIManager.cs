using UnityEngine;
using UnityEngine.UI;
using Whatwapp;

public class UIManager : MonoBehaviour {

    public Text Score;
    public Text Moves;

    Button PauseButton;
    Button UndoButton;
    Button HintButton;

    Transform PauseMenu;

    Button ReplayGameButton;
    Button RestartGameButton;
    Button ResumeGameButton;

    void Awake() {
        Score = transform.Find("Score Bar").Find("Score").GetComponent<Text>();
        Moves = transform.Find("Score Bar").Find("Moves").GetComponent<Text>();

        PauseButton = transform.Find("Tool Bar").Find("Button Pause").GetComponent<Button>();
        UndoButton = transform.Find("Tool Bar").Find("Button Undo").GetComponent<Button>();
        HintButton = transform.Find("Tool Bar").Find("Button Hint").GetComponent<Button>();

        PauseMenu = transform.Find("Pause Menu");

        ReplayGameButton = PauseMenu.Find("Pause Menu Frame").Find("Replay Game").GetComponent<Button>();
        RestartGameButton = PauseMenu.Find("Pause Menu Frame").Find("Restart Game").GetComponent<Button>();
        ResumeGameButton = PauseMenu.Find("Pause Menu Frame").Find("Resume Game").GetComponent<Button>();
    }
    

    void Start() {
        PauseButton.onClick.AddListener(OnPauseButton);
        UndoButton.onClick.AddListener(OnUndoButton);
        HintButton.onClick.AddListener(OnHintButton);

        ReplayGameButton.onClick.AddListener(OnReplayButton);
        RestartGameButton.onClick.AddListener(OnRestartButton);
        ResumeGameButton.onClick.AddListener(OnResumeButton);
    }

    void OnPauseButton() {
        
        PauseMenu.gameObject.SetActive(true);
        GameManager.Instance.GameState = GameState.PAUSE;
        Time.timeScale = 0.0f;

        PauseButton.enabled = false;
        UndoButton.enabled = false;
        HintButton.enabled = false;
    }

    void OnUndoButton() {
        GameManager.Instance.UndoLastMove();
    }

    void OnHintButton() {
        Debug.Log("HINT");
    }

    void OnRestartButton() {
        GameManager.Instance.Restart();
        Continue();
    }

    void OnReplayButton() {
        GameManager.Instance.Replay();
        Continue();
    }

    void OnResumeButton() {
        Continue();
    }

    void Continue() {
        PauseMenu.gameObject.SetActive(false);
        GameManager.Instance.GameState = GameState.PLAY;
        Time.timeScale = 1.0f;

        PauseButton.enabled = true;
        UndoButton.enabled = true;
        HintButton.enabled = true;
    }    
}
