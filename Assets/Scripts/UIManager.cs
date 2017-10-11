using System;
using UnityEngine;
using UnityEngine.UI;
using Whatwapp;

public class UIManager : MonoBehaviour {

    public Text Score;
    public Text Moves;

    // toolbar
    Button OptionsButton;
    Button PauseButton;
    Button UndoButton;
    Button HintButton;

    // modali
    Transform PauseMenu;
    Transform OptionsMenu;

    // pulsanti pause menu
    Button ReplayGameButton;
    Button RestartGameButton;
    Button ResumeGameButton;

    // opzioni
    Toggle OptionDraw3;
    Toggle OptionHints;
    Button CloseOptionsButton;

    float lastEventTime = 0.0f;
    float timeBetweenEvents = 0.4f;

    void Awake() {
        Score = transform.Find("Score Bar").Find("Score").GetComponent<Text>();
        Moves = transform.Find("Score Bar").Find("Moves").GetComponent<Text>();

        OptionsButton = transform.Find("Tool Bar").Find("Button Options").GetComponent<Button>();
        PauseButton = transform.Find("Tool Bar").Find("Button Pause").GetComponent<Button>();
        UndoButton = transform.Find("Tool Bar").Find("Button Undo").GetComponent<Button>();
        HintButton = transform.Find("Tool Bar").Find("Button Hint").GetComponent<Button>();

        PauseMenu = transform.Find("Pause Menu");
        OptionsMenu = transform.Find("Options Menu");

        ReplayGameButton = PauseMenu.Find("Pause Menu Frame").Find("Replay Game").GetComponent<Button>();
        RestartGameButton = PauseMenu.Find("Pause Menu Frame").Find("Restart Game").GetComponent<Button>();
        ResumeGameButton = PauseMenu.Find("Pause Menu Frame").Find("Resume Game").GetComponent<Button>();

        OptionDraw3 = OptionsMenu.Find("Options Menu Frame").Find("Option Draw 3").GetComponent<Toggle>();
        OptionHints = OptionsMenu.Find("Options Menu Frame").Find("Option Hints").GetComponent<Toggle>();
        CloseOptionsButton = OptionsMenu.Find("Button Close").GetComponent<Button>();
    }
    

    void Start() {
        OptionsButton.onClick.AddListener(OnOptionsButton);
        PauseButton.onClick.AddListener(OnPauseButton);
        UndoButton.onClick.AddListener(OnUndoButton);
        HintButton.onClick.AddListener(OnHintButton);

        ReplayGameButton.onClick.AddListener(OnReplayButton);
        RestartGameButton.onClick.AddListener(OnRestartButton);
        ResumeGameButton.onClick.AddListener(OnResumeButton);

        OptionDraw3.onValueChanged.AddListener(OnOptionDraw3Changed);
        OptionHints.onValueChanged.AddListener(OnOptionHintsChanged);
        CloseOptionsButton.onClick.AddListener(Continue);
    }

    private void OnOptionDraw3Changed(bool value) {
        GameManager.Instance.OptionDraw3 = value;
    }

    private void OnOptionHintsChanged(bool value) {
        GameManager.Instance.OptionHints = value;

        HintButton.gameObject.SetActive(GameManager.Instance.OptionHints);
    }

    void OnPauseButton() {
        if (GameManager.Instance.Initializing) return;
        PauseMenu.gameObject.SetActive(true);
        Freeze();
    }

    void OnOptionsButton() {
        if (GameManager.Instance.Initializing) return;
        OptionsMenu.gameObject.SetActive(true);
        Freeze();
    }

    void OnUndoButton() {

        if (Time.time - lastEventTime < timeBetweenEvents) return;
        lastEventTime = Time.time;

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
        OptionsMenu.gameObject.SetActive(false);

        GameManager.Instance.GameState = GameState.PLAY;
        Time.timeScale = 1.0f;

        OptionsButton.enabled = true;
        PauseButton.enabled = true;
        UndoButton.enabled = true;
        HintButton.enabled = true;
    }

    void Freeze() {
        GameManager.Instance.GameState = GameState.PAUSE;
        Time.timeScale = 0.0f;

        OptionsButton.enabled = false;
        PauseButton.enabled = false;
        UndoButton.enabled = false;
        HintButton.enabled = false;
    }
}
