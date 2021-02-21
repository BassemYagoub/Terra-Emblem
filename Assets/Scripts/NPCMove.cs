using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMove : TacticsMove {
    GameObject target;

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
            FindNearestTarget();
            CalculatePath();
            //FindSelectableTiles();
            actualTargetTile.target = true;
        }
        else if (!moving && actionPhase) {
            FindAttackableTiles();
            tacticsMoveUnit.attackOpponent(target.gameObject.GetComponent<Unit>());
        }
        else {
            Move();
        }
    }

    void CalculatePath() {
        Tile targetTile = GetTargetTile(target);
        FindPath(targetTile);
    }

    void FindNearestTarget() {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Player");

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
