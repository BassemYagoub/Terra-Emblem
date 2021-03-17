using UnityEngine;

[System.Serializable]
public class Dialogue {
    public string interlocutor;

    [TextArea(1, 6)]
    public string lines;

    public bool isLastLine; //is this text the last of the triggered dialogue

    public Dialogue(string name, string lines, bool last = false) {
        interlocutor = name;
        this.lines = lines;
        isLastLine = last;
    }

}
