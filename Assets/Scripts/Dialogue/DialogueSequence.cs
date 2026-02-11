using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Serializable]
    public class DialogueLine
    {
        [SerializeField] private string speaker;
        [TextArea(2, 5)]
        [SerializeField] private string text;

        public string Speaker => speaker;
        public string Text => text;
    }

    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public int LineCount => lines != null ? lines.Count : 0;

    public bool TryGetLine(int index, out DialogueLine line)
    {
        line = null;
        if (lines == null || index < 0 || index >= lines.Count)
        {
            return false;
        }

        line = lines[index];
        return true;
    }
}
