using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour {

    private TacticsMove unitTM;

    //private enum unitType {Warrior, Mage, Archer};
    private float maxHP = 40f;
    public float currentHP;
    private int lvl = 1;
    private float xp = 0.0f;

    private int strength = 5;
    private int wisdom = 2;
    private int luck = 3;

    //UI & Effects
    public GameObject hpCanvas;
    public Image hpBar;
    public GameObject dyingEffect;
    private float fillSpeed;
    //Animation anim;


    // Start is called before the first frame update
    void Start() {
        hpCanvas = gameObject.transform.Find("HPCanvas").gameObject;

        if (gameObject.tag == "Player") {
            hpBar.color = new Color32(0x1E, 0x76, 0xDD, 0xDD);
        }else if (gameObject.tag == "NPC") {
            hpBar.color = new Color32(0xFF, 0x0B, 0x1E, 0xDD);
        }
        unitTM = gameObject.GetComponent<TacticsMove>();
        currentHP = maxHP;
        //team = "blue";
    }

    private void Update() {
        HPBarFiller();
    }

    //rename function
    public void InflictDamage(Unit opponent) {
        int opponentDied = opponent.TakeDamage(3 * strength * lvl + Random.Range(wisdom, luck * 5));
        GainXP(opponent.lvl * 10 + (opponentDied * opponent.lvl * 5));
        unitTM.actionPhase = false;
        unitTM.RemoveAttackableTiles();
        TurnManager.EndTurn();
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
        currentHP -= dmg;
        if (currentHP <= 0) {
            Die();
            return 1;
        }
        return 0;
    }

    void Die() {
        GameObject effectInstance  = (GameObject)Instantiate(dyingEffect, transform.position, dyingEffect.transform.rotation);
        Destroy(effectInstance, 2f);
        CameraMovement.removeUnitFromList(gameObject.GetComponent<Unit>());
        Destroy(gameObject);//, 1.5f);
        TurnManager.RemoveUnit(gameObject.GetComponent<TacticsMove>());
        Debug.Log("Unit died");
    }

    void GainXP(float xpGained) {
        //Debug.Log("gained XP");
        xp += xpGained;
        while(xp-100f >= 100) {
            ++lvl;
            xp -= 100;
            Debug.Log("LVL UP");
        }
    }

}
