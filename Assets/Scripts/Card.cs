using System.Collections;
using System.Collections.Generic;
using Whatwapp;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Card : MonoBehaviour {

    SpriteRenderer backgroundRenderer;
    SpriteRenderer valoreRenderer;
    SpriteRenderer figuraRenderer;
    SpriteRenderer semeRenderer;

    public Deck Deck;

    public float FlipSpeed = 20.0f;
    public bool doFlip = true;

    public bool Draggable = false;

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

    public Whatwapp.Color Color {
        get {
            return Tables.SuitsColors[Suit];
        }
    }

    public bool IsScoperta {
        get { return valoreRenderer.enabled; }
    }

    [SerializeField]
    private Value _valore;
    public Value Valore {
        get {
            return _valore;
        }
        set {
            _valore = value;
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

    [SerializeField]
    private bool _visibile;
    public bool Visibile {
        get {
            return _visibile;
        }
        set {
            _visibile = value;
            gameObject.SetActive(value);
        }
    }

    void Awake() {
        backgroundRenderer = GetComponent<SpriteRenderer>();
        valoreRenderer = transform.Find("Value").GetComponent<SpriteRenderer>();
        figuraRenderer = transform.Find("Picture").GetComponent<SpriteRenderer>();
        semeRenderer = transform.Find("Suit").GetComponent<SpriteRenderer>();
    }

    private void Start() {
        //Nascondi();
        //StartCoroutine("cScopri");
    }
    public void UpdateFigure() {
        if ((int)Valore > 10)
            figuraRenderer.sprite = Resources.Load<Sprite>("carte/figure/" + Color.ToString().ToLower() + "/" + getSpriteFromValore(Valore));
        else
            figuraRenderer.sprite = Resources.Load<Sprite>("carte/semi/" + Suit.ToString().ToLower());

        name = Valore.ToString() + " DI " + Suit.ToString();
    }

    public void Scopri() {
        backgroundRenderer.sprite = Resources.Load<Sprite>("carte/fronte");
        valoreRenderer.enabled = true;
        figuraRenderer.enabled = true;
        semeRenderer.enabled = true;
    }

    public void Nascondi() {
        backgroundRenderer.sprite = Resources.Load<Sprite>("carte/retro-carte");
        valoreRenderer.enabled = false;
        figuraRenderer.enabled = false;
        semeRenderer.enabled = false;
    }

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

    public void SetSpritesOrderInLayer(int num) {
        if (transform.GetComponent<SpriteRenderer>() != null) {
            transform.GetComponent<SpriteRenderer>().sortingOrder = num;
        }

        SpriteRenderer[] srs = transform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs) {
            sr.sortingOrder = num;
        }
    }

    public void SetSortingLayerName(string layer) {
        if (transform.GetComponent<SpriteRenderer>() != null) {
            transform.GetComponent<SpriteRenderer>().sortingLayerName = layer;
        }

        SpriteRenderer[] srs = transform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs) {
            sr.sortingLayerName = layer;
        }
    }
    
    IEnumerator cFlip() {
        float originalScale = transform.localScale.x;
        while (transform.localScale.x > 0.0f) {
            transform.localScale = new Vector3(
                transform.localScale.x - (FlipSpeed * Time.deltaTime), 1.0f, 1.0f
            );            
            yield return 0;
        }

        if (IsScoperta) Nascondi(); else Scopri();

        while (transform.localScale.x < originalScale) {
            transform.localScale = new Vector3(
                transform.localScale.x + (FlipSpeed * Time.deltaTime), 1.0f, 1.0f
            );
            yield return 0;
        }

        transform.localScale = new Vector3(
                1.0f, 1.0f, 1.0f
            );
    }

}