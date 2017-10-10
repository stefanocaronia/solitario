using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Whatwapp;
using System.Linq;

public class DragManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    private Card _card;
    private Vector3 _origin;
    private Deck _targetDeck;
    public List<Collider2D> _overlaps = new List<Collider2D>();
    public Collider2D[] _ownColliders;
    private Dictionary<Collider2D, float> _overlapAreas = new Dictionary<Collider2D, float>();
    public GameObject MaxOverlap;

    private Vector3 _clickOffset = Vector2.zero;

    private void Awake() {
        _card = GetComponent<Card>();
    }

    private void Start() {
        _overlaps.Clear();
        _overlapAreas.Clear();
        MaxOverlap = null;
    }

    public void OnBeginDrag(PointerEventData eventData) {

        if (!_card.Draggable || !_card.Deck.Draggable) return;

        _origin = transform.position;
        _card.Dragged = true;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        
        _clickOffset = getPointerPosition(eventData) - transform.position;

        _ownColliders = transform.GetComponentsInChildren<Collider2D>();

    }

    public void OnDrag(PointerEventData eventData) {

        if (!_card.Draggable || !_card.Deck.Draggable) return;
        
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
        _card.Dragged = false;
        
        transform.localScale = new Vector3(1f, 1f, 1f);

        if (GameManager.Instance.Initializing || !_targetDeck) {
            transform.position = _origin;
        } else {
            if (!_targetDeck.Take(_card, _card.Deck)) {
                transform.position = _origin;
            }
        }

        _clickOffset = Vector3.zero;
        _targetDeck = null;
    }

    private Vector3 getPointerPosition(PointerEventData eventData) {
        Ray ray = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
        return new Vector3((ray.origin + ray.direction).x, (ray.origin + ray.direction).y, 0.0f);
    }

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
}
