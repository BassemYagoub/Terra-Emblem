using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : TacticsMove {
    private Unit tacticsMoveUnit;

    // Start is called before the first frame update
    void Start() {
        tacticsMoveUnit = gameObject.GetComponent<Unit>();
        Init();
    }

    // Update is called once per frame
    void Update() {

        Debug.DrawRay(transform.position, transform.forward);

        if (!turn || changingTurn) {
            return;
        }

        if (!moving && !actionPhase) {
            FindSelectableTiles();
            checkMouse();
        }
        else if (!moving && actionPhase) {
            FindAttackableTiles();
            checkMouse();
        }
        else {
            Move();
        }
    }

    void checkMouse() {
        if (Input.GetMouseButtonUp(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {
                //moving
                if (!actionPhase && hit.collider.tag == "Tile") {
                    Tile t = hit.collider.GetComponent<Tile>();
                    if (t.selectable) {
                        MoveToTile(t);
                    }
                }
                //attacking
                else if (actionPhase) {
                    if (hit.collider.tag == "NPC") {
                        Unit touchedUnit = hit.collider.GetComponent<Unit>();//.GetComponent<TacticsMove>();
                        Debug.Log("NPC at " + touchedUnit.transform.position);
                        tacticsMoveUnit.attackOpponent(/*null, */touchedUnit);

                    }
                    else {
                        Debug.Log("touched nothing");
                    }
                }
            }
        }
        else if (actionPhase && Input.GetKeyUp(KeyCode.Mouse1)) {
            Debug.Log("pass action phase");
            RemoveAttackableTiles();
            actionPhase = false;
            TurnManager.EndTurn();
        }
    }


}
