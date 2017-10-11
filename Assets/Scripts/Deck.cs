using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Whatwapp;
using System;
using System.Linq;

public class Deck : MonoBehaviour {

    public List<Card> Cards; // lista delle carte
    public Suit Suit; // seme del mazzo (si usa nelle basi)

    public bool IsMatrioska; // ogni carta � un child della carta precedente (si usa nelle colonne)
    public bool Draggable; // Se false le carte del mazzo non sono draggable
    public bool CanReceiveDrop; // pu� ricevere carte trascinate sopra dal giocatore
    public bool CardsHaveOffset; // le carte si dispongono nel mazzo shiftwate di un offset
    public Vector2 Offset = Vector2.zero; // offset tra le carte
    public Vector2 OffsetSecond = Vector2.zero; // secondo offset tra le carte
    public int CardsWithSecondOffset = 0; // numero di carte con un secondo offset

    private float _cardSpeed = 10.0f; // velocit� di traslazione delle carte

    private bool _cMoveCard_running; // � in corso il movimento della carta
    private bool _ordering; // � in corso il riordinamento del mazzo

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

            // le carte del mazzo main non sono draggabili, si pu� solo cliccare
            Draggable = _type != DeckType.MAIN;

            // solo le colonne o le basi possono ricevere il drop
            CanReceiveDrop = _type == DeckType.COLUMN || _type == DeckType.BASE;
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

    // true se il mazzo � vuoto
    public bool IsEmpty {
        get {
            return (Cards.Count == 0);
        }
    }

	// inizializzo la carta
    void Awake() {
        _backgroundRenderer = GetComponent<SpriteRenderer>();
        _suitRenderer = transform.Find("Suit").GetComponent<SpriteRenderer>();
    }

    /* il mazzo riceve il Drop di una o pi� carte da parte del giocatore */
    public bool Drop(ref Card card, Deck sender, MoveDirection direction = MoveDirection.FORWARD) {

        if (!CanReceiveDrop) return false;
        
        // numero di carte trascinate (nel caso di matrioska le carte sono contenute nella carta trascinata)
        int numCards = card.transform.GetComponentsInChildren<Card>().Length;

        // decido cosa impedire in base al tipo di mazzo di destinazione (this.Type) e i criteri di gioco
        switch (Type) {

            // il mazzo di destinazione � una base
            case (DeckType.BASE):
                if (numCards > 1) return false; // la base pu� ricevere solo una carta alla volta
                if (card.Suit != Suit) return false; // il seme della carta deve corrispondere a quello della base
                if (card.Value != (Top ? Top.Value + 1 : Value.ASSO)) return false; // il valore della carta deve essere la precedente + 1 o un asso se il mazzo � vuoto
                break;

            // il mazzo di destinazione � una colonna
            case (DeckType.COLUMN):
                if (direction == MoveDirection.REVERSE) break; // se � il reverse di una mossa (funzionalit� undo) permetti sempre il drop
                if (IsEmpty && card.Value != Value.RE) return false; // se la colonna � vuota accetta solo un re
                if (Top != null && card.Color == Top.Color) return false; // la carta non deve avere lo stesso colore della precedente
                if (!IsEmpty && card.Value != Top.Value - 1) return false; // la carta deve avere un valore decrescente
                break;
        }

        // inizializzo la mossa da salvare in history
        Move newMove = new Move(sender, this, ref card);

        bool flipped = false;

        // sono state trascinate pi� carte assieme
        if (numCards > 1 && Type != DeckType.BASE) {

            // ottengo tutte le carte della catena
            Card[] cards = card.transform.GetComponentsInChildren<Card>();
            
            // aggiungo tutte le carte al mazzo di destinazione
            for (int i = 0; i < cards.Length; i++) {
                AddCard(ref cards[i], sender);
                flipped = sender.DiscardTop(); // controllare: andrebbero tolte in senso inverso
            }

        } else {            

            // aggiungo la carta al mazzo di destinazione
            AddCard(ref card, sender);
            flipped = sender.DiscardTop();
        }

        // se non � una mossa reverse aggiungo punteggio e numero di mosse
        if (direction == MoveDirection.FORWARD) {
            int score = GameManager.CalculateScore(sender, this);
            newMove.Score += score;
        }

        // se non � una mossa reverse, il sender � una colonna, ed � rimasta una carta coperta libera, aggiorno il punteggio e la mossa
        if (direction == MoveDirection.FORWARD && sender.Type == DeckType.COLUMN && sender.Top != null && flipped) {            
            newMove.Score += (int)Scores.FlippedCard;
            newMove.Flipped = true;
        }

        // se il trascinamento viene da una base allora sottraggo il punteggio
        if (sender.Type == DeckType.BASE) {
            int score = GameManager.CalculateScore(this, sender);
            newMove.Score -= score;
        }

        // se non � una mossa reverse, aggiungo la mossa alla history
        if (direction == MoveDirection.FORWARD ) { // && !GameManager.Instance.Goals.Contains(newMove)
            
            // aggiorno il punteggio
            GameManager.Instance.Score += newMove.Score;
            GameManager.Instance.Goals.Add(newMove);

            // registro la mossa
            GameManager.Instance.RegisterMove(newMove);
        }
        
        // incremento il numero di mosse
        GameManager.Instance.Moves += 1;
        
        return true;
    }

