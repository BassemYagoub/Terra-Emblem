using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour {
    public float maxHeight = 3.0f;
    public float movingSpeed = 10.0f;
    
    private GameObject map;
    private BoxCollider mapColl; //dimensions of the map to not go too far
    private static List<Unit> units; //every unit in the field

    //to follow units
    static CameraMovement manager;
    private GameObject followedUnit;
    private bool followingUnits = true;
    private Text followUnitsText;
    private bool doneMoving = true;

    // Start is called before the first frame update
    void Start() {
        manager = this;
        units = new List<Unit>(GameObject.FindObjectsOfType<Unit>());
        map = GameObject.FindGameObjectWithTag("Map");
        mapColl = map.GetComponent<BoxCollider>();
        followUnitsText = GameObject.Find("FollowUnitButton").transform.Find("Text").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update() {
        RotateUnitsHPBar();
        if (followingUnits) {
            StartCoroutine(FollowUnit(followedUnit));
        }
        else {
            //move with middle-click+ mouse direction
            MoveCamera();
        }

        //camera reset to starting pos
        if (Input.GetKeyDown(KeyCode.R)) {
            ResetCameraPos();
        }

        //90° rotation of the map
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            RotateLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            RotateRight();
        }


        if (Input.GetKeyDown(KeyCode.F)) {
            UpdateFollowMode();
        }

    }

    void MoveCamera() {
        if (Input.GetKey(KeyCode.Mouse2)) {
            //security for out of bounds
            if((transform.position.x+1f < (map.transform.position.x - mapColl.size.x)) || (transform.position.x - 1f >= (map.transform.position.x + mapColl.size.x)) || (transform.position.z + 1f < (map.transform.position.z - mapColl.size.z)) || (transform.position.z - 1f >= (map.transform.position.z + mapColl.size.z)) ) {
                ResetCameraPos();
                return;
            }

            if (Input.GetAxis("Mouse X") > 0 && transform.position.x >= (map.transform.position.x - mapColl.size.x)) {
                transform.Translate(Vector3.left * movingSpeed * Time.deltaTime);
            }
            else if (Input.GetAxis("Mouse X") < 0 && transform.position.x < (map.transform.position.x + mapColl.size.x)) {
                transform.Translate(Vector3.right * movingSpeed * Time.deltaTime);
            }

            //(Y Axis of the mouse and not the one of the game)
            if (Input.GetAxis("Mouse Y") > 0 && transform.position.z >= (map.transform.position.z - mapColl.size.z)) {
                transform.Translate(Vector3.back * movingSpeed * Time.deltaTime);
            }
            else if (Input.GetAxis("Mouse Y") < 0 && transform.position.z < (map.transform.position.z + mapColl.size.z)) {
                transform.Translate(Vector3.forward * movingSpeed * Time.deltaTime);
            }
        }


        //zoom-in & zoom-out while scrolling
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && transform.position.y >= -1) {
            transform.Translate(Vector3.down * movingSpeed * Time.deltaTime);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && transform.position.y < map.transform.position.y + maxHeight) {
            transform.Translate(Vector3.up * movingSpeed * Time.deltaTime);
        }
    }


    public void ResetCameraPos() {
        transform.position = new Vector3(0, 0, 0);
        transform.rotation = new Quaternion(0, 0, 0, 0);
    }

    public void RotateLeft() {
        transform.position = new Vector3(0, 0, 0);
        transform.Rotate(Vector3.up, 90, Space.Self);
    }

    public void RotateRight() {
        transform.position = new Vector3(0, 0, 0);
        transform.Rotate(Vector3.up, -90, Space.Self);
    }

    private void RotateUnitsHPBar() {
        foreach (Unit u in units) {
            u.hpCanvas.transform.rotation = Quaternion.Euler(45, transform.rotation.eulerAngles.y, 0);
        }
    }

    public void UpdateFollowMode() {
        followingUnits = !followingUnits;
        if (followingUnits) {
            followUnitsText.text = "Follow Units : ON (F)";
        }
        else {
            followUnitsText.text = "Follow Units : OFF (F)";
        }
    }

    public static void RemoveUnitFromList(Unit u) {
        units.Remove(u);
    }

    public static IEnumerator FollowUnit(GameObject unit, float waitingTime = 0f) {
        yield return new WaitForSeconds(waitingTime);

        if (unit != null && manager.followingUnits) {

            //check condition to not change postion every frame when not needed
            if (unit.GetComponent<TacticsMove>().turn) {
                manager.doneMoving = false;
                float distFromUnit = 5f;
                manager.followedUnit = unit;

                float yPos = manager.transform.rotation.eulerAngles.y;
                if (yPos == 0)
                    manager.transform.position = new Vector3(unit.transform.position.x, manager.transform.position.y, unit.transform.position.z + distFromUnit);
                else if (yPos == 90)
                    manager.transform.position = new Vector3(unit.transform.position.x + distFromUnit, manager.transform.position.y, unit.transform.position.z);
                else if (yPos == 180)
                    manager.transform.position = new Vector3(unit.transform.position.x, manager.transform.position.y, unit.transform.position.z - distFromUnit);
                else if (yPos == 270)
                    manager.transform.position = new Vector3(unit.transform.position.x - distFromUnit, manager.transform.position.y, unit.transform.position.z);

                manager.doneMoving = true;
            }
        }

    }

    public static bool IsDoneMoving() {
        return manager.doneMoving;
    }

    //dumb way to not click on any object when game ends but it works
    public static void MoveCameraAway() {
        manager.transform.position = new Vector3(5000, 5000, 5000);
    }
}
