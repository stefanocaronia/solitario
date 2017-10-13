using System.Collections;
using Whatwapp;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/*  Componente Carta da gioco
 *  
 */
[System.Serializable]
public class Card : MonoBehaviour {
   
    #region init

    public Deck Deck;

    public float FlipSpeed;
    public bool doFlip;
    public bool Draggable;	
	public bool Moving;

    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer valoreRenderer;
    private SpriteRenderer figuraRenderer;
    private SpriteRenderer semeRenderer;

    public List<Move> PossibileMoves = new List<Move>();
    public Move NextBestMove;

    // attivo mentre la carta viene trascinata
    private bool _dragged;
    public bool Dragged {
        get {
            return _dragged;
        }
        set {
            _dragged = value;
            if (value) {
                SetSortingLayerName("Dragged");               
            } else {
                SetSortingLayerName("Cards");
            }
        }
    }

    // ricavo il colore del seme
    public Whatwapp.Color Color {
        get {
            return Tables.SuitsColors[Suit];
        }
    }

    // verifico se di fatto la carta è scoperta
    public bool IsScoperta {
        get { return valoreRenderer.enabled; }
    }

    [SerializeField]
    private Value _value;
    public Value Value {
        get {
            return _value;
        }
        set {
            _value = value;
            valoreRenderer.sprite = Resources.Load<Sprite>("carte/numeri/" + getSpriteFromValore(value));
            UpdateFigure();
        }
    }

    [SerializeField]
    private Suit _suit;
    public Suit Suit {
        get {
            return _suit;
        }
        set {
            _suit = value;
            semeRenderer.sprite = Resources.Load<Sprite>("carte/semi/" + value.ToString().ToLower());
            if (Color == Whatwapp.Color.NERO) {
                valoreRenderer.color = UnityEngine.Color.black;
            } else if (Color == Whatwapp.Color.ROSSO) {
                valoreRenderer.color = UnityEngine.Color.red;
            }
            UpdateFigure();
        }
    }

    [SerializeField]
    private bool _scoperta;
    private bool _flipping;

    public bool Scoperta {
        get {
            return _scoperta;
        }
        set {           
            if (value && !IsScoperta) {
                if (doFlip)
                    StartCoroutine("cFlip");
               else
                    Scopri();
            } else if (!value && IsScoperta) {
                Nascondi();
            }
            _scoperta = value;
        }
    }

    #endregion
    
    void Awake() {
        backgroundRenderer = GetComponent<SpriteRenderer>();
        valoreRenderer = transform.Find("Value").GetComponent<SpriteRenderer>();
        figuraRenderer = transform.Find("Picture").GetComponent<SpriteRenderer>();
        semeRenderer = transform.Find("Suit").GetComponent<SpriteRenderer>();
    }

    #region AI

    // trova la mossa migliore da fare
    public void FindBestMove() {

        if (Deck == null) return;

        // se la carta è coperta cancello le mosse e me ne vado
        if (!Scoperta) {
            PossibileMoves.Clear();
            return;
        }

        // svuoto la lista delle mosse
        PossibileMoves.Clear();

        Move move;

        // testo lo spostamento della carta su ogni colonna
        foreach (Deck target in GameManager.Instance.Columns) {
            if (target != Deck && target.Accept(this)) { // solo se la colonna accetta la mia carta
                move = new Move();
                move.Card = this;
                move.Sender = Deck;
                move.Receiver = target;
                if (Deck.Type == DeckType.BASE) continue; // se provengo da una base non è una buona mossa
                if (Deck.Type == DeckType.COLUMN && transform.parent.GetComponent<Card>() != null && transform.parent.GetComponent<Card>().Scoperta) continue; // se mi sposto da una carta già scoperta non serve a nulla 
                if (Value == Value.RE && Deck.Cards.First() == this) continue; // se sposto un re che è già al top di colonna, non è una grande mossa
                if (Deck.Type == DeckType.COLUMN && Deck.Cards.Count == 1) move.Weight += 1; // se libero una colonna è bene
                if (Deck.Type == DeckType.COLUMN && transform.parent.GetComponent<Card>() != null && !transform.parent.GetComponent<Card>().Scoperta) move.Weight += 2; // se libero una carta è bene
                if (Value == Value.RE) move.Weight += 3; // se ho messo un re su una colonna libera è ancora meglio
                PossibileMoves.Add(move);
            }
        }

        foreach (Deck target in GameManager.Instance.Bases) {
            if (target != Deck && target.Accept(this)) { // solo se la base accetta la mia carta 
                move = new Move();
                move.Card = this;
                move.Sender = Deck;
                move.Receiver = target;
                move.Weight += 4; // ovviamente se metto una cosa in base è la mossa migliore
                PossibileMoves.Add(move);
            }
        }

        // Ordino la lista delle mosse e prendo quella con peso maggiore
        PossibileMoves = PossibileMoves.OrderBy(x => x.Weight).Reverse().ToList();
        if (PossibileMoves.Count > 0)
            NextBestMove = PossibileMoves.First();
        else
            NextBestMove = null;
    }

