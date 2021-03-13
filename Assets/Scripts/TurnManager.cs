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
    public GameObject turnPanel; //panel
    private Text panelText; //text inside panel indicating the team
    float changingDuration = 1.5f;
    static bool gameEnded = false;

    // Start is called before the first frame update
    void Start() {
        turnManager = this;
        panelText = turnPanel.GetComponentInChildren<Text>();
        turnPanel.SetActive(false); //should already be false;
    }

    // Update is called once per frame
    void Update() {
        if (!gameEnded && turnTeam.Count == 0 && units.Count > 0 && turnKey.Count > 0) {
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

    //new turn for a unit
    public void StartTurn() {
        if (turnTeam.Count > 0) {
            TacticsMove.changingTurn = false;
            turnTeam.Peek().BeginTurn();
            if(turnTeam.Peek().tag == "Player") {
                UIManager.ShowPlayerActions();
            }
        }
    }

    public static void EndTurn() {
        TacticsMove unit = turnTeam.Dequeue();
        unit.EndTurn();

        TacticsMove.changingTurn = true;
        UIManager.HidePanelsBetweenTurns();

        if (!gameEnded) {
            if (turnTeam.Count > 0) {
                //yield return new WaitForSeconds(0.2f);
                turnManager.Invoke("StartTurn", 0.5f);
            }
            else { // changing team turn
                string team = turnKey.Dequeue();
                turnKey.Enqueue(team);
                InitTeamTurnQueue();
                if (turnKey.Peek() == "Player") {
                    UIManager.ResetReachableByEnemyTiles(true);
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

}
