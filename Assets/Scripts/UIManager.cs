using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour {
    public  GameObject turnPanel;
    float changingDuration = 1.0f;

    void Start() {
        turnPanel.SetActive(false);
    }

    void Update() { 

    }

    public void TurnHandler() {
        StartCoroutine(ChangeTeamTurn());
    }

    public IEnumerator ChangeTeamTurn() {
        turnPanel.SetActive(true);
        yield return new WaitForSeconds(changingDuration);
        turnPanel.SetActive(false);
    }
}
