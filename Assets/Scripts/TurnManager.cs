using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class TurnManager : MonoBehaviour {

    //teamTag : unitsFromTeam
    static Dictionary<string, List<TacticsMove>> units = new Dictionary<string, List<TacticsMove>>();
    static Queue<string> turnKey = new Queue<string>(); //whose turn
    static Queue<TacticsMove> turnTeam = new Queue<TacticsMove>(); //turn for each unit of playing team
    static TurnManager uiManager;
    public GameObject turnPanel; //panel
    private Text panelText; //text inside panel indicating the team
    float changingDuration = 1.5f;
    static bool gameEnded = false;

    // Start is called before the first frame update
    void Start() {
        uiManager = this;
        panelText = turnPanel.GetComponentInChildren<Text>();
        turnPanel.SetActive(false); //should already be false;
    }

    // Update is called once per frame
    void Update() {
        if (!gameEnded && turnTeam.Count == 0) {
            InitTeamTurnQueue();
        }

    }

    static void InitTeamTurnQueue() {
        List<TacticsMove> teamList = units[turnKey.Peek()];

        foreach (TacticsMove unit in teamList) {
            turnTeam.Enqueue(unit);
        }

        uiManager.StartCoroutine(uiManager.ChangeTeamTurn(turnKey.Peek()));

        StartTurn();
    }

    //coroutine for changing turn animation
    public IEnumerator ChangeTeamTurn(string team) {
        if (team == "Player") {
            panelText.text = "PLAYER PHASE";
            turnPanel.GetComponent<Image>().color = new Color32(0x00, 0x30, 0xEA, 0x55);
        }
        else if (team == "NPC") {
            panelText.text = "ENEMY PHASE";
            turnPanel.GetComponent<Image>().color = new Color32(0xF1, 0x00, 0x00, 0x55);
        }
        turnPanel.SetActive(true);
        yield return new WaitForSeconds(changingDuration);
        turnPanel.SetActive(false);
    }

    public IEnumerator WaitFor(float sec) {
        yield return new WaitForSeconds(sec);
    }

    //new turn for a unit
    public static void StartTurn() {
        if (turnTeam.Count > 0) {
            TacticsMove.changingTurn = false;
            turnTeam.Peek().BeginTurn();
        }
    }

    public static IEnumerator EndTurn() {
        TacticsMove unit = turnTeam.Dequeue();
        unit.EndTurn();

        TacticsMove.changingTurn = true;

        if (!gameEnded) {
            if (turnTeam.Count > 0) {
                yield return new WaitForSeconds(0.2f);
                StartTurn();
            }
            else { // changing team turn
                string team = turnKey.Dequeue();
                turnKey.Enqueue(team);
                InitTeamTurnQueue();
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

            unitToPlay.BeginTurn();
            return true;
        }
        return false;
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
            EndGame(deadUnit.tag);
        }
    }

    public static void EndGame(string losingTeam) {
        if (losingTeam == "Player") {
            uiManager.panelText.text = "YOU LOST";
            uiManager.turnPanel.GetComponent<Image>().color = new Color32(0xF1, 0x00, 0x00, 0x55);
        }
        else if (losingTeam == "NPC") {
            uiManager.panelText.text = "LEVEL COMPLETE";
            uiManager.turnPanel.GetComponent<Image>().color = new Color32(0x00, 0x30, 0xEA, 0x55);
        }
        uiManager.turnPanel.SetActive(true);
    }

}
