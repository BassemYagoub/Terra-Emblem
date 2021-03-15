using UnityEngine;

[System.Serializable]
public class Dialogue {
    public string interlocutor;

    [TextArea(1, 6)]
    public string lines;

    public Dialogue(string name, string lines) {
        interlocutor = name;
        this.lines = lines;
    }

}
