using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Whatwapp;
using System;
using System.Linq;

public class Deck : MonoBehaviour {
    
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

            if (value == DeckType.MAIN || value == DeckType.DRAW) { CanReceiveDrop = false; } 
            else if (value == DeckType.COLUMN || value == DeckType.BASE) { CanReceiveDrop = true; }
        }
    }
    public Suit Suit;
    public bool IsMatrioska = false;
    public bool Draggable = false;
    public bool CanReceiveDrop = false;

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

    private float _cardSpeed = 10.0f;

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
    
    public bool Drop(ref Card card, Deck sender, MoveDirection direction = MoveDirection.FORWARD) {

        if (!CanReceiveDrop) return false;
        int numCards = card.transform.GetComponentsInChildren<Card>().Length;
        switch (Type) {
            case (DeckType.BASE):
                if (numCards > 1) { return false; }             
                if (card.Suit != Suit) { return false; }
                if (card.Value != (Top?Top.Value + 1:Value.ASSO)) { return false; }
                break;
            case (DeckType.COLUMN):
                if (direction == MoveDirection.REVERSE) break;
                if (IsEmpty && card.Value != Value.RE) { return false; }
                if (Top != null && card.Color == Top.Color) { return false; }
                if (!IsEmpty && card.Value != Top.Value - 1) { return false; }
                break;
        }
       
        if (numCards > 1 && Type != DeckType.BASE) {
            Card[] cards = card.transform.GetComponentsInChildren<Card>();
            Move newMove = new Move(sender, this, ref card);

            for (int i = 0; i < cards.Length; i++) {
                AddCard(ref cards[i], sender);
                sender.DiscardTop();
            }            
            
            if (direction == MoveDirection.FORWARD) {
                int score = CalculateScore(sender, this);
                GameManager.Instance.Score += score;
                newMove.Score += score;
            }

            if (direction == MoveDirection.FORWARD && sender.Type == DeckType.COLUMN && Type == DeckType.COLUMN && sender.Top != null) {
                GameManager.Instance.Score += (int)Scores.FlippedCard;
                newMove.Score += (int)Scores.FlippedCard;
                newMove.Flipped = true;
            }

            if (direction == MoveDirection.FORWARD)
                GameManager.Instance.AddMove(newMove);
            GameManager.Instance.Moves += 1;

        } else {

            Move newMove = new Move(sender, this, ref card);

            AddCard(ref card, sender);
            sender.DiscardTop();

            if (direction == MoveDirection.FORWARD) {
                int score = CalculateScore(sender, this);
                GameManager.Instance.Score += score;
                newMove.Score += score;
            }

            if (direction == MoveDirection.FORWARD && sender.Type == DeckType.COLUMN && sender.Top != null) {
                GameManager.Instance.Score += (int)Scores.FlippedCard;
                newMove.Score += (int)Scores.FlippedCard;
                newMove.Flipped = true;
            }

            if (direction == MoveDirection.FORWARD)
                GameManager.Instance.AddMove(newMove);
            GameManager.Instance.Moves += 1;
        }

        return true;
    }

    public int CalculateScore(Deck sender, Deck receiver) {
        int score = 0;
        if (sender != null) {
            if (receiver.Type == DeckType.BASE && sender.Type == DeckType.COLUMN) {
                score += (int)Scores.FromColumnToBase;
            } else if (receiver.Type == DeckType.BASE && sender.Type == DeckType.DRAW) {
                score += (int)Scores.FromDrawToBase;
            } else if (receiver.Type == DeckType.COLUMN && sender.Type == DeckType.DRAW) {
                score += (int)Scores.FromDrawToColumn;
            }            
        }
        return score;
    }

    IEnumerator cAttractCard(Card card, Deck sender = null) {

        _cAttractCard_running = true;
        bool oldDraggable = Draggable;
        Draggable = false;

        Transform destination = ((IsMatrioska && !IsEmpty) ? Top.transform : transform);
        
        Cards.Add(card);
        card.Deck = this;
        card.Draggable = false;
        card.SetSortingLayerName("Dragged");

        while (card.transform.position != destination.position) {
            card.transform.position = Vector3.MoveTowards(card.transform.position, destination.position, _cardSpeed * Time.deltaTime);
            yield return 0;
        }

        card.transform.SetParent(destination);
        card.SetSortingLayerName("Cards");

        card.Scoperta = (Type != DeckType.MAIN);
        
        Reorder();

        Draggable = oldDraggable;
        _cAttractCard_running = false;
    }    

    public void AddCard(ref Card card, Deck sender = null, Transition transition = Transition.INSTANT) {
        if (_ordering) return;

        if (transition == Transition.ANIMATE) {
            if (!_cAttractCard_running) 
                StartCoroutine(cAttractCard(card, sender));
            return;
        }

        Transform destination = ((IsMatrioska && !IsEmpty) ? Top.transform : transform);

        card.Scoperta = (Type != DeckType.MAIN);

        Cards.Add(card);
        card.Deck = this;

        card.gameObject.transform.SetParent(destination);

        if (Type == DeckType.COLUMN) card.Draggable = card.Scoperta;

        Reorder();
    }
    

    public void Reorder() {

        _ordering = true;
        int orderLayer = 0;
        Card previous = null;
        Vector3 newPos = new Vector3();
        int count = -1;
        Vector2 offset = Offset;
        foreach (Card c in Cards) {
            count++;

            if (Type == DeckType.DRAW) {
                c.Draggable = false;
            }

            if (previous && CardsHaveOffset) {

                if (CardsWithSecondOffset > 0) {
                    if (count <= (Cards.Count - CardsWithSecondOffset)) {
                        offset = Offset;
                    } else {
                        offset = OffsetSecond;
                    }
                }
                
                newPos.x = previous.transform.localPosition.x;
                newPos.y = previous.transform.localPosition.y;

                if (!IsMatrioska) {
                    newPos.x += offset.x;
                    newPos.y += offset.y;
                } else {
                    newPos.x = offset.x;
                    newPos.y = offset.y;
                }

                if (Type == DeckType.COLUMN && !previous.Scoperta) {
                    newPos.y += 0.26f;
                }
            }

            //Debug.Log(Type + " " + c.name + " " + newPos.ToString());
            c.gameObject.transform.localPosition = newPos;
            
            c.SetSpritesOrderInLayer(orderLayer++);

            previous = c;            
        }

        if (Type == DeckType.DRAW) {
            Top.Draggable = true;
        }

        _ordering = false;
    }
    
    public void Shuffle() {
        Cards = Cards.OrderBy(o => Guid.NewGuid().ToString()).ToList();
        Reorder();
    }

    public void DiscardTop() {
        Cards.RemoveAt(Cards.Count - 1);
        if (Type != DeckType.MAIN  && Top != null) {
            Top.Scoperta = true;
            Top.Draggable = true;
        }
    }

    public void TransferCardsToDeck(Deck otherDeck, bool scopri = false, int numCards = 1, Transition mode = Transition.INSTANT, MoveDirection direction = MoveDirection.FORWARD, bool registerAsOneMove = false) {
        if (!Top) return;
        for (int c = 0; c < numCards; c++) {
            if (!Top) return;
            Card cref = Top;
            otherDeck.AddCard(ref cref, this, mode);
            
            if (direction == MoveDirection.FORWARD && !registerAsOneMove) {
                Move newMove = new Move(this, otherDeck, ref cref);
                GameManager.Instance.AddMove(newMove);
               
            }            

            if (!registerAsOneMove)
                GameManager.Instance.Moves += 1;

            DiscardTop();

            int score = CalculateScore(this, otherDeck);
            GameManager.Instance.Score += score;
        }

        if (direction == MoveDirection.FORWARD && registerAsOneMove) {
            Card cref = Top;
            Move newMove = new Move(this, otherDeck, ref cref, false, 0, numCards);
            GameManager.Instance.AddMove(newMove);
           
        }

        if (registerAsOneMove)
            GameManager.Instance.Moves += 1;
    }

    public void OnTouchCard() {
        if (GameManager.Instance.Initializing) return;

        if (Type == DeckType.MAIN && !IsEmpty) {
            TransferCardsToDeck(GameManager.Instance.DrawDeck, true, 1, Transition.ANIMATE);
        } else {
            GameManager.Instance.DrawDeck.TransferCardsToDeck(this, false, GameManager.Instance.DrawDeck.Cards.Count, Transition.INSTANT, MoveDirection.FORWARD, true);
            GameManager.Instance.Score = 0;
        }
    }

    public void Clean() {
        foreach (Card c in Cards) {
            Destroy(c.gameObject);
        }
        Cards.Clear();
    }
}