    public Deck ReadyToBaseDeck() {
        if (this != Deck.Top) return null;
        foreach (Deck target in GameManager.Instance.Bases) {
            if (target.Accept(this)) return target;
        }
        return null;
    }

    #endregion

    #region visualizzazione

    // setto la figura della carta
    public void UpdateFigure() {
        if ((int)Value > 10)
            figuraRenderer.sprite = Resources.Load<Sprite>("carte/figure/" + Color.ToString().ToLower() + "/" + getSpriteFromValore(Value));
        else
            figuraRenderer.sprite = Resources.Load<Sprite>("carte/semi/" + Suit.ToString().ToLower());

        // setto il nome della carta
        name = Value.ToString() + " DI " + Suit.ToString();
    }

    // mostro la carta scoperta
    public void Scopri() {
        backgroundRenderer.sprite = Resources.Load<Sprite>("carte/fronte");
        valoreRenderer.enabled = true;
        figuraRenderer.enabled = true;
        semeRenderer.enabled = true;
    }

    // Mostro il retro della carta
    public void Nascondi() {
        backgroundRenderer.sprite = Resources.Load<Sprite>("carte/retro-carte");
        valoreRenderer.enabled = false;
        figuraRenderer.enabled = false;
        semeRenderer.enabled = false;
    }

    // ottengo il nome della risorsa sprite per ogni valore
    private string getSpriteFromValore(Value val) {
        switch (val) {
            case Value.ASSO:
                return "A";
            case Value.DUE:
            case Value.TRE:
            case Value.QUATTRO:
            case Value.CINQUE:
            case Value.SEI:
            case Value.SETTE:
            case Value.OTTO:
            case Value.NOVE:
            case Value.DIECI:
                return ((int)val).ToString();
            case Value.JACK:
                return "J";
            case Value.DONNA:
                return "Q";
            case Value.RE:
                return "K";
            default:
                return "";
        }
    }

	#endregion
	
    #region comportamento

