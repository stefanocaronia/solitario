using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Whatwapp;
using System;
using System.Linq;

public class Deck : MonoBehaviour {

    public bool empty = false;

    [SerializeField]
    private DeckType _type;
    public DeckType Type {
        get {
            return _type;
        }
        set {
            _type = value;
            if (value == DeckType.BASE || value == DeckType.MAIN) { Draggable = false;
            } else if (value == DeckType.COLUMN || value == DeckType.DRAW) { Draggable = true; }

            if (value == DeckType.MAIN || value == DeckType.DRAW) { Droppable = false; } 
            else if (value == DeckType.COLUMN || value == DeckType.BASE) { Droppable = true; }
        }
    }
    public Suit Suit;
    public bool IsMatrioska = false;
    public bool Draggable = false;
    public bool Droppable = false;

    private bool _cAttractCard_running = false;

    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _suitRenderer;

    private bool _ordering;

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

    private bool _showSuit = false;
    public bool ShowSuit {
        get {
            return _showSuit;
        }
        set {
            _suitRenderer.enabled = value;
        }
    }

    

    public List<Card> Cards;
    public Card _cardPrefab;

    private float _cardSpeed = 20.0f;

    public bool CardsHaveOffset;
    public Vector2 Offset = Vector2.zero;
    public Vector2 OffsetSecond = Vector2.zero;

    public int CardsWithSecondOffset = 0; // numero di carte da spostare
   
    public Card Top {
        get {
            return (Cards.Count > 0 ? Cards[Cards.Count - 1] : null);
        }
    }

    public bool IsEmpty {
        get {
            return (Cards.Count == 0);
        }
    }

    void Awake() {
        _backgroundRenderer = GetComponent<SpriteRenderer>();
        _suitRenderer = transform.Find("Suit").GetComponent<SpriteRenderer>();
    }

    private void Update() {
        empty = IsEmpty;
    }

    public bool Take(Card card, Deck sender) {
        if (!Droppable) return false;
        int numCards = card.transform.GetComponentsInChildren<Card>().Length;
        switch (Type) {
            case (DeckType.BASE):
                if (numCards > 1) { return false; }             
                if (card.Suit != Suit) { return false; }
                if (card.Valore != (Top?Top.Valore + 1:Value.ASSO)) { return false; }
                break;
            case (DeckType.COLUMN):
                if (IsEmpty && card.Valore != Value.RE) {return false; }
                if (Top != null && card.Color == Top.Color) {return false; }
                if (!IsEmpty && card.Valore != Top.Valore - 1) {return false; }
                break;
        }
       
        if (numCards > 1) {
            Card[] cards = card.transform.GetComponentsInChildren<Card>();           
            foreach (Card c in cards) {
                Add(c, sender);
                sender.Discard();
            }

            if (sender.Type == DeckType.COLUMN && Type == DeckType.COLUMN && sender.Top != null) {
                GameManager.Instance.Score += (int)Scores.FlippedCard;
            }

        } else {
            Add(card, sender);
            sender.Discard();

            if (sender.Type == DeckType.COLUMN && Type == DeckType.COLUMN && sender.Top != null) {
                GameManager.Instance.Score += (int)Scores.FlippedCard;
            }
        }
        return true;
    }

    IEnumerator cAttractCard(Card card, Deck sender = null) {

        bool oldDraggable = Draggable;
        Draggable = false;

        Transform destination = ((IsMatrioska && !IsEmpty) ? Top.transform : transform);
        
        Cards.Add(card);
        card.Deck = this;
        card.SetSortingLayerName("Dragged");

        while (card.transform.position != destination.position) {
            card.transform.position = Vector3.MoveTowards(card.transform.position, destination.position, _cardSpeed * Time.deltaTime);
            yield return 0;
        }
       
        card.gameObject.transform.SetParent(destination);
        card.SetSortingLayerName("Cards");

        updateScore(sender);
        Reorder();

        Draggable = oldDraggable;
    }

    public void MoveAndAdd(Card card, Deck sender = null) {
        StartCoroutine(cAttractCard(card, sender));
    }

