using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Whatwapp;
using System;
using System.Linq;

/*  Componente per la gestione del mazzo
 *  
 */
public class Deck : MonoBehaviour {

    #region init

    public List<Card> Cards; // lista delle carte
    public Suit Suit; // seme del mazzo (si usa nelle basi)

    public bool IsMatrioska; // ogni carta è un child della carta precedente (si usa nelle colonne)
    public bool Draggable; // Se false le carte del mazzo non sono draggable
    public bool AcceptDrops; // può ricevere carte trascinate sopra dal giocatore
    public bool CardsHaveOffset; // le carte si dispongono nel mazzo shiftwate di un offset
    public Vector2 Offset = Vector2.zero; // offset tra le carte
    public Vector2 OffsetSecond = Vector2.zero; // secondo offset tra le carte
    public int CardsWithSecondOffset = 0; // numero di carte con un secondo offset    

    private bool _cMoveCard_running; // è in corso il movimento della carta
    private bool _ordering; // è in corso il riordinamento del mazzo

    private SpriteRenderer _backgroundRenderer; // background o fondo del mazzo
    private SpriteRenderer _suitRenderer; // seme del mazzo da visualizzare sul background (per le basi)

    // tipo di mazzo (base, column, main e draw)
    [SerializeField]
    private DeckType _type;
    public DeckType Type {
        get {
            return _type;
        }
        set {
            _type = value;

            // le carte del mazzo main non sono draggabili, si può solo cliccare
            Draggable = _type != DeckType.MAIN;

            // solo le colonne o le basi possono ricevere il drop
            AcceptDrops = _type == DeckType.COLUMN || _type == DeckType.BASE;
        }
    }

    // se true mostra il background del mazzo
    private bool _showBackground = false;
    public bool ShowBackground {
        get {
            return _showBackground;
        }
        set {
            _backgroundRenderer.enabled = value;
            _suitRenderer.enabled = value && ShowSuit;
            _suitRenderer.sprite = Resources.Load<Sprite>("carte/semi/" + Suit.ToString().ToLower());
        }
    }

    // se true mostra il seme del mazzo
    private bool _showSuit = false;
    public bool ShowSuit {
        get {
            return _showSuit;
        }
        set {
            _suitRenderer.enabled = value;
        }
    }

    // ottengo la carta al top del mazzo, o prima carta disponibile
    public Card Top {
        get {
            return (Cards.Count > 0 ? Cards[Cards.Count - 1] : null);
        }
    }

    // true se il mazzo è vuoto
    public bool IsEmpty {
        get {
            return (Cards.Count == 0);
        }
    }

    #endregion

    // inizializzo la carta
    void Awake() {
        _backgroundRenderer = GetComponent<SpriteRenderer>();
        _suitRenderer = transform.Find("Suit").GetComponent<SpriteRenderer>();
    }

    #region operations

    public bool Accept(Card card) {

        if (!AcceptDrops) return false;

        int numCards = card.transform.GetComponentsInChildren<Card>().Length;

        // decido cosa impedire, in base al tipo di mazzo di destinazione (this.Type) e i criteri di gioco
        switch (Type) {

            case (DeckType.BASE): // il mazzo di destinazione è una base
                if (numCards > 1) return false; // la base può ricevere solo una carta alla volta
                if (card.Suit != Suit) return false; // il seme della carta deve corrispondere a quello della base
                if (card.Value != (Top ? Top.Value + 1 : Value.ASSO)) return false; // il valore della carta deve essere la precedente + 1 o un asso se il mazzo è vuoto
                break;

            case (DeckType.COLUMN): // il mazzo di destinazione è una colonna               
                if (IsEmpty && card.Value != Value.RE) return false; // se la colonna è vuota accetta solo un re
                if (Top != null && card.Color == Top.Color) return false; // la carta non deve avere lo stesso colore della precedente
                if (!IsEmpty && card.Value != Top.Value - 1) return false; // la carta deve avere un valore decrescente
                break;
        }

        return true;
    }
	
    /* Il giocatore ha trascinato e droppato una o più carte sul mazzo */
    public bool Drop(
        ref Card card, 
        Deck sender, 
        TraslationType transition = TraslationType.INSTANT, 
        MoveType moveType = MoveType.FORWARD
    ) {

        if (!AcceptDrops) return false;
        
        if (!Accept(card) && moveType != MoveType.BACK) return false;

        int numCards = card.transform.GetComponentsInChildren<Card>().Length;

        // inizializzo la mossa da salvare in history
        Move newMove = new Move(sender, this, ref card);

        bool flipped = false;

        // sono state trascinate più carte assieme
        if (numCards > 1) {

            // ottengo tutte le carte della catena
            Card[] cards = card.transform.GetComponentsInChildren<Card>();
            
            // aggiungo tutte le carte al mazzo di destinazione
            for (int i = 0; i < cards.Length; i++) {
                AddCard(ref cards[i], sender);
                flipped = sender.DiscardTop(); // TODO: andrebbero tolte in senso inverso
            }
			
        } else {       
            // aggiungo la carta al mazzo di destinazione
            AddCard(ref card, sender, transition);
            flipped = sender.DiscardTop();
        }

        if (moveType == MoveType.FORWARD) {
            // calcolo il punteggio 
		    newMove.Score += GameManager.CalculateScore(sender, this);
        }

        // se il sender è una colonna ed è rimasta una carta coperta libera
        if (moveType == MoveType.FORWARD && sender.Type == DeckType.COLUMN && sender.Top != null && flipped) {
            newMove.Score += (int)Scores.FlippedCard;
            newMove.Flipped = true;
        }

        // se il trascinamento viene da una base allora sottraggo il punteggio
        if (sender.Type == DeckType.BASE) {
            newMove.Score -= GameManager.CalculateScore(this, sender);
        }

        if (moveType == MoveType.FORWARD) {
            // aggiorno il punteggio, registro la mossa, incremento il numero di mosse
            GameManager.Instance.Score += newMove.Score;
		    GameManager.Instance.RegisterMove(newMove);
        }

        GameManager.Instance.Moves += 1;

        card.FindBestMove();

        return true;
    }

