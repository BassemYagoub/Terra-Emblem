using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : TacticsMove {
    private Unit tacticsMoveUnit;
    private bool movingAttacking = false; //moving then attacking with 1 click
    private Unit opponentUnit = null;

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

        if (movingAttacking) {
            Move();
            if (!moving) { //attack only if done moving
                Debug.Log("moveAttack");
                //actionPhase = true;
                tacticsMoveUnit.attack(opponentUnit);
                opponentUnit = null;
                movingAttacking = false;
            }
        }
        else if (!moving && !actionPhase) {
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

                //moving and attacking in one click
                else if(!actionPhase && hit.collider.tag == "NPC") {
                    Unit opponent = hit.collider.GetComponent<Unit>();

                    RaycastHit hitTileUnderneath;

                    //get tile underneath opponent
                    if (Physics.Raycast(opponent.transform.position, Vector3.down, out hitTileUnderneath, 1)) {
                        Tile tileOpponent = hitTileUnderneath.transform.GetComponent<Tile>();

                        //accessible opponent (d>0 means reachable)
                        if (tileOpponent.attackable &&  tileOpponent.distance > 0) {
                            Debug.Log(tileOpponent.distance + " " + movingPoints +" "+attackRange);

                            //choose wich tile player is going to go to
                            foreach (Tile t in tileOpponent.adjacencyList) {
                                if (t.selectable) {
                                    MoveToTile(t);
                                    break;
                                }
                            }

                            movingAttacking = true;
                            opponentUnit = opponent;
                        }
                    }
                }

                //attacking after moving
                else if (actionPhase) {
                    if (hit.collider.tag == "NPC") {
                        Unit touchedUnit = hit.collider.GetComponent<Unit>();
                        Debug.Log("NPC at " + touchedUnit.transform.position);
                        tacticsMoveUnit.attackOpponent(touchedUnit);

                    }
                    else {
                        Debug.Log("touched nothing");
                    }
                }
            }
        }

        //pass acionPhase
        else if (actionPhase && Input.GetKeyUp(KeyCode.Mouse1)) {
            RemoveAttackableTiles();
            actionPhase = false;
            TurnManager.EndTurn();
        }
    }


}