    public void Add(Card card, Deck sender = null) {

        Transform destination = ((IsMatrioska && !IsEmpty) ? Top.transform : transform);

        Cards.Add(card);
        card.Deck = this;

        card.gameObject.transform.SetParent(destination);

        if (Type == DeckType.COLUMN) card.Draggable = card.Scoperta;

        updateScore(sender);

        Reorder();

    }
    
    private void updateScore(Deck sender) {
        if (sender != null) {
            if (Type == DeckType.BASE && sender.Type == DeckType.COLUMN) {
                GameManager.Instance.Score += (int)Scores.FromColumnToBase;
            } else if (Type == DeckType.BASE && sender.Type == DeckType.DRAW) {
                GameManager.Instance.Score += (int)Scores.FromDrawToBase;
            } else if (Type == DeckType.COLUMN && sender.Type == DeckType.DRAW) {
                GameManager.Instance.Score += (int)Scores.FromDrawToColumn;
            } 
            GameManager.Instance.Moves += 1;
        }
    }

    public void Reorder() {
        int orderLayer = 0;
        Card previous = null;
        Vector3 newPos = new Vector3(0.0f, 0.0f, 0.0f);
        int count = -1;
        Vector2 offset = Offset;
        foreach (Card c in Cards) {
            count++;
            if (previous && CardsHaveOffset) {

                if (CardsWithSecondOffset > 0) {
                    if (count <= (Cards.Count - CardsWithSecondOffset)) {
                        offset = Offset;
                    } else {
                        offset = OffsetSecond;
                    }
                }

                if (!IsMatrioska) {
                    newPos.x = previous.gameObject.transform.localPosition.x + offset.x;
                    newPos.y = previous.gameObject.transform.localPosition.y + offset.y;
                } else {
                    newPos.x = offset.x;
                    newPos.y = offset.y;
                }

                if (Type == DeckType.COLUMN && !previous.Scoperta) {
                    newPos.y += 0.26f;
                }
            }

            c.gameObject.transform.localPosition = newPos;
            
            c.SetSpritesOrderInLayer(orderLayer++);

            previous = c;

            if (Type == DeckType.DRAW) {
                c.Draggable = false;
            }
        }

        if (Type == DeckType.DRAW) {
            Top.Draggable = true;
        }
    }

    public void Populate() {
        foreach (Suit seme in Enum.GetValues(typeof(Suit))) {
            foreach (Value valore in Enum.GetValues(typeof(Value))) {
                Card c = Instantiate(_cardPrefab, transform);
                c.Valore = valore;
                c.Suit = seme;
                c.Scoperta = false;
                c.Visibile = true;               
                Add(c);
            }
        }        
    }

    public void Shuffle() {
        Cards = Cards.OrderBy(o => Guid.NewGuid().ToString()).ToList();
        Reorder();
    }

    public void Discard() {
        Cards.RemoveAt(Cards.Count - 1);
        if (Type != DeckType.MAIN  && Top != null) {
            Top.Scoperta = true;
            Top.Draggable = true;
        }
    }

    public void Transfer(Deck otherDeck, bool scopri = false, int numCards = 1, bool move = false) {        
        for (int c = 0; c < numCards; c++) {
            if (!Top) return;
            Top.Scoperta = scopri;
            if (move) {
                otherDeck.MoveAndAdd(Top, this);
            } else {
                otherDeck.Add(Top, this);
            }

            Discard();
        }
    }

    public void OnTouchCard() {
        if (GameManager.Instance.Initializing) return;

        if (Type == DeckType.MAIN && !IsEmpty) {
            Transfer(GameManager.Instance.DrawDeck, true, 1, true);
        } else {
            GameManager.Instance.DrawDeck.Transfer(this, false, GameManager.Instance.DrawDeck.Cards.Count, false);
            GameManager.Instance.Score = 0;
        }
    }
}


public static class IEnumerableExtensions {

    public static IEnumerable<t> Randomize<t>(this IEnumerable<t> target) {
        System.Random r = new System.Random();
        return target.OrderBy(x => (r.Next()));
    }
}
