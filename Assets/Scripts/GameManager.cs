using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Whatwapp;
using System.Linq;

/*  GEstione del flusso di gioco
 *  
 */
public class GameManager : MonoBehaviour {
    
    #region init

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
    public Move LastMove;
    public Move NextBestMove;

    float columnWidth = 0.76f;
    public float CardSpeed = 20.0f; // velocità di traslazione delle carte
    public float GiveCardSpeed = 40.0f; // velocità di traslazione delle carte

    public bool SomeoneIsDragging;
    public bool DraggingDisabled;

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

    #endregion

    void Awake() {

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

        Initializing = true;

        PopulateMainDeck();
        saveCards();
        CreateColumns();
        InitBases();
        StartCoroutine("cGiveCards");
    }
    
    #region ciclo di attività

    // Nuova partita
    public void Restart() {

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Replay partita con ultimo mazzo
    public void Replay() {

        Clean();
        restoreCards();
        saveCards();
        StartCoroutine("cGiveCards");
    }

    #endregion

    #region stack delle mosse

    // aggiungo una mossa allo stack delle mosse
    public void RegisterMove(Move move) {
        MoveList.Push(move);
        if (MoveList.Count > 0) LastMove = MoveList.First();

        //Debug.Log("Added Move: " + move.Quantity + " cards from " + move.Sender.name + " to " + move.Receiver.name + " Flipped: " + (move.Flipped?"TRUE":"FALSE"));
    }

    // rollback dell'ultima mossa
    public void UndoLastMove() {

        if (MoveList.Count == 0) 
            return;

        Move lastMove = MoveList.Pop();
        if (MoveList.Count > 0) LastMove = MoveList.First();

        //Debug.Log("Undo Last move: " + lastMove.Quantity + " cards from " + lastMove.Sender.name + " to " + lastMove.Receiver.name + " Flipped: " + (lastMove.Flipped ? "TRUE" : "FALSE"));

        // è stato ripristinato il mazzo principale rovesciando le carte estratte
        if (lastMove.Sender.Type == DeckType.DRAW && lastMove.Receiver.Type == DeckType.MAIN) {
            
            TransferCards(lastMove.Receiver, lastMove.Sender, lastMove.Quantity, TraslationType.INSTANT, MoveType.BACK, true);

        // l'ultima mossa veniva dalle carte estratte
        } else if (lastMove.Sender.Type == DeckType.DRAW) {

            TransferCards(lastMove.Receiver, lastMove.Sender, lastMove.Quantity, TraslationType.ANIMATE, MoveType.BACK);
            Score -= lastMove.Score;

        // l'ultima mossa veniva da una colonna
        } else if (lastMove.Sender.Type == DeckType.COLUMN) {

            if (lastMove.Flipped) {
                lastMove.Sender.Top.Scoperta = false;
            }

            lastMove.Sender.Drop(ref lastMove.Card, lastMove.Receiver, TraslationType.ANIMATE, MoveType.BACK);
			//TransferCards(lastMove.Receiver, lastMove.Sender, lastMove.Quantity, TraslationType.ANIMATE, MoveType.BACK);
            Score -= lastMove.Score;
        
        // l'ultima mossa veniva da una base
        } else if (lastMove.Sender.Type == DeckType.BASE) {
            
			//TransferCards(lastMove.Receiver, lastMove.Sender, lastMove.Quantity, TraslationType.ANIMATE, MoveType.BACK);
            lastMove.Sender.Drop(ref lastMove.Card, lastMove.Receiver, TraslationType.ANIMATE, MoveType.BACK);
            Score -= lastMove.Score;

        // l'ultima mossa veniva dal mazzo principale (estrazione di una carta)
        } else if (lastMove.Sender.Type == DeckType.MAIN) {

            TransferCards(lastMove.Receiver, lastMove.Sender, lastMove.Quantity, TraslationType.ANIMATE, MoveType.BACK);
        }
    }

    public Move GetNextBestMove() {

        List<Move> bestMoves = new List<Move>();
        List<Card> UsableCardsInScene = FindObjectsOfType<Card>().ToList().FindAll(x => (x.IsScoperta || x.Scoperta) && x.Draggable);

        foreach (Card card in UsableCardsInScene) {
            card.FindBestMove();
            if (card.NextBestMove != null) bestMoves.Add((Move)card.NextBestMove);
        }

        bestMoves = bestMoves.OrderBy(x => x.Weight).Reverse().ToList();

        if (bestMoves.Count > 0)
            NextBestMove = bestMoves.First();
        else
            NextBestMove = null;

        return NextBestMove;
    }

    #endregion

    #region gestione deck

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
            Bases[bnum].Offset = new Vector2(0.003f, 0.0f);
            Bases[bnum].transform.localPosition = new Vector3(hPos += columnWidth, 0.0f, 0.0f);
            Bases[bnum].name = "Base #" + bnum;
        }
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

        MoveList.Clear();

        Score = 0;
        Moves = 0;
    }

    /* Trasferisce le carte dalla cima di un mazzo alla cima dell'altro (anche multiple) */
    public static void TransferCards(
        Deck sender,   // mazzo di origine
        Deck receiver, // mazzo di destinazione
        int numCards = 1,
        TraslationType traslation = TraslationType.INSTANT,
        MoveType direction = MoveType.FORWARD,
        bool registerAsOneMove = false
    ) {

        if (!sender.Top) return;

        for (int c = 0; c < numCards; c++) {

            if (!sender.Top) return;

            Card card = sender.Top;

            receiver.AddCard(ref card, sender, traslation);

            if (direction == MoveType.FORWARD && !registerAsOneMove) {
                Move newMove = new Move(sender, receiver, ref card);
                GameManager.Instance.RegisterMove(newMove);
            }

            if (!registerAsOneMove)
                GameManager.Instance.Moves += 1;

            sender.DiscardTop();

            int score = CalculateScore(sender, receiver);
            GameManager.Instance.Score += score;
        }

        if (direction == MoveType.FORWARD && registerAsOneMove) {
            Card card = receiver.Top;
            Move newMove = new Move(sender, receiver, ref card, false, 0, numCards);
            GameManager.Instance.RegisterMove(newMove);
        }

        if (registerAsOneMove)
            GameManager.Instance.Moves += 1;
    }

    /* calcola il punteggio assegnato per il trasferimento di una carta da un mazzo all'altro*/
    public static int CalculateScore(Deck sender, Deck receiver) {
        int score = 0;
        if (sender != null) {
            if (receiver.Type == DeckType.BASE && sender.Type == DeckType.COLUMN) { // dalla colonna alla base
                score += (int)Scores.FromColumnToBase;
            } else if (receiver.Type == DeckType.BASE && sender.Type == DeckType.DRAW) { // dalle carte estratte alla base
                score += (int)Scores.FromDrawToBase;
            } else if (receiver.Type == DeckType.COLUMN && sender.Type == DeckType.DRAW) { //dalle carte estratte a una colonna
                score += (int)Scores.FromDrawToColumn;
            }
        }
        return score;
    }

    #endregion

    #region coroutines

    // distribuisce le carte di gioco
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
                    card.transform.position = Vector3.MoveTowards(card.transform.position, destination.position, GiveCardSpeed * Time.deltaTime);
                    yield return new WaitForFixedUpdate();
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
