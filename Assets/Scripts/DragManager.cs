using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Whatwapp;
using System.Linq;

/*  Componente che gestisce il drag & drop delle carte
 *  e il touch 
 *  
 */
public class DragManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    #region init

    private Card _card;
    private Vector3 _origin;
    private Vector3 _clickOffset = Vector2.zero;

    public GameObject MaxOverlap;
    private Deck _targetDeck;
    private Dictionary<Collider2D, float> _overlapAreas = new Dictionary<Collider2D, float>(); // aree di intersezione di ogni collider
    private List<Collider2D> _overlaps = new List<Collider2D>(); // tutti i collider sovrapposti
    private Collider2D[] _ownColliders; // tutti i child colliders (da escludere)

    float lastClickTime = 0.0f;
    
    #endregion

    private void Awake() {
        _card = GetComponent<Card>();
    }

    private void Start() {
        _overlaps.Clear();
        _overlapAreas.Clear();
        MaxOverlap = null;
    }


    #region drag manager

    private bool Disabled {
        get {
           return GameManager.Instance.Initializing || GameManager.Instance.DraggingDisabled || GameManager.Instance.GameState == GameState.PAUSE || !_card.Draggable || !_card.Deck.Draggable;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {

        if (Disabled) {
            eventData.pointerDrag = null;
            return;
        }

        _origin = transform.position;
        _card.Dragged = true;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);        
        _clickOffset = getPointerPosition(eventData) - transform.position;
        _ownColliders = transform.GetComponentsInChildren<Collider2D>();

    }

    public void OnDrag(PointerEventData eventData) {

        if (Disabled) {
            eventData.pointerDrag = null;
            return;
        }

        GameManager.Instance.SomeoneIsDragging = true;

        Vector3 destination = getPointerPosition(eventData);

        transform.position = destination - _clickOffset;
        
        if (MaxOverlap) {
            if (MaxOverlap.GetComponent<Deck>() != null) {
                _targetDeck = MaxOverlap.GetComponent<Deck>();
            } else if (MaxOverlap.GetComponent<Card>() != null) {
                _targetDeck = MaxOverlap.GetComponent<Card>().Deck;
            } else {
                _targetDeck = null;
            }
        } else {
            _targetDeck = null;
        }        
    }

    public void OnEndDrag(PointerEventData eventData) {

        clearOverlaps();

        if (Disabled) {
            eventData.pointerDrag = null;
            return;
        }

        _card.Dragged = false;
        transform.localScale = new Vector3(1f, 1f, 1f);

        if (GameManager.Instance.Initializing || !_targetDeck) {
            transform.position = _origin;
        } else {
            if (!_targetDeck.Drop(ref _card, _card.Deck)) {
                transform.position = _origin;
            }
        }

        _clickOffset = Vector3.zero;
        _targetDeck = null;
        GameManager.Instance.SomeoneIsDragging = false;
    }

    private Vector3 getPointerPosition(PointerEventData eventData) {
        Ray ray = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
        return new Vector3((ray.origin + ray.direction).x, (ray.origin + ray.direction).y, 0.0f);
    }

    private void OnMouseDown() {

        // double click event
        if (Time.time - lastClickTime < 0.5) {
            OnDoubleTouch();
        }
        lastClickTime = Time.time;
    }

    private void OnDoubleTouch() {
        Deck target = _card.ReadyToBaseDeck();
        if (target != null) {
            target.Drop(ref _card, _card.Deck);
        } 
    }

    #endregion

    #region overlap manager

    public void OnTriggerStay2D(Collider2D collision) {
        if (_card.Dragged) {

            float area = Utility.OverlapArea(
                Utility.RectOfCollider(collision),
                Utility.RectOfCollider(GetComponent<BoxCollider2D>())
            );

            if (!_ownColliders.Contains(collision)) {
                if (!_overlapAreas.ContainsKey(collision)) {
                    _overlapAreas.Add(collision, area);
                } else {
                    _overlapAreas[collision] = area;
                }
            }

            if (_overlapAreas.Count > 0) {
                MaxOverlap = _overlapAreas.FirstOrDefault(x => x.Value == _overlapAreas.Values.Max()).Key.gameObject;
            }
        }
    }

    public void OnTriggerExit2D(Collider2D collision) {
        _overlaps.Remove(collision);
        _overlapAreas.Remove(collision);
        if (_overlaps.Count == 0) {
            MaxOverlap = null;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (_card.Dragged) {
            _overlaps.Add(collision);
            if (!_overlapAreas.ContainsKey(collision)) {
                _overlapAreas.Add(collision, 0.0f);
            }
        }
    }

    private void clearOverlaps() {
        _overlaps.Clear();
        _overlapAreas.Clear();
        MaxOverlap = null;
    }

    #endregion
}
