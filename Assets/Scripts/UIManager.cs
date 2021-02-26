using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour {
    public Texture2D[] cursorTextures;
    static string currentCursor = "arrow";
    static UIManager manager; //to have only 1 cursorTexture

    static PlayerMove currentUnit;
    static TacticsMove selectedUnit;
    static GameObject actionPanel;
    static GameObject unitInfoPanel;

    void Start() {
        manager = this;
        actionPanel = GameObject.Find("ActionPanel"); 
        unitInfoPanel = GameObject.Find("UnitInfoPanel");
        //Debug.Log(actionPanel.name);
        actionPanel.SetActive(false);
        unitInfoPanel.SetActive(false);
    }

    void Update() { 
    }

    //used by other classes
    public static void ChangeCursor(string cursorName) {
        if (cursorName != currentCursor) {
            currentCursor = cursorName;

            if (cursorName == "hand"/* && manager.currentCursor == "arrow"*/)
                Cursor.SetCursor(manager.cursorTextures[0], new Vector2(10, 0), CursorMode.ForceSoftware);
            else if (cursorName == "arrow")
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            else if (cursorName == "sword")
                Cursor.SetCursor(manager.cursorTextures[1], Vector2.zero, CursorMode.Auto);
            else if (cursorName == "feet")
                Cursor.SetCursor(manager.cursorTextures[2], new Vector2(15, 10), CursorMode.Auto);
        }
    }

    //used by Unity buttons
    public void ChangeCursorToHand() {
        Cursor.SetCursor(manager.cursorTextures[0], new Vector2(10, 0), CursorMode.ForceSoftware);
    }
    public void ChangeCursorToArrow() {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public static void ChangePlayer(PlayerMove p) {
        currentUnit = p;
        currentUnit.ResetFoundTiles();
    }
    public static void ChangeSelectedUnit(TacticsMove unit) {
        selectedUnit = unit;
    }

    public static void ShowUnitInfoPanel() {

        if (selectedUnit.tag == "NPC") {
            unitInfoPanel.transform.Find("UnitNamePanel").GetComponent<Image>().color = new Color32(0x9A, 0x00, 0x0A, 0xFF);
        }
        else {
            unitInfoPanel.transform.Find("UnitNamePanel").GetComponent<Image>().color = new Color32(0x00, 0x2E, 0x9A, 0xFF);
        }

        unitInfoPanel.transform.Find("UnitNamePanel").transform.Find("UnitName").GetComponent<Text>().text = selectedUnit.name;
        unitInfoPanel.transform.Find("UnitHPInfo").GetComponent<Text>().text = selectedUnit.getHP();
        unitInfoPanel.transform.Find("UnitLvlInfo").GetComponent<Text>().text = selectedUnit.getLvl();

        unitInfoPanel.SetActive(true); //if wanting to show unitInfoPanel but not actionPanel
    }

    public static void ShowPlayerActions() {
        //if(selectedUnit == null) { //to change
            selectedUnit = currentUnit;
        //}
        manager.HidePanels();
        manager.ShowPanels();
        actionPanel.transform.Find("AttackButton").gameObject.SetActive(false);
        actionPanel.transform.Find("WaitButton").gameObject.SetActive(true);
    }

    public static void ShowOnEnemyActions(TacticsMove enemy) {
        selectedUnit = enemy;
        manager.HidePanels();
        manager.ShowPanels();
        actionPanel.transform.Find("AttackButton").gameObject.SetActive(true);
        actionPanel.transform.Find("WaitButton").gameObject.SetActive(false);
    }

    public void WaitNextTurn() {
        Debug.Log(currentUnit.name+" wait");
        HidePanels();
        currentUnit.PassTurn();
    }

    public void AttackEnemy() {
        HidePanels();
        if (!currentUnit.actionPhase) {
            currentUnit.FindPathThenAttack(selectedUnit.GetComponent<Unit>());
        }
        else {
            currentUnit.AttackOpponent(selectedUnit.GetComponent<Unit>());
            currentUnit.ResetFoundTiles();
        }

    }

    public void CancelAction() {
        Debug.Log("cancel action");
        HidePanels();
    }

    public void ShowPanels() {
        actionPanel.SetActive(true);
        unitInfoPanel.SetActive(true);
        ShowUnitInfoPanel();
    }

    public void HidePanels() {
        ChangeCursorToArrow();
        //actionPanel.GetComponent<Animator>().speed = -1;
        actionPanel.SetActive(false);
        unitInfoPanel.SetActive(false);
    }

    public static void Reset() {
        actionPanel.SetActive(false);
    }
}
