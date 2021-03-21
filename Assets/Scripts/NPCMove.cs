using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMove : TacticsMove {
    GameObject target;
    List<GameObject> targets = new List<GameObject>(); //used if no path to nearest target

    // Start is called before the first frame update
    void Start() {
        Init();
    }

    // Update is called once per frame
    void Update() {

        Debug.DrawRay(transform.position, transform.forward);

        if (!UIManager.MenuIsOn()) {
            if (!turn || changingTurn) {
                return;
            }

            if (!moving && !actionPhase) {
                UpdateTargets();
                FindNearestTarget();
                CalculatePath();
                //FindSelectableTiles();
                actualTargetTile.target = true;
            }
            else if (!moving && actionPhase) {
                FindAttackableTiles();
                tacticsMoveUnit.AttackOpponent(target.gameObject.GetComponent<Unit>());
                foundTiles = false;
            }
            else {
                Move();
                if (!moving) {
                    foundTiles = false;
                }
            }
        }
    }

    void CalculatePath() {
        Tile targetTile = GetTargetTile(target);
        FindPath(targetTile);

        //if no path found actualtargetTile == null => find other target
        while(actualTargetTile == null && targets.Count > 0) {
            targets.Remove(target);
            FindNearestTarget();
            targetTile = GetTargetTile(target);
            FindPath(targetTile);
        }

        //impossible to reach anyone => don't move (really unlikely)
        if (targets.Count == 0) {
            actualTargetTile = currentTile;
            target = null;
            MoveToTile(actualTargetTile);
        }
    }

    void UpdateTargets() {
        targets = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
    }

    void FindNearestTarget() {

        GameObject nearest = null;
        float distance = Mathf.Infinity;

        foreach (GameObject obj in targets) {
            float d = Vector3.Distance(transform.position, obj.transform.position);

            if (d < distance) {
                distance = d;
                nearest = obj;
            }
        }

        target = nearest;
    }


}
