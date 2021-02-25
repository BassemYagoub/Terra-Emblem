using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMove : MonoBehaviour {
    protected Unit tacticsMoveUnit;

    //stats
    public int movingPoints = 5;
    public int attackRange = 1;
    public float jumpHeight = 1;
    public float moveSpeed = 6;
    public float jumpVelocity = 4.5f;

    //states
    public bool moving = false;
    public bool jumping = false;
    public bool falling = false;
    public bool movingEdge = false;
    public bool turn = false;
    public bool actionPhase = false;
    public static bool changingTurn = true; //cannot move if changing turn

    List<Tile> selectableTiles = new List<Tile>();
    List<Tile> attackableTiles = new List<Tile>();
    List<Tile> tilesWithEnemies = new List<Tile>();
    GameObject[] tiles;
    Stack<Tile> path = new Stack<Tile>();
    Tile currentTile;

    Vector3 velocity = new Vector3();
    Vector3 heading = new Vector3();
    Vector3 jumpTarget;
    float halfHeight = 0; //height of character (often used)

    protected Tile actualTargetTile;

    protected void Init() {
        tiles = GameObject.FindGameObjectsWithTag("Tile");
        halfHeight = GetComponent<Collider>().bounds.extents.y;
        tacticsMoveUnit = gameObject.GetComponent<Unit>();
        TurnManager.AddUnit(this);

    }

    public Tile GetTargetTile(GameObject target) {
        RaycastHit hit;
        Tile tile = null;
        if (Physics.Raycast(target.transform.position, Vector3.down, out hit, 1)) {
            tile = hit.collider.GetComponent<Tile>();
        }

        return tile;
    }

    public void ComputeAdjacencyLists(float jumpHeight, Tile target, string team, bool attackPhase=false) {
        //tiles = GameObject.FindGameObjectsWithTag("Tile"); //if changing map during game

        foreach (GameObject tile in tiles) {
            Tile t = tile.GetComponent<Tile>();
            t.FindNeighbors(jumpHeight, target, team, attackPhase);

            if (t.checkIfEnemyOnTop(gameObject.tag)) {
                tilesWithEnemies.Add(t);
            }
        }
    }

    public void GetCurrentTile() {
        currentTile = GetTargetTile(gameObject);
        currentTile.current = true;
    }

    //BFS
    public void FindSelectableTiles() {
        ComputeAdjacencyLists(jumpHeight, null, gameObject.tag);
        GetCurrentTile();

        Queue<Tile> process = new Queue<Tile>();
        Queue<Tile> processAttackable = new Queue<Tile>();

        process.Enqueue(currentTile);
        currentTile.visited = true;

        while(process.Count > 0) {
            Tile t = process.Dequeue();
            selectableTiles.Add(t);

            //if not an enemy on top of tile => selectable
            if (!t.attackable) {
                t.selectable = true;
            }

            if (t.distance < movingPoints) {
                foreach (Tile tile in t.adjacencyList) {
                    if (!tile.visited) {
                        tile.parent = t;
                        tile.visited = true;
                        tile.distance = t.distance + 1;
                        process.Enqueue(tile);
                    }
                }
            }
            //gather border of selectable tiles
            if (t.distance == movingPoints) {
                processAttackable.Enqueue(t);
            }
        }

        FindAttackableBorder(processAttackable);
        FindReachableEnemies(tilesWithEnemies);

    }

    //border of selectable tiles <=> attackable
    public void FindAttackableBorder(Queue<Tile> processAttackable) {
        while (processAttackable.Count > 0) {
            Tile t = processAttackable.Dequeue();
            attackableTiles.Add(t);

            if (t.distance < movingPoints + attackRange) {
                foreach (Tile tile in t.adjacencyList) {
                    if (!tile.visited) {
                        tile.parent = t;
                        tile.visited = true;
                        tile.distance = t.distance + 1;
                        tile.attackable = true;
                        processAttackable.Enqueue(tile);
                    }
                }
            }
        }
    }

    //checking if enemies on top of tiles are reachable
    public void FindReachableEnemies(List<Tile> tilesWithEnemies) {
        Queue<Tile> reprocess = new Queue<Tile>();

        foreach (Tile t in tilesWithEnemies) {
            //Debug.Log(t.name);
            bool isAttackable = false;


            foreach (Tile adjTile in t.adjacencyList) {

                //neighbour selectable <=> t attackable + not in any path
                if (adjTile.selectable) {
                    adjTile.adjacencyList.Remove(t);
                    isAttackable = true; //potential useless loops

                    if (adjTile.parent == t) {
                        if (adjTile.adjacencyList.Count > 0) {
                            adjTile.parent = adjTile.adjacencyList[0];
                            adjTile.distance = adjTile.parent.distance + 1;
                        }
                    }
                }

                //enemies in attack border
                else if (adjTile.attackable) {
                    Tile parentTile = adjTile.parent;
                    int enemyBorderDist = 1; //dist of enemy counting from the attack border

                    while(enemyBorderDist < attackRange) {
                        if (parentTile.selectable) {
                            adjTile.adjacencyList.Remove(t);
                            isAttackable = true;
                            break;
                        }
                        parentTile = parentTile.parent;
                        ++enemyBorderDist;
                    }

                }
            }

            if (isAttackable) {
                t.attackable = true;
            }
        }

        List<Tile> toRemoveFromAdj = new List<Tile>();
        foreach (Tile adjTile in currentTile.adjacencyList) {
            //Debug.Log("adj: " + adjTile.name);
            if (adjTile.enemyOnTop) {
                toRemoveFromAdj.Add(adjTile);
            }
        }
        foreach (Tile removable in toRemoveFromAdj) {
            currentTile.adjacencyList.Remove(removable);
        }

        tilesWithEnemies.Clear();

        //recalculate dists
        /*while (reprocess.Count > 0) {
            Tile t = reprocess.Dequeue();
            Debug.Log("reprocess: " + t.name);

            if (t.selectable) {
                foreach (Tile tile in t.adjacencyList) {
                    if (tile.parent == t) {
                        Debug.Log("dist: " + tile.distance);
                        tile.distance = t.distance + 1;
                        Debug.Log("new dist: " + tile.distance);
                        reprocess.Enqueue(tile);
                    }
                }
            }
            if(t.distance > movingPoints) {
                t.selectable = false;
                if(t.distance < movingPoints + attackRange) {
                    t.attackable = true;
                }
            }
        }*/

    }

    //attackable tiles in actionPhase
    public void FindAttackableTiles() {
        ComputeAdjacencyLists(jumpHeight, null, gameObject.tag, true);
        GetCurrentTile();
        Queue<Tile> process = new Queue<Tile>();
        process.Enqueue(currentTile);
        currentTile.visited = true;

        while (process.Count > 0) {
            Tile t = process.Dequeue();
            attackableTiles.Add(t);
            
            //player cannot attack its own pos
            if (t != currentTile) {
                t.attackable = true;
            }

            if (t.distance < attackRange) {
                foreach (Tile tile in t.adjacencyList) {
                    if (!tile.visited) {
                        tile.visited = true;
                        tile.distance = t.distance + 1; //dist = parent's dist+1
                        process.Enqueue(tile);
                    }
                }
            }
        }

    }

    public void MoveToTile(Tile tile) {
        path.Clear();
        tile.target = true;
        moving = true;

        Tile next = tile;
        while (next != null) {
            //Debug.Log("next=" + next);
            path.Push(next);
            next = next.parent;
        }
    }

    protected void RemoveSelectedTiles() {
        if(currentTile != null) {
            currentTile.current = false;
            currentTile = null;
        }

        foreach(Tile tile in selectableTiles) {
            tile.Reset();
        }
        selectableTiles.Clear();
    }

    public void RemoveAttackableTiles() {
        if (currentTile != null) {
            currentTile.current = false;
            currentTile = null;
        }

        foreach (Tile tile in attackableTiles) {
            tile.Reset();
        }
        attackableTiles.Clear();
    }

    void CalculateHeading(Vector3 target) {
        heading = target - transform.position;
        heading.Normalize();
    }

    void SetHorizontalVelocity() {
        velocity = heading * moveSpeed;
    }

    //Jumping functions
    void PrepareJump(Vector3 target) {
        float targetY = target.y;
        target.y = transform.position.y;
        CalculateHeading(target);

        //jump downward
        if(transform.position.y > targetY) {
            falling = false;
            jumping = false;
            movingEdge = true;

            jumpTarget = transform.position + (target - transform.position) / 2.0f;
        }
        else { //jump upward
            falling = false;
            jumping = true;
            movingEdge = false;

            velocity = heading * moveSpeed / 3.0f;
            float diffY = targetY - transform.position.y;
            velocity.y = jumpVelocity * (0.5f + diffY / 2.0f);
        }
    }

    void FallDownward(Vector3 target) {
        velocity += Physics.gravity * Time.deltaTime;

        if (transform.position.y <= target.y) {
            falling = false;
            jumping = false;
            movingEdge = false;

            Vector3 p = transform.position;
            p.y = target.y;
            transform.position = p;

            velocity = new Vector3();
        }
    }

    void JumpUpward(Vector3 target) {
        velocity += Physics.gravity * Time.deltaTime;

        if(transform.position.y > target.y) {
            jumping = false;
            falling = true;
        }
    }

    void MoveToEdge() {
        if(Vector3.Distance(transform.position, jumpTarget) >= 0.05f) {
            SetHorizontalVelocity();
        }
        else {
            movingEdge = false;
            falling = true;

            velocity /= 5.0f;
            velocity.y = 1.5f;
        }
    }

    void Jump(Vector3 target) {
        if (falling) {
            FallDownward(target);
        }
        else if (jumping) {
            JumpUpward(target);
        }
        else if (movingEdge) {
            MoveToEdge();
        }
        else {
            PrepareJump(target);
        }
    }


    public void Move() {
        if(path.Count > 0) {
            Tile t = path.Peek();
            Vector3 target = t.transform.position;

            //calculate the unit's pos on top of the target tile
            target.y += halfHeight + t.GetComponent<Collider>().bounds.extents.y;
            if(Vector3.Distance(transform.position, target) >= 0.05f) {

                bool jump = (transform.position.y != target.y);

                if (jump) {
                    Jump(target);
                }
                else {
                    CalculateHeading(target);
                    SetHorizontalVelocity();
                }

                //Locomotion
                transform.forward = heading;
                transform.position += velocity * Time.deltaTime;
            }
            else {
                //Tile center reached
                transform.position = target;
                path.Pop();
            }
        }
        else {
            RemoveSelectedTiles();
            moving = false;
            actionPhase = true;
        }
    }

    protected Tile FindLowestF(List<Tile> list) {
        Tile lowest = list[0];

        foreach (Tile t in list) {
            if (t.f < lowest.f) {
                lowest = t;
            }
        }

        list.Remove(lowest);

        return lowest;
    }

    protected Tile FindEndTile(Tile t) {
        Stack<Tile> tempPath = new Stack<Tile>();

        Tile next = t.parent;
        while (next != null) {
            tempPath.Push(next);
            next = next.parent;
        }

        if (tempPath.Count <= movingPoints/* - attackRange*/) {
            return t.parent;
        }

        Tile endTile = null;
        for (int i = 0; i <= movingPoints/* - attackRange*/; i++) {
            endTile = tempPath.Pop();
        }

        return endTile;
    }
    
    //A*
    protected void FindPath(Tile target) {
        ComputeAdjacencyLists(jumpHeight, target, gameObject.tag);
        GetCurrentTile();

        List<Tile> openList = new List<Tile>();
        List<Tile> closedList = new List<Tile>();

        openList.Add(currentTile);
        //currentTile.parent = ??
        currentTile.h = Vector3.Distance(currentTile.transform.position, target.transform.position);
        currentTile.f = currentTile.h;

        while (openList.Count > 0) {
            Tile t = FindLowestF(openList);

            closedList.Add(t);

            if (t == target) {
                actualTargetTile = FindEndTile(t);
                MoveToTile(actualTargetTile);
                tilesWithEnemies.Clear();
                return;
            }

            foreach (Tile tile in t.adjacencyList) {
                if (closedList.Contains(tile)) {
                    //Do nothing, already processed
                }
                else if (openList.Contains(tile)) {
                    float tempG = t.g + Vector3.Distance(tile.transform.position, t.transform.position);

                    if (tempG < tile.g) {
                        tile.parent = t;

                        tile.g = tempG;
                        tile.f = tile.g + tile.h;
                    }
                }
                else {
                    tile.parent = t;

                    tile.g = t.g + Vector3.Distance(tile.transform.position, t.transform.position);
                    tile.h = Vector3.Distance(tile.transform.position, target.transform.position);
                    tile.f = tile.g + tile.h;

                    openList.Add(tile);
                }
            }
        }

        //todo - what do you do if there is no path to the target tile?
        Debug.Log("Path not found");
    }

    //turn to play for this unit
    public void BeginTurn() {
        Debug.Log(name + " begin");
        turn = true;
        if(gameObject.tag == "Player") {
            UIManager.ChangePlayer(gameObject.GetComponent<PlayerMove>());
        }
    }

    public void EndTurn() {
        UIManager.Reset();
        Debug.Log(name + " end");
        turn = false;
    }


    public virtual void PassTurn() {
        Debug.Log("pass turn");
    }
}