    /* Aggiunge una carta al mazzo */
    public void AddCard(ref Card card, Deck sender = null, TraslationType transition = TraslationType.INSTANT) {

        // lock
        if (_ordering) return;        

        // se � una matrioska e non piena la destinazione � l'ultima carta, se no il mazzo
        Transform destination = (!IsEmpty && IsMatrioska ? Top.transform : transform);

        // scopro subito la carta (se la destinazione non � il mazzo principale)
        card.Scoperta = (Type != DeckType.MAIN);

		Cards.Add(card); // aggiungo l'oggetto carta alla lista
        card.Deck = this; // imposto la reference del mazzo	
        
        // se � stata richiesta una transizione animata passo alla coroutine
        if (transition == TraslationType.ANIMATE && !_cMoveCard_running) {
            StartCoroutine(cMoveCard(card, destination));
            return;
        }

        attachCardToParent(card, destination);
    }

    /* rende una carta child di un mazzo o di un'altra carta */
    private void attachCardToParent(Card card, Transform destination) {

        // il mazzo di destinazione diventa il parent della carta
        card.transform.SetParent(destination);        

        // scopro subito la carta (se la destinazione non � il mazzo principale)
        card.Scoperta = (Type != DeckType.MAIN);

        // se la destinazione � una colonna, imposto la carta a trascinabile solo se � scoperta
        if (Type == DeckType.COLUMN) card.Draggable = card.Scoperta;

        // riordinamento
        Reorder();
    }

    #region operations

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

            // se c'� una carta precedente ed � previsto un offset tra le carte
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
                } else { // la posizione locale � quella della carta precedente pi� l'offset
                    newPos.x += offset.x;
                    newPos.y += offset.y;
                }

                // se si tratta di una colonna e la precedente � scoperta aumento l'offset per far vedere il valore
                if (Type == DeckType.COLUMN && !previous.Scoperta) {
                    newPos.y += 0.26f;
                }
            }

            // sposto la carta
            card.transform.localPosition = newPos;
            card.SetSpritesOrderInLayer(orderLayer++);

            previous = card;
        }

		// se � il mazzo carte estratte la prima � draggable
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

	/* rimuovo dalla lista di carte la prima carta (top), restituisce true se � stata flippata la carta sopra */
    public bool DiscardTop() {

        if (Top == null) return false;

        Cards.RemoveAt(Cards.Count - 1);

        if (Top == null) return false;

        bool wasScoperta = Top.IsScoperta;

		// se non � il mazzo principale la prima carta � scoperta e trascinabile
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
		
        if (!IsEmpty) { // se non � vuoto sposto le carte nel mazzo delle estratte
		
            GameManager.TransferCards(this, GameManager.Instance.DrawDeck, (GameManager.Instance.OptionDraw3 ? 3 : 1), TraslationType.ANIMATE, MoveDirection.FORWARD, true);
			
        } else { //se � vuoto rimetto tutte le carte dal mazzo delle estratte nel mazzo principale
		
            GameManager.TransferCards(GameManager.Instance.DrawDeck, this, GameManager.Instance.DrawDeck.Cards.Count, TraslationType.INSTANT, MoveDirection.FORWARD, true);
            GameManager.Instance.Score = 0;
        }
    }

	#endregion

    #region coroutines

	/* sposta una carta animando la traslazione e la aggiunge al parent */
    IEnumerator cMoveCard(Card card, Transform destination) {

        _cMoveCard_running = true;

        bool oldDraggable = Draggable;
        Draggable = false;
        card.SetSortingLayerName("Dragged"); // porto la carta in foreground

        // ciclo per spostamento
        while (card.transform.position != destination.position) {
            card.transform.position = Vector3.MoveTowards(card.transform.position, destination.position, _cardSpeed * Time.deltaTime);
            yield return 0;
        }
        card.SetSortingLayerName("Cards"); // porto la carta in background

        Draggable = oldDraggable;
        attachCardToParent(card, destination);

        _cMoveCard_running = false;
    }    

    #endregion
}