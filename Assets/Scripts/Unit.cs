using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour {

    private TacticsMove unitTM;

    //private enum unitType {Warrior, Mage, Archer};
    private float maxHP = 40f;
    private float currentHP;
    private int lvl = 1;
    private float xp = 0.0f;

    private int strength = 5;
    private int wisdom = 2;
    private int luck = 3;

    //private string team;

    public Image hpBar;
    public GameObject dyingEffect;
    //Animation anim;


    // Start is called before the first frame update
    void Start() {
        unitTM = gameObject.GetComponent<TacticsMove>();
        currentHP = maxHP;
        //team = "blue";
    }

    // Update is called once per frame
    void Update() {

    }

    public void attackOpponent(/*Tile t, */Unit opponent) {
        /*if (opponent == null && t != null) {
            t.attackUnitOnTile();
            //Debug.Log("attack on " + t.transform.position);
        }
        else */
        if (opponent != null/* && t == null*/) {

            //checking if not in the same team
            if(opponent.gameObject.tag != gameObject.tag) {

                RaycastHit hit;

                //get tile underneath opponent
                if (Physics.Raycast(opponent.transform.position, Vector3.down, out hit, 1)) {
                    Tile tileOpponent = hit.transform.GetComponent<Tile>();
                    //Debug.Log("isAttackable : " + tileOpponent.attackable);
                    if (tileOpponent.attackable) {
                        opponent.TakeDamage(3*strength * lvl+Random.Range(wisdom, luck*5));
                        GainXP(opponent.lvl * 10);
                        unitTM.actionPhase = false;
                        unitTM.RemoveAttackableTiles();
                        TurnManager.EndTurn();
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

    void TakeDamage(float dmg) {
        currentHP -= dmg;
        hpBar.fillAmount = currentHP / maxHP;
        if (currentHP <= 0) {
            Die();
        }
    }

    void Die() {
        GameObject effectInstance  = (GameObject)Instantiate(dyingEffect, transform.position, dyingEffect.transform.rotation);
        Destroy(effectInstance, 2f);
        Destroy(gameObject, 1.5f);
        TurnManager.RemoveUnit(gameObject.GetComponent<TacticsMove>());
        TurnManager.checkIfEndGame(gameObject.tag);
        Debug.Log("Unit died");
    }

    void GainXP(float xpGained) {
        xp += xpGained;
        while(xp-100f >= 100) {
            ++lvl;
            xp -= 100;
        }
        Debug.Log("gained XP");
    }

    /*void OnMouseDown() {
        if (!isSelected) {
            print("blue");
            GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
            foreach(GameObject unit in units) {
                if (unit.GetComponent<Unit>().isSelected) {
                    unit.GetComponent<Unit>().isSelected = false;
                    unit.GetComponent<Renderer>().material.color = Color.red;
                    break;
                }
            }
            isSelected = true;
            gameObject.GetComponent<Renderer>().material.color = Color.blue;
        }
        else {
            print("red");
            gameObject.GetComponent<Renderer>().material.color = Color.red;
            isSelected = false;
        }
    }*/
}
