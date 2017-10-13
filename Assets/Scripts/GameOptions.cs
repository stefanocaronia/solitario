using UnityEngine;

/*  Oggetto contenente le opzioni (indistruttibile al reload della scena)
 *  
 */
public class GameOptions : MonoBehaviour {

    public static GameOptions Instance;

    // opzioni
    public bool OptionDraw3;
    public bool OptionHints;

    void Awake() {

        // Creo l'istanza singleton
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);

        OptionDraw3 = false;
        OptionHints = false;
    }
}
