using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour {
    static DialogueManager manager;

    public static bool dialogueOn = true;
    Queue<Dialogue> dialogues;
    Dialogue currentDialogue;
    bool textAnimationDone = true;
    bool closePanel = false; //when to close the dialogue panel

    //UI
    public GameObject dialoguePanel;
    public GameObject inGamePanels; //all panels that are to be hidden when dialogue is ON
    public TextMeshProUGUI dialogueInterlocutor;
    public TextMeshProUGUI dialogueText;


    void Start() {
        manager = this;
        dialogues = new Queue<Dialogue>();
        dialogueOn = true;
        textAnimationDone = true;
        closePanel = false;

        ChargeDialogues();
        GoToNextDialogue();
        if (dialogueOn) {
            StartCoroutine(HideGamePanels());
        }
        else {
            dialoguePanel.SetActive(false);
        }
    }


    //REALLY BAD WAY OF STORING DIALOGUES (but I'm being lazy) 
    //=> to change if more dialogues are to be put in game
    void ChargeDialogues() {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Level0") {
            dialogues.Enqueue(new Dialogue("Zack", "Hey, here comes the new guy !\nLee, right ?\nI'm Zack, and I'll be the one supervising the next missions."));
            dialogues.Enqueue(new Dialogue("Zack", "Anyway, first time in here, isn't it ?\nSo, before going \"au charbon\", I want to see what you can do."));
            dialogues.Enqueue(new Dialogue("Zack", "Don't worry, even if this thing looks realistic it's just a dummy puppet.\nThe goal here is not to take it down. In fact, it's programmed to beat you up, so, don't push your luck."));
            dialogues.Enqueue(new Dialogue("Zack", "I just want to see if you know all the basics you are supposed to know from your training.\nIf you want to try out some things, now is the time."));
            dialogues.Enqueue(new Dialogue("Zack", "\nWhen you're done, we'll join the others.\nLet's start whenever you're ready.", true));
        }
        else if (sceneName == "Level1") {
            dialogues.Enqueue(new Dialogue("Laydee Ron", "...\n\nWhat the hell were you two doing ?"));
            dialogues.Enqueue(new Dialogue("Zack", "Come on now, don't blame your sweat Zacky, you know the procedure for newcomers."));
            dialogues.Enqueue(new Dialogue("Laydee Ron", "Bla bla bla...\nLet's get started, they won't wait for us eternally.\n\n(They will)"));
            dialogues.Enqueue(new Dialogue("Lee", "Humm...\nWeren't we supposed to meet \"the otherS\" ?"));
            dialogues.Enqueue(new Dialogue("Laydee Ron", "Yeah, well, you'll learn that, as always, our last member is late.\nNow, let's get rid of this.", true));
        }
        else if (sceneName == "Level2") {
            dialogues.Enqueue(new Dialogue("Zack", "Four of them now ?!"));
            dialogues.Enqueue(new Dialogue("Zack", "Ok guys, they might be more than us but we can take advantage of the field.\nAnd be careful, they have a ranged soldier too."));
            dialogues.Enqueue(new Dialogue("Laydee Ron", "Heard that, new guy ?\nWe count on you.\nCome on now.", true));
        }
        else if (sceneName == "Level3") {
            dialogues.Enqueue(new Dialogue("Alaska", "Hello There !\nI missed you guys !"));
            dialogues.Enqueue(new Dialogue("Zack", "..."));
            dialogues.Enqueue(new Dialogue("Laydee Ron", "..."));
            dialogues.Enqueue(new Dialogue("Alaska", "Oh, don't look at me like that !\nI'm sure you did fine, even if it's without me. And I'm here now, isn't it what matters ?"));
            dialogues.Enqueue(new Dialogue("Zack", "..."));
            dialogues.Enqueue(new Dialogue("Laydee Ron", "..."));
            dialogues.Enqueue(new Dialogue("Lee", "(Ok...)", true));

            //ambush
            dialogues.Enqueue(new Dialogue("Laydee Ron", "Shit !\nIt was an ambush."));
            dialogues.Enqueue(new Dialogue("Zack", "Damn, I knew something was off."));
            dialogues.Enqueue(new Dialogue("Zack", "Let's keep it calm and think about our options.", true));

        }
    }

    IEnumerator TextAnimation() {
        dialogueText.text = "";
        foreach (char letter in currentDialogue.lines) {
            if (!textAnimationDone) { //if wanting to pass animation
                dialogueText.text += letter;
                yield return new WaitForSeconds(.02f);
            }
        }
        textAnimationDone = true;
    }

    IEnumerator HideGamePanels() {
        yield return new WaitForSeconds(.1f);
        inGamePanels.SetActive(false);
    }

    public void GoToNextDialogue() {
        if(dialogues.Count > 0 && dialogueOn && !closePanel) {
            if (textAnimationDone) {
                textAnimationDone = false;
                currentDialogue = dialogues.Dequeue();

                if (currentDialogue.isLastLine) {
                    closePanel = true;
                }

                dialogueInterlocutor.text = currentDialogue.interlocutor;

                //follow speaking unit to add some life to dialogues
                if(dialogueInterlocutor.text == "Laydee Ron") { //don't want complete name in other game UI
                    CameraMovement.FollowUnit("Laydee");
                }
                else {
                    CameraMovement.FollowUnit(dialogueInterlocutor.text);
                }

                dialogueText.text = currentDialogue.lines;
                StartCoroutine(TextAnimation());
                return;
            }

        }
        
        if (closePanel && textAnimationDone) { //close only if text fully shown
            dialogueOn = false;
            closePanel = false;
            dialoguePanel.GetComponent<Animator>().SetTrigger("ExitPanel");
            inGamePanels.SetActive(true);
        }

        if (!textAnimationDone && dialogueOn) { //clicking when text is animating => pass animation
            textAnimationDone = true;
            dialogueText.text = currentDialogue.lines;
        }
    }

    public static bool InDialogueMode() {
        return dialogueOn;
    }
    
    public static void TriggerDialogue() {
        dialogueOn = true;
        manager.inGamePanels.SetActive(false);
        manager.dialoguePanel.GetComponent<Animator>().SetTrigger("OpenPanel");
        manager.GoToNextDialogue();
    }
}
