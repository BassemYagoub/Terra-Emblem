﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour {
    public Texture2D[] cursorTextures;
    bool menuOn = false;
    Vector3 cameraStoredPos;
    static string currentCursor = "arrow";
    static UIManager manager; //singleton

    static PlayerMove currentUnit;
    static TacticsMove selectedUnit;
    static GameObject actionPanel;
    static GameObject unitInfoPanel;
    static GameObject unitTurnPanel;
    public GameObject menuPanel;
    static GameObject enemiesRangeButton;
    static GameObject[] map;

    static bool canSeeEnemiesRange = false;
    static List<GameObject> enemies;

    void Start() {
        if (SceneManager.GetActiveScene().name.Contains("Level")) {
            manager = this;
            actionPanel = GameObject.Find("ActionPanel");
            unitInfoPanel = GameObject.Find("UnitInfoPanel");
            unitTurnPanel = GameObject.Find("UnitTurnPanel");
            enemiesRangeButton = GameObject.Find("EnemiesRangeButton");
            map = GameObject.FindGameObjectsWithTag("Tile");
            enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("NPC"));

            actionPanel.SetActive(false);
            unitInfoPanel.SetActive(false);
        }
        else if (SceneManager.GetActiveScene().name == "Credits") {
            StartCoroutine(ChargeTitleScreen()); // wait for end of credits
        }
    }

    void Update() {
        if (SceneManager.GetActiveScene().name.Contains("Level")) {
            if (Input.GetKeyDown(KeyCode.P)) {//for debug : pause game when needed
                Debug.Break();
            }

            else if (Input.GetKeyDown(KeyCode.E) && !menuOn) {
                ShowEnemiesRange();
            }

            else if (Input.GetKeyDown(KeyCode.Escape)) {
                ShowMenu();
            }
        }
    }

    public static bool MenuIsOn() {
        return manager.menuOn;
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

    public static void ChangeCurrentUnit(PlayerMove p) {
        currentUnit = p;
        //if (selectedUnit == null) {
            selectedUnit = currentUnit;
        //}

        unitTurnPanel.transform.Find("CurrentUnit").GetComponent<Text>().text = currentUnit.name;
        currentUnit.ResetFoundTiles();
    }

    public static void ChangeSelectedUnit(TacticsMove unit) {
        selectedUnit = unit;
    }

    public static void ShowUnitInfoPanel(bool canPlay = true) {
        unitInfoPanel.SetActive(false); //to trigger animation

        if (selectedUnit.tag == "NPC") {
            unitInfoPanel.transform.Find("UnitNamePanel").GetComponent<Image>().color = new Color32(0x9A, 0x00, 0x0A, 0xFF);
        }
        else {
            unitInfoPanel.transform.Find("UnitNamePanel").GetComponent<Image>().color = new Color32(0x00, 0x2E, 0x9A, 0xFF);
        }

        unitInfoPanel.transform.Find("UnitNamePanel").transform.Find("UnitName").GetComponent<Text>().text = selectedUnit.name;
        unitInfoPanel.transform.Find("UnitHPInfo").GetComponent<Text>().text = selectedUnit.GetHP();
        unitInfoPanel.transform.Find("UnitLvlInfo").GetComponent<Text>().text = selectedUnit.GetLvl();
        unitInfoPanel.transform.Find("UnitMoveRangeInfo").GetComponent<Text>().text = selectedUnit.GetMovingRange();
        unitInfoPanel.transform.Find("UnitAttackRangeInfo").GetComponent<Text>().text = selectedUnit.GetAttackRange();

        GameObject unitTurnInfo = unitInfoPanel.transform.Find("UnitNamePanel").transform.Find("UnitTurnInfo").gameObject;
        if (!canPlay) {
            unitTurnInfo.SetActive(true);
        }
        else {
            unitTurnInfo.SetActive(false);
        }

        unitInfoPanel.SetActive(true); //if wanting to show unitInfoPanel but not actionPanel
    }

    public static void ShowPlayerActions() {
        selectedUnit = currentUnit;

        if (!canSeeEnemiesRange) {
            ResetReachableByEnemyTiles();
        }
        //manager.HidePanels();
        manager.ShowPanels();
        actionPanel.transform.Find("AttackButton").gameObject.SetActive(false);
        actionPanel.transform.Find("WaitButton").gameObject.SetActive(true);
    }

    public static void ShowOnEnemyActions(TacticsMove enemy) {
        if(selectedUnit != enemy) { 
            selectedUnit = enemy;
            if (!canSeeEnemiesRange) {
                ResetReachableByEnemyTiles();
            }
            enemy.FindReachableByEnemyTiles(); 
        
            if (!currentUnit.actionPhase) {
                currentUnit.FindSelectableTiles();
            }
            else {
                currentUnit.FindAttackableTiles();
            }

            //reboot to show panelanimation
            //manager.HidePanels();
            manager.ShowPanels();

            RaycastHit hit;
            if (Physics.Raycast(enemy.transform.position, Vector3.down, out hit, 1)) {
                if (hit.transform.GetComponent<Tile>().attackable) {
                    actionPanel.transform.Find("AttackButton").gameObject.SetActive(true);
                }
                else {
                    actionPanel.transform.Find("AttackButton").gameObject.SetActive(false);
                }
            }
            actionPanel.transform.Find("WaitButton").gameObject.SetActive(false);
        }
        else { //clicking on previously clicked enemy cancels its panels
            manager.CancelAction();
            selectedUnit = currentUnit;
        }
    }


    public static void ResetPanel() {
        actionPanel.SetActive(false);
    }

    //resets the tile reachableByEnemy property when changing selection
    public static void ResetReachableByEnemyTiles(bool reload = false) {
        bool tmpCanSee = canSeeEnemiesRange;
        canSeeEnemiesRange = false;
        for (int i = 0; i < map.Length; ++i) {
            map[i].GetComponent<Tile>().reachableByEnemy = false;
        }

        if(tmpCanSee && reload) { //if player wants to see enemies' range, reload updated map
            manager.Invoke("ShowEnemiesRange", 1f);
        }
    }

    public static void RemoveEnemy(GameObject enemy) {
        enemies.Remove(enemy);
    }

    //to hide panels when changing turns
    public static void HidePanelsBetweenTurns() {
        manager.ChangeCursorToArrow();
        actionPanel.SetActive(false);
        unitInfoPanel.SetActive(false);
    }


    /*----------- NON STATIC METHODS -----------*/


    //used by Unity buttons
    public void ChangeCursorToHand() {
        Cursor.SetCursor(manager.cursorTextures[0], new Vector2(10, 0), CursorMode.ForceSoftware);
    }

    public void ChangeCursorToArrow() {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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
        if (!canSeeEnemiesRange) {
            ResetReachableByEnemyTiles();
        }
        HidePanels();
    }

    public void PreviousUnit() {
        TurnManager.ExchangeTurn(currentUnit, false);
    }

    public void NextUnit() {
        TurnManager.ExchangeTurn(currentUnit, true);
    }


    public void ShowPanels() {
        actionPanel.SetActive(true);
        unitInfoPanel.SetActive(true);
        ShowUnitInfoPanel();
    }

    public void HidePanels() {
        ChangeCursorToArrow();
        actionPanel.GetComponent<Animator>().SetTrigger("CancelPanel");
        StartCoroutine(DelayHiding(0.5f));
    }

    public IEnumerator DelayHiding(float duration) {
        yield return new WaitForSeconds(duration);
        actionPanel.SetActive(false);
        unitInfoPanel.SetActive(false);
    }

    //Enemy range button : shows/unshows the sum of enemies' range
    public void ShowEnemiesRange() {
        if (currentUnit != null) {
            Text buttonText = enemiesRangeButton.transform.Find("Text").GetComponent<Text>();
            if (!canSeeEnemiesRange) {
                foreach (GameObject enemy in enemies) {
                    enemy.GetComponent<TacticsMove>().FindReachableByEnemyTiles();
                }
                canSeeEnemiesRange = true;
                buttonText.text = "Enemy Range : ON (E)";
            }
            else {
                ResetReachableByEnemyTiles();
                buttonText.text = "Enemy Range : OFF (E)";
            }

            if (!currentUnit.actionPhase) {
                currentUnit.FindSelectableTiles();
            }
            else {
                currentUnit.FindAttackableTiles();
            }
        }
    }

    //show or unshow menu depending on wether its already active
    //should be improved to pause every gameObject in the game
    public void ShowMenu() {
        if (!TurnManager.GameEnded()) {
            if (!menuOn) {
                AudioManager.ReduceVolumeByHalf();
                menuOn = true;
                Text titleMenuText = manager.menuPanel.GetComponentInChildren<Text>();
                Text retryButtonText = manager.menuPanel.transform.Find("RetryButton").GetComponentInChildren<Text>();

                titleMenuText.text = "PAUSE";
                //endPanelText.color = new Color32(0x00, 0x30, 0xEA, 0x55);
                //retryButtonText.text = "Restart level";

                manager.menuPanel.SetActive(true);
                cameraStoredPos = CameraMovement.GetCameraPos();
                CameraMovement.MoveCameraAway();
            }
            else {
                AudioManager.RiseVolumeByHalf();
                menuOn = false;
                CameraMovement.MoveCameraTo(cameraStoredPos);
                manager.menuPanel.SetActive(false);
            }
        }

    }

    //when every units of a team died
    public static IEnumerator ShowEndLevelMenu(string losingTeam, float changingDuration) {
        AudioManager.ReduceVolumeByHalf();

        yield return new WaitForSeconds(changingDuration);
        manager.menuPanel.transform.Find("ExitButton").gameObject.SetActive(false);
        Text endPanelText = manager.menuPanel.GetComponentInChildren<Text>();
        Text endButtonText = manager.menuPanel.transform.Find("RetryButton").GetComponentInChildren<Text>();


        if (losingTeam == "Player") {
            endPanelText.text = "YOU LOST";
            endPanelText.color = new Color32(0xF1, 0x00, 0x00, 0x55);
        }
        else if (losingTeam == "NPC") {
            endPanelText.text = "MISSION COMPLETE";
            //endPanelText.color = new Color32(0x00, 0x30, 0xEA, 0x55);
            endButtonText.text = "Reload level";
            manager.menuPanel.transform.Find("NextLvlButton").gameObject.SetActive(true);
        }

        manager.menuPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        CameraMovement.MoveCameraAway();
    }


    //loading scenes
    public void ReloadLevel() {
        TurnManager.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ChargeNextScene() {
        string sceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(SceneTransition(sceneName));
    }
    public IEnumerator ChargeTitleScreen() {
        yield return new WaitForSeconds(8f);
        StartCoroutine(AudioManager.VolumeTransition(0f));
        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene(0);
    }

    IEnumerator SceneTransition(string sceneName) {
        Debug.Log("aaaaaaaa");
        StartCoroutine(AudioManager.VolumeTransition(0f));
        if (sceneName == "TitleScreen") {
            GameObject.Find("TitleScreenPanel").GetComponent<Animator>().SetTrigger("PlayGame");
            yield return new WaitForSeconds(5f);
        }
        else {
            if (menuPanel.activeSelf) {
                menuPanel.GetComponent<Animator>().SetTrigger("LevelTransition");
                yield return new WaitForSeconds(2f);
            }
            else { //possible in level 0 when exiting room without menu
                menuPanel.SetActive(true);
                menuPanel.GetComponent<Animator>().SetTrigger("LevelTransition0");
                yield return new WaitForSeconds(.45f);
            }
            TurnManager.Reset();
        }


        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    //text changes on menus
    public void PlayButtonGreyHover() {
        GameObject.Find("PlayButton").GetComponentInChildren<TextMeshProUGUI>().color = new Color32(0xAA, 0xAA, 0xAA, 0xFF);
    }
    public void PlayButtonWhiteHover() {
        GameObject.Find("PlayButton").GetComponentInChildren<TextMeshProUGUI>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    }

    public void MenuButtonToGrey(string buttonName) {
        manager.menuPanel.transform.Find(buttonName).GetComponentInChildren<Text>().color = Color.grey;
    }
    public void MenuButtonToWhite(string buttonName) {
        manager.menuPanel.transform.Find(buttonName).GetComponentInChildren<Text>().color = Color.white;
    }

}
