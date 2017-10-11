using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Whatwapp;

public class GameManager : MonoBehaviour {
    
    public static GameManager Instance; 
    public UIManager UI; // reference alla UI

    public Card CardPrefab; // prefab della carta
    public Deck DeckPrefab; // prefab del mazzo

    public bool Initializing = true;
    public GameState GameState = GameState.PLAY;

    public Deck MainDeck;
    public Deck DrawDeck;

    public List<Card> SavedCards;    

    public Transform ColumnsContainer;
    public Transform BasesContainer;

    public Deck[] Columns = new Deck[7];
    public Deck[] Bases = new Deck[4];

    // Stack delle mosse
    public Stack<Move> MoveList = new Stack<Move>();

    float columnWidth = 0.76f;
    float _cardSpeed = 30.0f; // velocità a cui vengono date le carte
    
    // punteggio
    private int _score;
    public int Score {
        get { return _score; }
        set {
            _score = (value < 0 ? 0 : value);
            UI.Score.text = _score.ToString();
        }
    }
    
    // numero di mosse effettuate
    private int _moves;
    public int Moves {
        get { return _moves; }
        set {
            _moves = (value < 0 ? 0 : value);
            UI.Moves.text = _moves.ToString();
        }
    }    

    void Awake () {

        // Creo l'istanza singleton
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }
        //DontDestroyOnLoad(gameObject);
    }

    // primo avvio del gioco
    void Start() {   
        
        PopulateMainDeck();
        saveCards();
        CreateColumns();       
        InitBases();
        StartCoroutine("cGiveCards");
    }

    // Nuova partita
    public void Restart() {

        //Clean();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Replay partita con ultimo mazzo
    public void Replay() {

        Clean();
        restoreCards();
        saveCards();
        StartCoroutine("cGiveCards");
    }

    // salvo le carte dell'ultimo mazzo
    private void saveCards() {

        foreach (Card c in MainDeck.Cards)
            SavedCards.Add(c);
    }

    // ripristino le carte salvate
    private void restoreCards() {

        foreach (Card sc in SavedCards) {
            Card c = Instantiate(CardPrefab, MainDeck.transform);
            c.Value = sc.Value;
            c.Suit = sc.Suit;
            c.Scoperta = false;
            c.Draggable = false;
            MainDeck.AddCard(ref c);
        }
        SavedCards.Clear();
    }

    // svuoto tutti i mazzi
    private void Clean() {

        foreach (Deck d in Bases) d.Clean();
        foreach (Deck d in Columns) d.Clean(); 

        DrawDeck.Clean();
        MainDeck.Clean();

        Score = 0;
        Moves = 0;
    }

    #region gestione stack delle mosse

    // aggiungo una mossa allo stack delle mosse
    public void AddMove(Move move) {

        MoveList.Push(move);
        //Debug.Log("Added Move: " + move.Quantity + " cards from " + move.Sender.name + " to " + move.Receiver.name + " Flipped: " + (move.Flipped?"TRUE":"FALSE"));
    }

    // rollback dell'ultima mossa
    public void UndoLastMove() {

        if (MoveList.Count == 0) 
            return;

        Move lastMove = MoveList.Pop();

        //Debug.Log("Undo Last move: " + lastMove.Quantity + " cards from " + lastMove.Sender.name + " to " + lastMove.Receiver.name + " Flipped: " + (lastMove.Flipped ? "TRUE" : "FALSE"));
        
        if (lastMove.Sender.Type == DeckType.DRAW && lastMove.Receiver.Type == DeckType.MAIN) {
            
            lastMove.Receiver.TransferCardsToDeck(lastMove.Sender, true, lastMove.Quantity, Transition.INSTANT, MoveDirection.REVERSE, true);

        } else if (lastMove.Sender.Type == DeckType.DRAW) {

            lastMove.Receiver.TransferCardsToDeck(lastMove.Sender, true, 1, Transition.ANIMATE, MoveDirection.REVERSE);
            GameManager.Instance.Score -= lastMove.Score;

        } else if (lastMove.Sender.Type == DeckType.COLUMN) {

            if (lastMove.Flipped) {
                lastMove.Sender.Top.Scoperta = false;
            }

            lastMove.Sender.Drop(ref lastMove.Card, lastMove.Receiver, MoveDirection.REVERSE);
            GameManager.Instance.Score -= lastMove.Score;

        } else if (lastMove.Sender.Type == DeckType.MAIN) {
            lastMove.Receiver.TransferCardsToDeck(lastMove.Sender, false, lastMove.Quantity, Transition.ANIMATE, MoveDirection.REVERSE);
        }

    }

    #endregion

    #region initializers

    public void PopulateMainDeck() {
        MainDeck.Clean();
        foreach (Suit seme in Enum.GetValues(typeof(Suit))) {
            foreach (Value valore in Enum.GetValues(typeof(Value))) {
                Card c = Instantiate(CardPrefab, MainDeck.transform);
                c.Value = valore;
                c.Suit = seme;
                c.Scoperta = false;
                c.Draggable = false;
                MainDeck.AddCard(ref c);
            }
        }
        MainDeck.Shuffle();
    }

    public void CreateColumns() {
        // create columns
        float hPos = -columnWidth;
        for (int cnum = 0; cnum < Columns.Length; cnum++) {
            Columns[cnum] = Instantiate(DeckPrefab, ColumnsContainer);
            Columns[cnum].Type = DeckType.COLUMN;
            Columns[cnum].IsMatrioska = true;
            Columns[cnum].CardsHaveOffset = true;
            Columns[cnum].Offset = new Vector2(0, -0.5f);
            Columns[cnum].transform.localPosition = new Vector3(hPos += columnWidth, 0.0f, 0.0f);
            Columns[cnum].name = "Column #" + cnum;
        }
    }

    public void InitBases() {
        // init bases
        float hPos = -columnWidth;
        Suit s = 0;
        for (int bnum = 0; bnum < Bases.Length; bnum++) {
            Bases[bnum] = Instantiate(DeckPrefab, BasesContainer);
            Bases[bnum].Type = DeckType.BASE;
            Bases[bnum].Suit = s; s++;
            Bases[bnum].ShowBackground = true;
            Bases[bnum].ShowSuit = true;
            Bases[bnum].CardsHaveOffset = true;
            Bases[bnum].Offset = new Vector2(-0.001f, 0.0f);
            Bases[bnum].transform.localPosition = new Vector3(hPos += columnWidth, 0.0f, 0.0f);
            Bases[bnum].name = "Base #" + bnum;
        }
    }

    #endregion

    #region coroutines

    IEnumerator cGiveCards() {

        if (Columns.Length == 0) yield break;

        Transform destination;
        Card card;
        Deck column;

        for (int row = 0; row < Columns.Length; row++) {
            for (int cnum = row; cnum < Columns.Length; cnum++) {

                column = Columns[cnum];

                destination = (!column.IsEmpty ? column.Top.transform : column.transform);

                card = MainDeck.Top;
                card.SetSortingLayerName("Dragged");

                while (card.transform.position != destination.position) {
                    card.transform.position = Vector3.MoveTowards(card.transform.position, destination.position, _cardSpeed * Time.deltaTime);
                    yield return 0;
                }

                column.Cards.Add(card);
                card.Deck = column;
                card.gameObject.transform.SetParent(destination);
                card.SetSortingLayerName("Cards");
                card.doFlip = false;
                card.Scoperta = (row == cnum);
                card.doFlip = true;
                card.Draggable = (row == cnum);

                MainDeck.DiscardTop();
                column.Reorder();
            }           
        }

        Initializing = false;        
    }

    #endregion
}