    // setto l'ordine degli sprite nel layer
    public void SetSpritesOrderInLayer(int num) {
        if (transform.GetComponent<SpriteRenderer>() != null) {
            transform.GetComponent<SpriteRenderer>().sortingOrder = num;
        }

        SpriteRenderer[] srs = transform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs) {
            sr.sortingOrder = num;
        }
    }

    // setto il sorting layer di tutti gli sprite
    public void SetSortingLayerName(string layer) {
        if (transform.GetComponent<SpriteRenderer>() != null) {
            transform.GetComponent<SpriteRenderer>().sortingLayerName = layer;
        }

        SpriteRenderer[] srs = transform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs) {
            sr.sortingLayerName = layer;
        }
    }
	
	/* rende una carta child di un mazzo o di un'altra carta */
    public void SetParent(Transform destination) {
       
        transform.SetParent(destination); // il mazzo di destinazione diventa il parent della carta  
        Scoperta = (Deck.Type != DeckType.MAIN); // scopro subito la carta (se la destinazione non è il mazzo principale)      
        if (Deck.Type == DeckType.COLUMN) Draggable = Scoperta; // se la destinazione è una colonna, imposto la carta a trascinabile solo se è scoperta
        Deck.Reorder(); // riordinamento mazzo
    }
	
	public void Move(Transform target, TraslationType transition = TraslationType.INSTANT, Func<Card, bool> callback = null) {
		StartCoroutine(cMove(target, transition, callback));
	}

    #endregion
       
    #region coroutines	

    public void MovePhantom(Vector3 position) {
        StartCoroutine(cMovePhantom(position));
    }

    public void SetAlpha(float alpha) {

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        UnityEngine.Color color;

        foreach (SpriteRenderer sr in renderers) {
            color = sr.material.color;
            color.a = alpha;
            sr.material.color = color;
        }
    }

    public IEnumerator cMovePhantom(Vector3 position) {

        if (Moving) yield break;
        Moving = true;

        Card Phantom = Instantiate(this, transform);
        Phantom.SetSortingLayerName("Dragged");

        Phantom.GetComponent<BoxCollider2D>().enabled = false;
        Phantom.GetComponent<DragManager>().enabled = false;

        Phantom.SetAlpha(0.8f);

        // ciclo per spostamento
        while (Phantom.transform.position != position && !GameManager.Instance.SomeoneIsDragging) {
            Phantom.transform.position = Vector3.MoveTowards(Phantom.transform.position, position, (GameManager.Instance.CardSpeed/2) * Time.deltaTime);
            yield return 0;
        }

        float alf = 1.0f;
        // ciclo per dissolvenza
        while (Phantom.GetComponent<SpriteRenderer>().material.color.a > 0.0f && !GameManager.Instance.SomeoneIsDragging) {
            Phantom.SetAlpha(alf -= 1.6f * Time.deltaTime);
            yield return 0;
        }

        Phantom.SetSortingLayerName("Cards"); // porto la carta in background

        Destroy(Phantom.gameObject);
        Moving = false;
    }

    /* sposta una carta fino a un target ed esegue il callback */
    IEnumerator cMove(Transform target, TraslationType transition = TraslationType.INSTANT, Func<Card, bool> callback = null) {

        if (transition == TraslationType.ANIMATE) {

            GameManager.Instance.DraggingDisabled = true;
            
            SetSortingLayerName("Dragged"); // porto la carta in foreground
           
            // ciclo per spostamento
            while (transform.position != target.position) {
                transform.position = Vector3.MoveTowards(transform.position, target.position, GameManager.Instance.CardSpeed * Time.deltaTime);
                //yield return new WaitForSeconds(0.2f);
                yield return new WaitForFixedUpdate();
            }

            SetSortingLayerName("Cards"); // porto la carta in background
        } else {
            transform.position = target.position;
        }
        
		// eseguo il callback
		callback(this);
        GameManager.Instance.DraggingDisabled = false;
    }

    // animazione di flip della carta
    IEnumerator cFlip() {

        if (_flipping) yield break;
        _flipping = true;

        GameManager.Instance.DraggingDisabled = true;

        float originalScale = transform.localScale.x;
        while (transform.localScale.x > 0.0f) {
            transform.localScale = new Vector3(transform.localScale.x - (FlipSpeed * Time.deltaTime), 1.0f, 1.0f);            
            yield return 0;
        }

        if (IsScoperta) Nascondi(); else Scopri();

        while (transform.localScale.x < originalScale) {
            transform.localScale = new Vector3(transform.localScale.x + (FlipSpeed * Time.deltaTime), 1.0f, 1.0f);
            yield return 0;
        }

        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

        _flipping = false;
        GameManager.Instance.DraggingDisabled = false;
    }

    #endregion
}