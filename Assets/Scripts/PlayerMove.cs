using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : TacticsMove {
    private bool movingAttacking = false; //moving then attacking with 1 click
    public Unit opponentUnit = null;
    private Tile targetTile = null;

    // Start is called before the first frame update
    void Start() {
        Init();
    }

    // Update is called once per frame
    void Update() {

        Debug.DrawRay(transform.position, transform.forward);

        if (!turn || changingTurn) {
            //Debug.Log(name);
            return;
        }

        else if (movingAttacking) {
            Move();
            if (!moving) { //attack only if done moving
                Debug.Log("moveAttack "+targetTile.name);
                //actionPhase = true;
                targetTile.attackable = true;
                tacticsMoveUnit.InflictDamage(opponentUnit);
                opponentUnit = null;
                movingAttacking = false; 
                targetTile = null;
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

                        //A* Ver.
                        /*targetTile = t;
                        actualTargetTile = targetTile;
                        FindPath(targetTile);
                        actualTargetTile.target = true;*/
                    }
                }

                //moving and attacking in one click
                else if(!actionPhase && hit.collider.tag == "NPC") {
                    Unit opponent = hit.collider.GetComponent<Unit>();

                    targetTile = GetTargetTile(opponent.gameObject);
                    opponentUnit = opponent;

                    if (targetTile.attackable) {
                        //MoveToTile(targetTile);
                        if(attackRange == 1) { // + move only if dist > 1 ??
                            Debug.Log("TargetTile attackable");
                            FindPath(targetTile);
                        }
                        else {
                            Debug.Log("TargetTile range attackable");
                            int range = 1;
                            int dist = 1000;
                            Tile nearTargetTile = targetTile;

                            while(range < attackRange) {
                                //get an optimal tile
                                foreach(Tile adjTile in nearTargetTile.adjacencyList) {

                                    if (dist > adjTile.distance) {
                                        nearTargetTile = adjTile;
                                        //Debug.Log(nearTargetTile + " chosen");
                                        dist = adjTile.distance;
                                    }
                                }
                                ++range;
                                //Debug.Log("range=" + range);
                            }
                            FindPath(nearTargetTile);
                        }
                        movingAttacking = true;
                        actualTargetTile.target = true;
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

        //pass acionPhase with right click
        else if (actionPhase && Input.GetKeyUp(KeyCode.Mouse1)) {
            RemoveAttackableTiles();
            actionPhase = false;
            TurnManager.EndTurn();
        }
    }


}