    /* Aggiunge una carta al mazzo */
    public void AddCard(ref Card card, Deck sender = null, TraslationType transition = TraslationType.INSTANT) {

        // lock
        if (_ordering) return;        

        // se è una matrioska e non vuota la destinazione è l'ultima carta, se no il mazzo
        Transform target = (!IsEmpty && IsMatrioska ? Top.transform : transform);

        // scopro subito la carta (se la destinazione non è il mazzo principale)
        card.Scoperta = (Type != DeckType.MAIN);

		Cards.Add(card); // aggiungo l'oggetto carta alla lista
        card.Deck = this; // imposto la reference del mazzo	

        card.Move(target, transition, delegate(Card theCard) {
            theCard.SetParent(target);
            Reorder();
            return true;
		});
    }

    /* riordinamento del mazzo (offset e disposizione carte) */
    public void Reorder() {

        // lock
        if (_ordering) return;
        _ordering = true;

        int count = -1;
        int orderLayer = 0;
        Card previous = null;
        Vector3 newPos = new Vector3();
        Vector2 offset = Offset;

        foreach (Card card in Cards) {

            count++;

            // se c'è una carta precedente ed è previsto un offset tra le carte
            if (previous && CardsHaveOffset) {

                // se ci sono carte con un diverso offset lo uso per CardsWithSecondOffset carte dalla fine
                if (CardsWithSecondOffset > 0) {
                    if (count <= (Cards.Count - CardsWithSecondOffset)) {
                        offset = Offset;
                    } else {
                        offset = OffsetSecond;
                    }
                }

                // inizializzo la nuova posizione su quella della carta precedente
                newPos.x = previous.transform.localPosition.x;
                newPos.y = previous.transform.localPosition.y;

                if (IsMatrioska) { // carte nidificate: la posizione locale corrisponde all'offset
                    newPos.x = offset.x;
                    newPos.y = offset.y;
                } else { // la posizione locale è quella della carta precedente più l'offset
                    newPos.x += offset.x;
                    newPos.y += offset.y;
                }

                // se si tratta di una colonna e la precedente è scoperta aumento l'offset per far vedere il valore
                if (Type == DeckType.COLUMN && !previous.Scoperta) {
                    newPos.y += 0.26f;
                }
            }

            // sposto la carta
            card.transform.localPosition = newPos;
            card.SetSpritesOrderInLayer(orderLayer++);    

            if (Type == DeckType.DRAW) {
                card.Draggable = false;
            }

            previous = card;
        }

		// se è il mazzo carte estratte la prima è draggable
        if (Type == DeckType.DRAW && Top != null) {
            Top.Draggable = true;
        }

        _ordering = false;
    }

    /* mischio le carte del mazzo */
    public void Shuffle() {
        Cards = Cards.OrderBy(o => Guid.NewGuid().ToString()).ToList();
        Reorder();
    }
		
	/* svuoto la lista di carte e elimino i gameObject delle carte contenute */
    public void Clean() {
        foreach (Card c in Cards) {
            Destroy(c.gameObject);
        }
        Cards.Clear();
    }

	/* rimuovo dalla lista di carte la prima carta (top), restituisce true se è stata flippata la carta sopra */
    public bool DiscardTop() {

        if (Top == null) return false;

        Cards.RemoveAt(Cards.Count - 1);

        if (Top == null) return false;

        bool wasScoperta = Top.IsScoperta;

		// se non è il mazzo principale la prima carta è scoperta e trascinabile
        if (Type != DeckType.MAIN) {
            if (!wasScoperta) Top.Scoperta = true;
            Top.Draggable = true;
        }

        Reorder();

        return !wasScoperta;
    }
	
	#endregion
	
	#region events

	/* eseguita quando viene toccato il mazzo principale */
    public void OnTouchDeck() {

        if (GameManager.Instance.Initializing) return;
		if (Type != DeckType.MAIN) return;
		
        if (!IsEmpty) { // se non è vuoto sposto le carte nel mazzo delle estratte
		
            GameManager.TransferCards(this, GameManager.Instance.DrawDeck, (GameOptions.Instance.OptionDraw3 ? 3 : 1), TraslationType.ANIMATE, MoveType.FORWARD, true);

        } else { //se è vuoto rimetto tutte le carte dal mazzo delle estratte nel mazzo principale
		
            GameManager.TransferCards(GameManager.Instance.DrawDeck, this, GameManager.Instance.DrawDeck.Cards.Count, TraslationType.INSTANT, MoveType.FORWARD, true);
            GameManager.Instance.Score = 0;
        }
    }

	#endregion
}