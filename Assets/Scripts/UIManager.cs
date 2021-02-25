using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour {
    public Texture2D cursorTexture;
    //string currentCursor = "arrow";
    static UIManager manager; //to have only 1 cursorTexture

    static PlayerMove currentUnit;
    static TacticsMove selectedUnit;
    static GameObject actionPanel;

    void Start() {
        manager = this;
        actionPanel = GameObject.Find("ActionPanel");
        //Debug.Log(actionPanel.name);
        actionPanel.SetActive(false);
        actionPanel.transform.Find("WaitButton").gameObject.SetActive(false);
    }

    void Update() { 
        if(currentUnit != null) {

        }
    }

    public static void ChangeCursor(string cursorName) {
        if(cursorName == "hand"/* && manager.currentCursor == "arrow"*/)
            Cursor.SetCursor(manager.cursorTexture, new Vector2(10, 0), CursorMode.ForceSoftware);
        else// if(manager.currentCursor != "arrow")
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public static void ChangePlayer(PlayerMove p) {
        currentUnit = p;
        currentUnit.ResetFoundTiles();
    }

    public static void ShowPlayerActions() {
        actionPanel.SetActive(true);
        actionPanel.transform.Find("AttackButton").gameObject.SetActive(false);
        actionPanel.transform.Find("WaitButton").gameObject.SetActive(true);
    }

    public static void ShowOnEnemyActions(TacticsMove enemy) {
        selectedUnit = enemy;
        actionPanel.SetActive(true);
        actionPanel.transform.Find("AttackButton").gameObject.SetActive(true);
        actionPanel.transform.Find("WaitButton").gameObject.SetActive(false);
    }

    public void WaitNextTurn() {
        Debug.Log(currentUnit.name+" wait");
        currentUnit.PassTurn();
    }

    public void AttackEnemy() {
        currentUnit.FindPathThenAttack(selectedUnit.GetComponent<Unit>());
    }

    public void CancelAction() {
        Debug.Log("cancel action");
        actionPanel.SetActive(false);
    }

    public static void Reset() {
        actionPanel.SetActive(false);
    }
}
