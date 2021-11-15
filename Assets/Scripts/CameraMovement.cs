using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// <summary>
/// Takes in charge every camera movement / rotation
/// </summary>
public class CameraMovement : MonoBehaviour {
    public float maxHeight = 3.0f; //limit in y-axis
    public float movingSpeed = 10.0f;
    
    private GameObject map; //reference to have boundaries for the camera
    private BoxCollider mapColl; //dimensions of the map to not go too far
    private static List<Unit> units; //every unit in the field

    //to follow units
    static CameraMovement manager; //singleton

    //is the camera following units or free to move
    private bool followingUnits = true;

    //the character that is followed by the camera if it is in follow mode
    private GameObject followedUnit;
    private TextMeshProUGUI followUnitsText;

    // Start is called before the first frame update
    void Start() {
        manager = this;
        units = new List<Unit>(GameObject.FindObjectsOfType<Unit>());
        map = GameObject.FindGameObjectWithTag("Map");
        mapColl = map.GetComponent<BoxCollider>();
        followUnitsText = GameObject.Find("FollowUnitButton").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
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

        if (!UIManager.MenuIsOn() && !TurnManager.GameEnded() && !DialogueManager.InDialogueMode()) {

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

    }

    /// <summary>
    /// Moves the camera if it's within the map's boundaries
    /// </summary>
    /// <remarks>The camera can be moved with the mouse middle click, by either clicking on it or by using the scroll wheel</remarks>
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


    /// <summary>
    /// Resets the camera position (will not change the position if camera is following units
    /// </summary>
    public void ResetCameraPos() {
        transform.position = new Vector3(0, 0, 0);
        transform.rotation = new Quaternion(0, 0, 0, 0);
    }

    /// <summary>
    /// Rotates the camera by 90°
    /// </summary>
    public void RotateLeft() {
        transform.position = new Vector3(0, 0, 0);
        transform.Rotate(Vector3.up, 90, Space.Self);
    }

    /// <summary>
    /// Rotates the camera by -90°
    /// </summary>
    public void RotateRight() {
        transform.position = new Vector3(0, 0, 0);
        transform.Rotate(Vector3.up, -90, Space.Self);
    }

    /// <summary>
    /// Rotates every unit HP Bar according to the camera current rotation
    /// </summary>
    private void RotateUnitsHPBar() {
        foreach (Unit u in units) {
            if(u.hpCanvas != null) {
                u.hpCanvas.transform.rotation = Quaternion.Euler(45, transform.rotation.eulerAngles.y, 0);
            }
        }
    }

    /// <summary>
    /// Changes the following mode to either true or false
    /// </summary>
    /// <remarks>Also updates UI text for the following mode</remarks>
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

    public static void UpdateFollowedUnit(GameObject unit) {
        manager.followedUnit = unit;
    }

    /// <summary>
    /// Makes the camera follow units
    /// </summary>
    /// <param name="unit">The unit to follow</param>
    /// <param name="waitingTime">Time to wait before starting to follow</param>
    /// <param name="dialogueMode">Optionnal parameter for optimization</param>
    /// <returns></returns>
    public static IEnumerator FollowUnit(GameObject unit, float waitingTime = 0f, bool dialogueMode = false) {
        yield return new WaitForSeconds(waitingTime);

        if (unit != null && manager.followingUnits && !UIManager.MenuIsOn()) {

            //check condition to not change postion every frame when not needed
            if (unit.GetComponent<TacticsMove>().turn || dialogueMode) {
                float distFromUnit = 5f;
                manager.followedUnit = unit;

                float yPos = manager.transform.rotation.eulerAngles.y;
                if (yPos == 0)
                    manager.transform.position = new Vector3(unit.transform.position.x, 0, unit.transform.position.z + distFromUnit);
                else if (yPos == 90)
                    manager.transform.position = new Vector3(unit.transform.position.x + distFromUnit, 0, unit.transform.position.z);
                else if (yPos == 180)
                    manager.transform.position = new Vector3(unit.transform.position.x, 0, unit.transform.position.z - distFromUnit);
                else if (yPos == 270)
                    manager.transform.position = new Vector3(unit.transform.position.x - distFromUnit, 0, unit.transform.position.z);

            }
        }

    }

    //special case for dialogues
    public static void FollowUnit(string unitName) {
        GameObject unit = GameObject.Find(unitName);
        if(unit != null) {
            manager.StartCoroutine(FollowUnit(unit, 0f, true));
        }
        else {
            Debug.LogError("Unit Not Found");
        }
    }
    /// <summary>
    /// Follows an object for a certain amount of time
    /// </summary>
    /// <remarks>Function used to do some "cutscenes"</remarks>
    /// <param name="obj">The object to follow</param>
    /// <param name="seconds">The amount of time to follow it</param>
    public static IEnumerator FollowObjectFor(GameObject obj, float seconds) {
        bool tmpFollow = manager.followingUnits;
        manager.followingUnits = false;

        manager.transform.position = new Vector3(obj.transform.position.x, manager.transform.position.y, obj.transform.position.z + 5f);

        yield return new WaitForSeconds(seconds);
        manager.UpdateFollowMode();
        TurnManager.SetTriggerToFalse();
        Debug.Log("trigger set to false");
    }

    //dumb way to not click on any object when on a mennu but it works
    public static void MoveCameraAway() {
        manager.transform.position = new Vector3(5000, 5000, 5000);
    }

    public static Vector3 GetCameraPos() {
        return manager.transform.position;
    }

    public static void MoveCameraTo(Vector3 pos) {
        manager.transform.position = pos;
    }

    //update units list to know which ones can be followed
    public static void UpdateUnits() {
        units = new List<Unit>(GameObject.FindObjectsOfType<Unit>());
    }

}
