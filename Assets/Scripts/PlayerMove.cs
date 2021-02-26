using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : TacticsMove {
    private bool movingAttacking = false; //moving then attacking with 1 click
    private bool foundTiles = false; //to not call FindSelectableTiles every frame
    private Unit opponentUnit = null;
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
                tacticsMoveUnit.InflictDamage(opponentUnit);
                opponentUnit = null;
                movingAttacking = false; 
                targetTile = null;
                foundTiles = false;
            }
        }
        else if (!moving && !actionPhase) {
            if (!foundTiles) {
                FindSelectableTiles();
                foundTiles = true;
            }
            checkMouse();
        }
        else if (!moving && actionPhase) {
            if (!foundTiles) {
                FindAttackableTiles();
                foundTiles = true;
            }
            checkMouse();
        }
        else {
            Move();
        }
    }

    public void ResetFoundTiles() {
        foundTiles = false;
    }

    public void AttackOpponent(Unit opponent) {
        tacticsMoveUnit.attackOpponent(opponent);
        foundTiles = false;
    }

    //moving and attacking in one click
    public void FindPathThenAttack(Unit opponent) {
        targetTile = GetTargetTile(opponent.gameObject);
        opponentUnit = opponent;

        if (targetTile.attackable) {
            if (attackRange == 1) { // + move only if dist > 1 ??
                FindPath(targetTile);
            }
            else {
                int range = 1;
                int dist = 1000;
                Tile nearTargetTile = targetTile;

                while (range < attackRange) {
                    //get an optimal tile
                    foreach (Tile adjTile in nearTargetTile.adjacencyList) {

                        if (dist > adjTile.distance) {
                            nearTargetTile = adjTile;
                            dist = adjTile.distance;
                        }
                    }
                    ++range;
                }
                FindPath(nearTargetTile);
            }
            movingAttacking = true;
            actualTargetTile.target = true;
        }
    }

    public override void PassTurn() {
        RemoveSelectedTiles();
        RemoveAttackableTiles();
        actionPhase = false;
        foundTiles = false;
        StartCoroutine(TurnManager.EndTurn());
    }

    void checkMouse() {
        if (Input.GetMouseButtonUp(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {

                if (hit.collider.tag == "Player") {
                    if(gameObject == hit.collider.gameObject) {
                        UIManager.ShowPlayerActions();
                    }
                }

                else if(hit.collider.tag == "NPC") {
                    Unit opponent = hit.collider.GetComponent<Unit>();
                    UIManager.ShowOnEnemyActions(opponent.GetComponent<TacticsMove>());
                }

                //moving
                else if (!actionPhase && hit.collider.tag == "Tile") {
                    Tile t = hit.collider.GetComponent<Tile>();
                    if (t.selectable) {
                        MoveToTile(t);
                        foundTiles = false;
                    }
                }

            }
        }

        //pass acionPhase with right click
        else if (actionPhase && Input.GetKeyUp(KeyCode.Mouse1)) {
            PassTurn();
        }
    }


}
