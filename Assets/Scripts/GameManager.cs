using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Whatwapp;

public class GameManager : MonoBehaviour {
    
    public static GameManager Instance;

    public GUISkin Skin;

    [SerializeField]
    private int _score;
    public int Score {
        get { return _score; }
        set {
            _score = value;
            ScoreText.text = value.ToString();
        }
    }

    [SerializeField]
    private int _moves;
    public int Moves {
        get { return _moves; }
        set {
            _moves = value;
            MovesText.text = value.ToString();
        }
    }

    public bool PLAY;
    public bool Initializing = true;

    public Deck MainDeck;
    public Deck DrawDeck;
    public Canvas UI;

    public Text ScoreText;
    public Text MovesText;

    public Transform ColumnsContainer;
    public Transform BasesContainer;

    [SerializeField]
    private Deck _deckPrefab;
    
    public Deck[] Columns = new Deck[7];
    public Deck[] Bases = new Deck[4];
    
    float columnWidth = 0.76f;
    float _cardSpeed = 30.0f;

    void Awake () {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);

        ScoreText = UI.transform.Find("Score Bar").Find("Score").GetComponent<Text>();
        MovesText = UI.transform.Find("Score Bar").Find("Moves").GetComponent<Text>();
    }

    void Start() {
        
        if (!PLAY) return;

        // populate main deck
        MainDeck.Populate();
        MainDeck.Shuffle();

        // create columns
        float hPos = -columnWidth;
        for (int cnum = 0; cnum < Columns.Length; cnum++) {
            Columns[cnum] = Instantiate(_deckPrefab, ColumnsContainer);
            Columns[cnum].Type = DeckType.COLUMN;
            Columns[cnum].IsMatrioska = true;
            Columns[cnum].CardsHaveOffset = true;
            Columns[cnum].Offset = new Vector2(0, -0.5f);
            Columns[cnum].transform.localPosition = new Vector3(hPos += columnWidth, 0.0f, 0.0f);
            Columns[cnum].name = "Column #" + cnum;
        }

        // give cards
        StartCoroutine("cGiveCards");

        // init bases
        hPos = -columnWidth;
        Suit s = 0;
        for (int bnum = 0; bnum < Bases.Length; bnum++) {
            Bases[bnum] = Instantiate(_deckPrefab, BasesContainer);
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

    //private void OnGUI() {
    //    //GUI.skin = Skin;
    //}

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

                MainDeck.Discard();
                column.Reorder();
            }           
        }

        Initializing = false;        
    }
}
