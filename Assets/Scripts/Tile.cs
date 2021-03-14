using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {

    public bool current = false;
    public bool target = false;
    public bool selectable = false;
    public bool attackable = false;
    public bool walkable = true;
    public bool enemyOnTop = false;
    public bool mouseOver = false;
    public bool reachableByEnemy = false;
    public bool inTheWay = false;

    public List<Tile> adjacencyList = new List<Tile>();

    //For BFS
    public bool visited = false;
    public Tile parent = null;
    public int distance = 0;

    //For A* (costs)
    public float f = 0;
    public float g = 0;
    public float h = 0;

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        if (current) {
            if (!reachableByEnemy)
                GetComponent<Renderer>().material.color = Color.cyan;
            else
                GetComponent<Renderer>().material.color = new Color32(0xE6, 0x00, 0xFF, 0xFF);
        }
        else if (inTheWay) {
            GetComponent<Renderer>().material.color = Color.yellow;
        }
        else if (target) {
            GetComponent<Renderer>().material.color = Color.green;
        }
        else if (attackable) {
            if (!reachableByEnemy)
                GetComponent<Renderer>().material.color = new Color32(0xF1, 0x00, 0x00, 0xFF);
            else
                GetComponent<Renderer>().material.color = new Color32(0xA9, 0x00, 0x08, 0xFF);
        }
        else if (selectable) {
            if (!mouseOver) {
                if(!reachableByEnemy)
                    GetComponent<Renderer>().material.color = new Color32(0x58, 0x7B, 0xFF, 0xFF);
                else
                    GetComponent<Renderer>().material.color = new Color32(0x94, 0x7B, 0xFF, 0xFF);
            }
            else {
                GetComponent<Renderer>().material.color = new Color32(0x00, 0x35, 0xFF, 0xFF);
            }
        }
        else if (reachableByEnemy) {
            GetComponent<Renderer>().material.color = new Color32(0xA4, 0x00, 0x69, 0xFF);
        }
        else {
            GetComponent<Renderer>().material.color = Color.white;//new Color32(0x8F, 0xFF, 0x83, 0xFF);
        }
    }

    void OnMouseOver() {
        mouseOver = true;
        if (selectable) {
            UIManager.ChangeCursor("feet");
        }
    }

    void OnMouseExit() {
        mouseOver = false;
        UIManager.ChangeCursor("arrow");
    }

    public void Reset() {
        adjacencyList.Clear();
        current = false;
        target = false;
        attackable = false;
        selectable = false;
        enemyOnTop = false;
        inTheWay = false;
        //reachableByEnemy = false;


        visited = false;
        parent = null;
        distance = 0;
    }

    public void FindNeighbors(float jumpHeight, Tile target, string team, bool attackPhase = false, bool checkEnemyReachable = false) {
        Reset();

        CheckTile(Vector3.forward, jumpHeight, target, team, attackPhase, checkEnemyReachable);
        CheckTile(Vector3.back, jumpHeight, target, team, attackPhase, checkEnemyReachable);
        CheckTile(Vector3.right, jumpHeight, target, team, attackPhase, checkEnemyReachable);
        CheckTile(Vector3.left, jumpHeight, target, team, attackPhase, checkEnemyReachable);
    }


    
    public bool checkIfEnemyOnTop(string team) {
        if (!enemyOnTop) {
            RaycastHit hit;

            //if something on top of tile
            bool touchUnit = Physics.Raycast(gameObject.transform.position, Vector3.up, out hit, 1);

            if (touchUnit) {
                //see attackable tiles within selectable tiles
                if (hit.transform.gameObject.tag != team) {
                    enemyOnTop = true;

                    selectable = false;

                    return true;
                }
            }
            return false;
        }
        else {
            return true;
        }

    }

    public void CheckTile(Vector3 dir, float jumpHeight, Tile target, string team, bool actionPhase, bool checkEnemyReachable) {
        Vector3 halfExtents = new Vector3(0.25f, (1+jumpHeight)/2.0f, 0.25f);
        Collider[] colliders = Physics.OverlapBox(transform.position + dir, halfExtents);

        foreach(Collider col in colliders) {
            if (!actionPhase) {
                Tile tile = col.GetComponent<Tile>();

                if (tile != null && tile.walkable) {
                    RaycastHit hit;

                    //if something on top of tile
                    bool touchUnit = Physics.Raycast(tile.transform.position, Vector3.up, out hit, 1);

                    //1st condition for A*
                    if ((tile == target) || !touchUnit) {
                        adjacencyList.Add(tile);
                    }
                    else if(touchUnit) {
                        if (current) {
                            Debug.Log(tile.name);
                            tile.adjacencyList.Add(this);
                        }

                        //check if enemy can attack player
                        if (hit.transform.gameObject.tag == "Player" && checkEnemyReachable) {
                            this.adjacencyList.Add(tile);
                        }
                    }
                    
                }
            }
            else {
                Tile tile = col.GetComponent<Tile>();

                if (tile != null && tile.walkable) {
                    adjacencyList.Add(tile);
                }

            }
        }
    }
    

    //check if tile with enemy is next to current tile
    public bool IsNeighborCurrent(float jumpHeight, string team) {
        Vector3 halfExtents = new Vector3(0.25f, (1 + jumpHeight) / 2.0f, 0.25f);
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right};

        foreach (Vector3 dir in directions) {
            Collider[] colliders = Physics.OverlapBox(transform.position + dir, halfExtents);

            foreach (Collider col in colliders) {
                Tile tile = col.GetComponent<Tile>();

                if (tile != null && tile.current) {
                    return true;
                }
            }
        }

        return false;
    }

}
