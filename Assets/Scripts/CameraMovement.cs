using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour {
    public float maxHeight = 8.0f;
    public float movingSpeed = 20.0f;
    
    private GameObject map;
    private BoxCollider mapColl; //dimensions of the map to not go too far
    private static List<Unit> units; //every unit in the field

    //to follow units
    static CameraMovement camera;
    private GameObject followedUnit;
    private bool followingUnits = true;
    private Text followUnitsText;
    private bool doneMoving = true;

    // Start is called before the first frame update
    void Start() {
        camera = this;
        units = new List<Unit>(GameObject.FindObjectsOfType<Unit>());
        map = GameObject.FindGameObjectWithTag("Map");
        mapColl = map.GetComponent<BoxCollider>();
        followUnitsText = GameObject.Find("FollowUnitButton").transform.Find("Text").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update() {
        RotateUnitsHPBar();

        //move with middle-click+ mouse direction
        if (Input.GetKey(KeyCode.Mouse2)) {

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
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && transform.position.y < map.transform.position.y+maxHeight) {
            transform.Translate(Vector3.up * movingSpeed * Time.deltaTime);
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


        if (Input.GetKeyDown(KeyCode.E)) {
            UpdateFollowUnit();
        }

    }


    public void ResetCameraPos() {
        transform.position = new Vector3(0, 0, 0);
        transform.rotation = new Quaternion(0, 0, 0, 0);
        CameraMovement.FollowUnit(followedUnit);
    }

    public void RotateLeft() {
        transform.position = new Vector3(0, 0, 0);
        transform.Rotate(Vector3.up, 90, Space.Self);
        CameraMovement.FollowUnit(followedUnit);
    }
    public void RotateRight() {
        transform.position = new Vector3(0, 0, 0);
        transform.Rotate(Vector3.up, -90, Space.Self);
        CameraMovement.FollowUnit(followedUnit);
    }


    private void RotateUnitsHPBar() {
        foreach (Unit u in units) {
            u.hpCanvas.transform.rotation = Quaternion.Euler(45, transform.rotation.eulerAngles.y, 0);
        }
    }


    public void UpdateFollowUnit() {
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

    public static void FollowUnit(GameObject unit) {
        if (unit != null && camera.followingUnits) {
            camera.doneMoving = false;
            float distFromUnit = 5f;
            camera.followedUnit = unit;

            float yPos = camera.transform.rotation.eulerAngles.y;
            if (yPos == 0)
                camera.transform.position = new Vector3(unit.transform.position.x, camera.transform.position.y, unit.transform.position.z + distFromUnit);
            else if (yPos == 90)
                camera.transform.position = new Vector3(unit.transform.position.x + distFromUnit, camera.transform.position.y, unit.transform.position.z);
            else if (yPos == 180)
                camera.transform.position = new Vector3(unit.transform.position.x, camera.transform.position.y, unit.transform.position.z - distFromUnit);
            else if (yPos == 270)
                camera.transform.position = new Vector3(unit.transform.position.x - distFromUnit, camera.transform.position.y, unit.transform.position.z);

            camera.doneMoving = true;
        }
    }

    public static bool IsDoneMoving() {
        return camera.doneMoving;
    }
}
