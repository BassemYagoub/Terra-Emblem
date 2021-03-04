using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour {

    private TacticsMove unitTM;

    //private enum unitType {Warrior, Mage, Archer};
    public float maxHP = 40f;
    public float currentHP = 40f;
    public int lvl = 1;
    private float xp = 0.0f;

    private int strength = 5;
    private int wisdom = 2;
    private int luck = 3;

    //UI & Effects
    public GameObject hpCanvas;
    private Image hpBar;
    private GameObject eventText;
    private float fillSpeed;
    public GameObject dyingEffect;

    // Start is called before the first frame update
    void Start() {
        hpCanvas = gameObject.transform.Find("HPCanvas").gameObject;
        eventText = hpCanvas.gameObject.transform.Find("EventText").gameObject;

        //not pretty but practical when changing hpbar prefab
        hpBar = hpCanvas.gameObject.transform.Find("HPBG").gameObject.transform.Find("HPBar").GetComponent<Image>();

        if (gameObject.tag == "Player") {
            hpBar.color = new Color32(0x1E, 0x76, 0xDD, 0xDD);
        }else if (gameObject.tag == "NPC") {
            hpBar.color = new Color32(0xFF, 0x0B, 0x1E, 0xDD);
        }
        currentHP = maxHP;

        unitTM = gameObject.GetComponent<TacticsMove>();

    }

    private void Update() {
        HPBarFiller();
    }

    void OnMouseOver() {
        if(gameObject.tag == "NPC")
            UIManager.ChangeCursor("sword");
        else
            UIManager.ChangeCursor("hand");
    }
    void OnMouseExit() {
        UIManager.ChangeCursor("arrow");
    }

    //rename function
    public void InflictDamage(Unit opponent) {
        gameObject.transform.LookAt(opponent.transform);
        if (unitTM.attackRange == 1) {
            gameObject.GetComponent<Animator>().SetTrigger("Punch");
        }
        else {
            gameObject.transform.rotation *= Quaternion.Euler(0, 90, 0);
            gameObject.GetComponent<Animator>().SetTrigger("Shoot");
        }

        float dmg = 3 * strength * lvl + Random.Range(wisdom, luck * 5);
        int opponentDied = opponent.TakeDamage(dmg);
        float xpReceived = opponent.lvl * 10 + (opponentDied * opponent.lvl * 5);
        GainXP(xpReceived);
        ShowBattleResults(opponent, dmg, xpReceived);

        unitTM.actionPhase = false;
        unitTM.RemoveAttackableTiles();
        TurnManager.EndTurn();
    }
    void ShowBattleResults(Unit opponent, float dmg, float xpReceived) {
        eventText.GetComponent<Text>().color = Color.blue;
        eventText.GetComponent<Text>().text = "+ " + xpReceived + " XP";

        opponent.eventText.GetComponent<Text>().color = Color.red;
        opponent.eventText.GetComponent<Text>().text = "- "+dmg+" HP";

        eventText.GetComponent<Animator>().SetTrigger("newEvent");
        opponent.eventText.GetComponent<Animator>().SetTrigger("newEvent");
    }

    public void attackOpponent(Unit opponent) {
        if (opponent != null) {

            //checking if not in the same team
            if(opponent.gameObject.tag != gameObject.tag) {

                RaycastHit hit;

                //get tile underneath opponent
                if (Physics.Raycast(opponent.transform.position, Vector3.down, out hit, 1)) {
                    Tile tileOpponent = hit.transform.GetComponent<Tile>();
                    Debug.Log("isAttackable : " + tileOpponent.attackable);

                    if (tileOpponent.attackable) {
                        InflictDamage(opponent);
                    }
                    else if(gameObject.tag == "NPC") {
                        unitTM.actionPhase = false;
                        unitTM.RemoveAttackableTiles();
                        TurnManager.EndTurn();
                    }
                }
                else {
                    Debug.LogError("No tile underneath Unit");
                }
            }
        }

    }

    //possibility to not call the function every frame ?
    void HPBarFiller() {
        fillSpeed = 2f * Time.deltaTime;
        hpBar.fillAmount = Mathf.Lerp(hpBar.fillAmount, currentHP / maxHP, fillSpeed);
    }

    //returns 1 if Unit died (bool to give an XP bonus to opponent)
    int TakeDamage(float dmg) {
        Invoke("ReactAnimation", 1f);
        currentHP -= dmg;
        if (currentHP <= 0) {
            Die();
            Invoke("DieAnimation", 1f);
            Debug.Log("aaaa");

            return 1;
        }
        return 0;
    }

    void ReactAnimation() {
        gameObject.GetComponent<Animator>().SetTrigger("React");
    }

    void Die() {
        CameraMovement.RemoveUnitFromList(gameObject.GetComponent<Unit>());
        TurnManager.RemoveUnit(gameObject.GetComponent<TacticsMove>());
        if(gameObject.tag == "NPC") {
            UIManager.RemoveEnemy(gameObject);
            UIManager.ResetReachableByEnemyTiles(true);
        }
        Debug.Log("Unit died");
    }

    void DieAnimation() {
        hpCanvas.SetActive(false);
        gameObject.GetComponent<Animator>().SetBool("isDying", true);
        //GameObject effectInstance  = (GameObject)Instantiate(dyingEffect, transform.position, dyingEffect.transform.rotation);
        //Destroy(effectInstance, 2f);
        Destroy(gameObject, 2f);
    }

    void GainXP(float xpGained) {
        xp += xpGained;
        while(xp-100f >= 100) {
            ++lvl;
            xp -= 100;
            Debug.Log(name+" LVL UP : "+lvl);
        }
    }

}
