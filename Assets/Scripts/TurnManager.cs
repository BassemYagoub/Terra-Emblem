using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TurnManager : MonoBehaviour {

    //teamTag : unitsFromTeam
    static Dictionary<string, List<TacticsMove>> units = new Dictionary<string, List<TacticsMove>>();
    static Queue<string> turnKey = new Queue<string>(); //whose turn
    static Queue<TacticsMove> turnTeam = new Queue<TacticsMove>(); //turn for each unit of playing team
    static TurnManager turnManager;
    
    static bool gameEnded = false;
    static int turnNumber = 1;

    public GameObject turnPanel; //panel
    public GameObject[] hiddenObjects; //objects to be shown at a certain turn
    private Text panelText; //text inside panel indicating the team
    float changingDuration = 1.5f;
    bool eventTriggered = false;
    bool newTurn = true; //bool to know if change of team turn

    List<bool> triggers = new List<bool>(); //list of triggers to enter only once in event

    //force Player to be first to play
    private void Awake() {
        units["Player"] = new List<TacticsMove>();
        turnKey.Enqueue("Player");
        triggers.Add(false); //only one event to be triggered in the current state of game
        gameEnded = false;
        turnNumber = 1;
    }

    // Start is called before the first frame update
    void Start() {
        turnManager = this;
        panelText = turnPanel.GetComponentInChildren<Text>();
        turnPanel.SetActive(false); //should already be false;
    }

    // Update is called once per frame
    void Update() {
        TriggerEvents();
        if (newTurn && !gameEnded && !eventTriggered && turnTeam.Count == 0 && !DialogueManager.InDialogueMode()) {
            InitTeamTurnQueue();
        }
    }

    //necessary when changing scene
    public static void Reset() {
        units = new Dictionary<string, List<TacticsMove>>();
        turnKey = new Queue<string>();
        turnTeam = new Queue<TacticsMove>();
        gameEnded = false;
    }

    static void InitTeamTurnQueue() {
        turnManager.newTurn = false;
        List<TacticsMove> teamList = units[turnKey.Peek()];

        foreach (TacticsMove unit in teamList) {
            turnTeam.Enqueue(unit);
        }

        turnManager.StartCoroutine(turnManager.ChangeTeamTurn(turnKey.Peek()));
        turnManager.Invoke("StartTurn", turnManager.changingDuration);
    }

    //coroutine for changing turn animation
    public IEnumerator ChangeTeamTurn(string team) {
        yield return new WaitForSeconds(0.5f);
        if (team == "Player") {
            panelText.text = "PLAYER PHASE";
            turnPanel.GetComponent<Image>().color = new Color32(0x00, 0x30, 0xEA, 0x55);
        }
        else if (team == "NPC") {
            panelText.text = "ENEMY PHASE";
            turnPanel.GetComponent<Image>().color = new Color32(0xF1, 0x00, 0x00, 0x55);
        }

        turnPanel.SetActive(true);
        TacticsMove.changingTurn = true;

        yield return new WaitForSeconds(changingDuration);
        turnPanel.SetActive(false);
        TacticsMove.changingTurn = false;
    }

    //new turn for a unit (called in "Invoke" function)
    public void StartTurn() {
        if (turnTeam.Count > 0) {
            TacticsMove.changingTurn = false;
            turnTeam.Peek().BeginTurn();
        }
    }

    public static void EndTurn() {
        TacticsMove unit = turnTeam.Dequeue();
        unit.ResetAllTiles();
        unit.EndTurn();

        TacticsMove.changingTurn = true;

        if (!gameEnded) {
            if (turnTeam.Count > 0) {
                //yield return new WaitForSeconds(0.2f);
                turnManager.Invoke("StartTurn", 0.5f);
            }
            else { // changing team turn
                ++turnNumber;
                Debug.Log("turn n°" + turnNumber);

                string team = turnKey.Dequeue();
                turnKey.Enqueue(team);
                if (turnKey.Peek() == "Player") {
                    UIManager.ResetReachableByEnemyTiles(true);
                }

                if (!gameEnded && !turnManager.eventTriggered && turnTeam.Count == 0 && !DialogueManager.InDialogueMode()) {
                    //InitTeamTurnQueue();
                    turnManager.newTurn = true;
                }
            }
        }
    }

    //exchange turns between units of the same team if unitToPlay still playable
    public static bool ExchangeTurn(TacticsMove playedUnit, TacticsMove unitToPlay) {
        if (turnTeam.Contains(unitToPlay)) {
            
            //if exchange : need to be first in queue (or change data structure/ use bools)
            TacticsMove[] tmpArray = turnTeam.ToArray();
            turnTeam.Clear();

            turnTeam.Enqueue(unitToPlay);
            for (int i=0; i<tmpArray.Length; ++i) {
                if(tmpArray[i] != unitToPlay) {
                    turnTeam.Enqueue(tmpArray[i]);
                }
            }
            playedUnit.turn = false;
            unitToPlay.BeginTurn();
            return true;
        }
        return false;
    }

    //exchange turns between a unit and its relative prev/next one
    public static void ExchangeTurn(TacticsMove playedUnit, bool isNext) {
        if (turnTeam.Count > 1) {
            string team = turnKey.Peek();
            int unitId = units[team].IndexOf(playedUnit);
            int unitToPlayId = -1;

            do {
                if (isNext) {
                    unitToPlayId = (unitId + 1) % units[team].Count;
                }
                else { //isPrevious
                    if (unitId == 0) {
                        unitToPlayId = units[team].Count - 1;
                    }
                    else {
                        unitToPlayId = unitId - 1;
                    }
                }
                unitId = unitToPlayId;

            } while (!ExchangeTurn(playedUnit, units[team][unitToPlayId]));
        }
    }

    public static void AddUnit(TacticsMove unit) {
        List<TacticsMove> list;

        if (!units.ContainsKey(unit.tag)) {
            list = new List<TacticsMove>();
            units[unit.tag] = list;

            if (!turnKey.Contains(unit.tag)) {
                turnKey.Enqueue(unit.tag);
            }
        }
        else {
            list = units[unit.tag];
        }

        list.Add(unit);

    }

    //ends the game if everyone from a team died
    public static void RemoveUnit(TacticsMove deadUnit) {
        units[deadUnit.tag].Remove(deadUnit);
        if (units[deadUnit.tag].Count == 0) {
            gameEnded = true;
            turnManager.StartCoroutine(UIManager.ShowEndLevelMenu(deadUnit.tag, 2.5f));
        }
    }

    public static bool GameEnded() {
        return gameEnded;
    }

    public static void SetTriggerToFalse() {
        turnManager.eventTriggered = false;
    }

    //basic function to trigger events while stopping the game : to change if more events are to happen
    void TriggerEvents() {
        if (!eventTriggered) { //otherwise : infinite calls during turn

            //trigger in level3 turn 5
            if (!triggers[0] && SceneManager.GetActiveScene().name == "Level3" && turnNumber == 4) {
                eventTriggered = true;
                triggers[0] = true; //==> this event won't ever be triggered
                GameObject hiddenNPCs = hiddenObjects[0];

                if (hiddenNPCs != null) {
                    StartCoroutine(AudioManager.TriggerClipChange(0));
                    hiddenNPCs.gameObject.SetActive(true);
                    UIManager.UpdateEnemies();
                    CameraMovement.UpdateUnits();
                    StartCoroutine(CameraMovement.FollowObjectFor(hiddenNPCs, 2f));
                    StartCoroutine(TriggerDialogue(1.5f));
                }
                else {
                    Debug.LogError("No hidden Objects found");
                }
            }
        }
    }

    IEnumerator TriggerDialogue(float delay) {
        yield return new WaitForSeconds(delay);
        DialogueManager.TriggerDialogue();
    }

}
