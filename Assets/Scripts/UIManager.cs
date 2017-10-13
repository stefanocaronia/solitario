using System;
using UnityEngine;
using UnityEngine.UI;
using Whatwapp;

/*  Componente che gestisce tutta la UI
 *  
 */
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
    Text AlertGameRestart;

    float lastEventTime = 0.0f;
    float timeBetweenEvents = 0.4f;

    bool OptionDraw3_saved;
    bool GameMustRestart;

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

        AlertGameRestart = OptionsMenu.Find("Options Menu Frame").Find("Game Restart").GetComponent<Text>();
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

    private void OnOptionMenuOpen() {

        GameMustRestart = false;
        AlertGameRestart.gameObject.SetActive(false);
        OptionDraw3_saved = GameOptions.Instance.OptionDraw3;

        OptionDraw3.isOn = GameOptions.Instance.OptionDraw3;
        OptionHints.isOn = GameOptions.Instance.OptionHints;
        HintButton.gameObject.SetActive(GameOptions.Instance.OptionHints);
             
    }

    private void OnOptionDraw3Changed(bool value) {

        AlertGameRestart.gameObject.SetActive(OptionDraw3_saved != value);
        GameMustRestart = (OptionDraw3_saved != value);
        GameOptions.Instance.OptionDraw3 = value;
    }

    private void OnOptionHintsChanged(bool value) {

        GameOptions.Instance.OptionHints = value;
        HintButton.gameObject.SetActive(GameOptions.Instance.OptionHints);
    }

    void OnPauseButton() {

        if (GameManager.Instance.Initializing) return;
        PauseMenu.gameObject.SetActive(true);
        Freeze();
    }

    void OnOptionsButton() {

        if (GameManager.Instance.Initializing) return;
        OptionsMenu.gameObject.SetActive(true);
        OnOptionMenuOpen();
        Freeze();
    }

    void OnUndoButton() {

        if (Time.time - lastEventTime < timeBetweenEvents) return;
        lastEventTime = Time.time;

        GameManager.Instance.UndoLastMove();
    }

    void OnHintButton() {

        Move NextBestMove = GameManager.Instance.GetNextBestMove();      
        Card SuggestedCard;
        Deck SuggestedReceiver;
        if (NextBestMove == null) {
            if (!GameManager.Instance.MainDeck.IsEmpty) {
                SuggestedCard = GameManager.Instance.MainDeck.Top;
                SuggestedReceiver = GameManager.Instance.DrawDeck;
            } else {
                return;
            }
        } else {
            SuggestedCard = NextBestMove.Card;
            SuggestedReceiver = NextBestMove.Receiver;
        }

        // Debug.Log(NextBestMove.Card.name + " in " + NextBestMove.Receiver.name);
        Vector3 position;
        if (SuggestedReceiver.Top != null) position = SuggestedReceiver.Top.transform.position;
        else position = SuggestedReceiver.transform.position;

        if (SuggestedReceiver.Type == DeckType.COLUMN && !SuggestedReceiver.IsEmpty) {
            position = new Vector3 (position.x, position.y - 0.26f, position.z);
        }
        SuggestedCard.MovePhantom(position);
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

        if (GameMustRestart) {
            GameMustRestart = false;
            AlertGameRestart.gameObject.SetActive(false);
            OptionDraw3_saved = GameOptions.Instance.OptionDraw3;
            GameManager.Instance.Replay();
        }
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
