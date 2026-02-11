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

        public DialogueLine(string speakerName, string lineText)
        {
            speaker = speakerName ?? string.Empty;
            text = lineText ?? string.Empty;
        }
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

    public static DialogueSequence CreateRuntime(params DialogueLine[] runtimeLines)
    {
        DialogueSequence sequence = CreateInstance<DialogueSequence>();
        sequence.lines = new List<DialogueLine>();
        if (runtimeLines == null)
        {
            return sequence;
        }

        for (int i = 0; i < runtimeLines.Length; i++)
        {
            if (runtimeLines[i] != null)
            {
                sequence.lines.Add(runtimeLines[i]);
            }
        }

        return sequence;
    }

    public static DialogueLine MakeLine(string speakerName, string lineText)
    {
        return new DialogueLine(speakerName, lineText);
    }
}
