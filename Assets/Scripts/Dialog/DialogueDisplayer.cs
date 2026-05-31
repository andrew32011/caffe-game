using UnityEngine;
using UnityEngine.UI;

public class DialogueDisplayer : MonoBehaviour
{
    [Header("UI Elements")]
    public Text dialogText;
    public GameObject dialogBox;

    [Header("References")]
    public DialogueManager manager;

    private void Start()
    {
        if (dialogBox != null) dialogBox.SetActive(false);
    }

    // ѕоказать конкретную реплику
    public void ShowLine(string lineID, string characterID, bool conditionMet = false)
    {
        if (manager == null || dialogText == null) return;

        string text = manager.GetLineWithFallback(lineID, characterID, conditionMet);

        if (!string.IsNullOrEmpty(text))
        {
            if (dialogBox != null) dialogBox.SetActive(true);
            dialogText.text = text;
        }
        else
        {
            HideDialogue();
        }
    }

    // ѕоказать случайную уникальную реплику (если условие выполнено)
    public void ShowUniqueLine(string characterID, bool conditionMet = true)
    {
        if (manager == null || dialogText == null) return;

        string text = conditionMet
            ? manager.GetRandomUniqueLine(characterID, true)
            : manager.GetRandomCommonLine(characterID);

        if (!string.IsNullOrEmpty(text))
        {
            if (dialogBox != null) dialogBox.SetActive(true);
            dialogText.text = text;
        }
        else
        {
            HideDialogue();
        }
    }

    public void HideDialogue()
    {
        if (dialogBox != null) dialogBox.SetActive(false);
        if (dialogText != null) dialogText.text = "";
    }
}