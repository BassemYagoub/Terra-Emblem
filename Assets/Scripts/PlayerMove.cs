using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : TacticsMove {
    private bool movingAttacking = false; //moving then attacking with 1 click
    private Unit opponentUnit = null;
    private Tile targetTile = null;

    // Start is called before the first frame update
    void Start() {
        Init();
    }

    // Update is called once per frame
    void Update() {

        Debug.DrawRay(transform.position, transform.forward);

        if (!UIManager.MenuIsOn()) {

            //not unit's turn to play
            if (!turn || changingTurn) {
                animator.SetBool("isRunning", false);
                animator.SetBool("isWalking", false);
                //Debug.Log(name);
                return;
            }

            //unit moves then attacks
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

            //unit's turn but didn't make a move yet
            else if (!moving && !actionPhase) {
                if (!foundTiles) {
                    FindSelectableTiles();
                    foundTiles = true;
                }
                CheckMouse();
            }

            //unit already moved but can still attack
            else if (!moving && actionPhase) {
                if (!foundTiles) {
                    FindAttackableTiles();
                    foundTiles = true;
                }
                CheckMouse();
            }

            //Player clicked on a tile
            else {
                Move();
            }
        }
    }

    public void ResetFoundTiles() {
        foundTiles = false;
    }

    public void AttackOpponent(Unit opponent) {
        tacticsMoveUnit.AttackOpponent(opponent);
        foundTiles = false;
    }

    /// <summary>
    /// Makes unit move and attack in one click (if attack = true)
    /// </summary>
    /// <param name="opponent">target to attack</param>
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

                //find a path from target tile to current tile
                while (range < attackRange) {
                    //get an optimal tile (attackRange dist from enemy)
                    foreach (Tile adjTile in nearTargetTile.adjacencyList) {

                        if (dist > adjTile.distance && adjTile.distance > 0 && adjTile.distance <= movingRange) {
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
        actionPhase = false;
        foundTiles = false;
        TurnManager.EndTurn();
    }

    void CheckMouse() {
        if (Input.GetMouseButtonUp(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {

                if (hit.collider.tag == "Player") {

                    //if other unit player can play : change turn
                    if (!(gameObject == hit.collider.gameObject)) {
                        TacticsMove unitToPlay = hit.collider.GetComponent<TacticsMove>();
                        TurnManager.ExchangeTurn(gameObject.GetComponent<TacticsMove>(), unitToPlay);
                        
                        if (turn) { // <=> unitToPlay already played
                            Debug.Log("turn alreay ended");
                            UIManager.ChangeSelectedUnit(unitToPlay);
                            UIManager.ShowUnitInfoPanel(false); //false => show unit turn ended in UI
                            return;
                        }
                    } //if self or other unit going to play
                    UIManager.ShowPlayerActions();
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

        //pass turn with right click
        else if (Input.GetKeyUp(KeyCode.Mouse1)) {
            PassTurn();
        }
    }


}
