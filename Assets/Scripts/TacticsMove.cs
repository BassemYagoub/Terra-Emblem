using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMove : MonoBehaviour {

    /// <summary>
    /// singleton that can be updated by PlayerMove and NPCMove
    /// </summary>
    protected Unit tacticsMoveUnit;

    //stats
    public int movingRange = 5; //max tile distance attainable in a turn
    public int attackRange = 1; //max tile distance attackable in a turn
    public float moveSpeed = 6; //moving speed for units
    public float jumpVelocity = 4.5f; //not used for the moment
    public float jumpHeight = 1; //same

    //states
    public bool jumping = false; //not used for the moment
    public bool movingEdge = false; //same
    public bool turn = false; //is it this unit's turn to play
    public bool actionPhase = false; //if the unit moved but can still attack
    public bool moving = false; //is the unit moving
    public bool falling = false; //same
    public static bool changingTurn = true; //cannot move if changing turn

    GameObject[] tiles; //every tile in the map
    List<Tile> selectableTiles = new List<Tile>(); //list of tiles where the unit can go to
    List<Tile> attackableTiles = new List<Tile>(); //list of tiles where the unit can attack
    List<Tile> tilesWithEnemies = new List<Tile>(); //list of tiles where a unit from the opposite team are
    protected Stack<Tile> path = new Stack<Tile>(); //the tiles that have been chosen to go to a destination
    protected Tile currentTile; //tile beneath the unit


    //variables used for the jump
    Vector3 velocity = new Vector3();
    Vector3 heading = new Vector3();
    Vector3 jumpTarget;
    float halfHeight = 0; //height of character (often used)

    protected Tile actualTargetTile;
    protected Animator animator; //animator is here to know whether the unit should walk or run
    protected bool foundTiles = false; //to not call FindTiles functions every frame

    /// <summary>
    /// Initializes elements in Start Method
    /// </summary>
    protected void Init() {
        tiles = GameObject.FindGameObjectsWithTag("Tile");
        halfHeight = GetComponent<Collider>().bounds.extents.y;
        tacticsMoveUnit = gameObject.GetComponent<Unit>();
        TurnManager.AddUnit(this);

        animator = gameObject.GetComponent<Animator>();
    }

    /// <summary>
    /// Returns the target tile to go to
    /// </summary>
    /// <param name="target">the targel tile GameObject</param>
    /// <returns></returns>
    public Tile GetTargetTile(GameObject target) {
        RaycastHit hit;
        Tile tile = null;
        if (Physics.Raycast(target.transform.position, Vector3.down, out hit, 1)) {
            tile = hit.collider.GetComponent<Tile>();
        }

        return tile;
    }

    /// <summary>
    /// Computes the adjacency lists for each tile in the map
    /// </summary>
    /// <param name="jumpHeight">unit's jumping height</param>
    /// <param name="target">target tile</param>
    /// <param name="team">team tag</param>
    /// <param name="attackPhase">can the player still move</param>
    /// <param name="checkEnemyReachable">if true : checks if tiles are reachable by enemies</param>
    public void ComputeAdjacencyLists(float jumpHeight, Tile target, string team, bool attackPhase=false, bool checkEnemyReachable = false) {
        //tiles = GameObject.FindGameObjectsWithTag("Tile"); //if changing map during game

        foreach (GameObject tile in tiles) {
            Tile t = tile.GetComponent<Tile>();
            t.FindNeighbors(jumpHeight, target, team, attackPhase, checkEnemyReachable);

            if (t.checkIfEnemyOnTop(gameObject.tag)) {
                tilesWithEnemies.Add(t);
            }
        }
    }

    /// <summary>
    /// Returns the tile beneath playing unit
    /// </summary>
    public void GetCurrentTile() {
        currentTile = GetTargetTile(gameObject);
        currentTile.current = true;
    }

    /// <summary>
    /// Does a BFS to find selectable, attackable and enemyReachable tiles
    /// </summary>
    /// <param name="enemyReachableMode">if true : checks if tiles are reachable by enemies</param>
    public void FindSelectableTiles(bool enemyReachableMode = false) {
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
            if (!t.attackable && !enemyReachableMode) {
                t.selectable = true;
            }

            if (enemyReachableMode) {
                t.reachableByEnemy = true;
            }

            if (t.distance < movingRange) {
                foreach (Tile tile in t.adjacencyList) {
                    if (!tile.visited) {
                        tile.parent = t;
                        tile.visited = true;
                        if (enemyReachableMode) {
                            tile.reachableByEnemy = true;
                        }
                        tile.distance = t.distance + 1;
                        process.Enqueue(tile);
                    }
                }
            }
            //gather border of selectable tiles
            if (t.distance == movingRange) {
                processAttackable.Enqueue(t);
            }
        }

        FindAttackableBorder(processAttackable, enemyReachableMode);
        FindReachableEnemies(tilesWithEnemies, enemyReachableMode);

    }

    /// <summary>
    /// Finds which tiles are attackable in the border of the selectable tiles
    /// </summary>
    /// <param name="processAttackable">Queue containg selectable tiles in the border</param>
    /// <param name="enemyReachableMode">if true : checks if tiles are reachable by enemies</param>
    public void FindAttackableBorder(Queue<Tile> processAttackable, bool enemyReachableMode = false) {
        while (processAttackable.Count > 0) {
            Tile t = processAttackable.Dequeue();
            attackableTiles.Add(t);

            if (t.distance < movingRange + attackRange) {
                foreach (Tile tile in t.adjacencyList) {
                    if (!tile.visited) {
                        tile.parent = t;
                        tile.visited = true;
                        tile.distance = t.distance + 1;

                        if (!enemyReachableMode) {
                            tile.attackable = true;
                        }
                        else {
                            tile.reachableByEnemy = true;
                        }
                        processAttackable.Enqueue(tile);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds which enemies are within reach for the current unit
    /// </summary>
    /// <param name="tilesWithEnemies">List containg the tiles beneath every enemy</param>
    /// <param name="enemyReachableMode">if true : checks if tiles are reachable by enemies</param>
    public void FindReachableEnemies(List<Tile> tilesWithEnemies, bool enemyReachableMode) {
        Queue<Tile> reprocess = new Queue<Tile>();

        foreach (Tile t in tilesWithEnemies) {
            //Debug.Log(t.name);
            bool isAttackable = false;


            foreach (Tile adjTile in t.adjacencyList) {

                //neighbour selectable <=> t attackable + not in any path
                if (adjTile.selectable) {
                    if (adjTile.current) {
                        Debug.Log(adjTile.name);
                    }
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
                else if (adjTile.attackable /*&& !adjTile.enemyOnTop*/) {
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

            if (isAttackable){
                if (!enemyReachableMode) {
                    t.attackable = true;
                }
                else {
                    t.reachableByEnemy = true;
                }
            }
            else {
                if (t.IsNeighborCurrent(jumpHeight, tag)) {
                    if (!enemyReachableMode) {
                        t.attackable = true;
                    }
                    else {
                        t.reachableByEnemy = true;
                    }
                }
                else {
                    t.attackable = false;
                }
            }
        }

        currentTile.adjacencyList.RemoveAll(item => item.enemyOnTop);

        tilesWithEnemies.Clear();

    }

    /// <summary>
    /// Finds which tiles can be attacked after the unit made a move
    /// </summary>
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

        tilesWithEnemies.Clear();
    }

    /// <summary>
    /// Moves unit to a certain tile
    /// </summary>
    /// <param name="tile">the tile to move to</param>
    public void MoveToTile(Tile tile) {
        path.Clear();
        tile.target = true;
        moving = true;

        Tile next = tile;
        while (next != null) {
            //Debug.Log("next=" + next);
            path.Push(next);
            next.inTheWay = true;
            next = next.parent;
        }

        //animation is different if the tile is close
        if(tile != currentTile) {
            if(path.Count > 3) {
                animator.SetBool("isRunning", true);
            }
            else {
                animator.SetBool("isWalking", true);
            }
        }
    }

    public void ResetAllTiles() {
        foreach(GameObject t in tiles) {
            t.GetComponent<Tile>().Reset();
        }
        selectableTiles.Clear();
        attackableTiles.Clear();
        tilesWithEnemies.Clear();
    }

    public void RemoveSelectedTiles() {
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


    //Jumping functions
    void CalculateHeading(Vector3 target) {
        heading = target - transform.position;
        heading.Normalize();
    }

    void SetHorizontalVelocity() {
        velocity = heading * moveSpeed;
    }

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
            //gameObject.GetComponent<Animator>().SetTrigger("Jump");
            JumpUpward(target);
        }
        else if (movingEdge) {
            MoveToEdge();
        }
        else {
            PrepareJump(target);
        }
    }

    /// <summary>
    /// Move unit to next tile in path
    /// </summary>
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

            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
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

        if (tempPath.Count <= movingRange/* - attackRange*/) {
            return t.parent;
        }

        Tile endTile = null;
        for (int i = 0; i <= movingRange/* - attackRange*/; i++) {
            endTile = tempPath.Pop();
        }

        return endTile;
    }

    /// <summary>
    /// Finds path from current tile to target tile with the A* algorithm
    /// </summary>
    /// <param name="target">target tile</param>
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

        //no path to target tile => change target (check npcMove)
        Debug.Log("Path not found to :"+target.name);
        Debug.Log(target.transform.position);

        actualTargetTile = null;

    }

    /// <summary>
    /// Changes current unit
    /// </summary>
    public void BeginTurn() {
        Debug.Log(name + " begin");
        turn = true;
        if(gameObject.tag == "Player") {
            UIManager.ChangeCurrentUnit(gameObject.GetComponent<PlayerMove>());
            UIManager.ShowPlayerActions();
            CameraMovement.UpdateFollowedUnit(gameObject);
        }
        else {
            UIManager.HidePanelsBetweenTurns(); //not most efficient way
        }
        StartCoroutine(CameraMovement.FollowUnit(gameObject, 0.1f));
    }

    public void EndTurn() {
        UIManager.HidePanelsBetweenTurns();
        Debug.Log(name + " end");
        turn = false;
    }


    public virtual void PassTurn() {
        Debug.Log("pass turn");
    }

    public string GetHP() {
        return tacticsMoveUnit.currentHP + "/" + tacticsMoveUnit.maxHP;
    }

    public string GetLvl() {
        return ""+tacticsMoveUnit.lvl;
    }

    public string GetMovingRange() {
        return ""+movingRange;
    }

    public string GetAttackRange() {
        return ""+attackRange;
    }
}
