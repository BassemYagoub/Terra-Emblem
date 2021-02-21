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
        foreach (TacticsMove unit in turnTeam) {
            unit.changingTurn = true;
        }
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

        foreach (TacticsMove unit in turnTeam) {
            unit.changingTurn = false;
        }
    }

    //new turn for a unit
    public static void StartTurn() {
        if (turnTeam.Count > 0) {
            turnTeam.Peek().BeginTurn();
        }
    }

    public static void EndTurn() {
        TacticsMove unit = turnTeam.Dequeue();
        unit.EndTurn();

        if (!gameEnded) {
            if (turnTeam.Count > 0) {
                Debug.Log("no change");
                StartTurn();
            }
            else { // changing team turn
                Debug.Log("change");
                string team = turnKey.Dequeue();
                turnKey.Enqueue(team);
                InitTeamTurnQueue();
            }
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
