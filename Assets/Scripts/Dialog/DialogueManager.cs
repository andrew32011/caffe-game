using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    [Header("Database")]
    public DialogueDatabase database;

    [Header("Settings")]
    public string currentLanguage = "ru"; // "ru" или "en"

    // Сохранённые состояния уникальных реплик (ключ = lineID)
    private HashSet<string> usedUniqueLines = new HashSet<string>();

    private void Awake()
    {
        if (database != null)
        {
            database.BuildCache();
        }

        LoadUsedLines();
    }

    // Загрузить использованные реплики из сохранения
    private void LoadUsedLines()
    {
        string saved = PlayerPrefs.GetString("Dialogue_UsedLines", "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] ids = saved.Split('|');
            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    usedUniqueLines.Add(id);
                }
            }
        }
    }

    // Сохранить использованные реплики
    private void SaveUsedLines()
    {
        string saved = string.Join("|", usedUniqueLines);
        PlayerPrefs.SetString("Dialogue_UsedLines", saved);
        PlayerPrefs.Save();
    }

    // Проверить, использовалась ли уникальная реплика
    public bool WasLineUsed(string lineID)
    {
        return usedUniqueLines.Contains(lineID);
    }

    // Отметить реплику как использованную
    public void MarkLineAsUsed(string lineID)
    {
        if (!usedUniqueLines.Contains(lineID))
        {
            usedUniqueLines.Add(lineID);
            SaveUsedLines();
        }
    }

    // Получить текст реплики с учётом всех условий
    public string GetLineText(string lineID, string characterID, bool conditionMet = false)
    {
        if (database == null) return "";

        DialogEntry entry = database.GetByID(lineID);

        if (entry == null)
        {
            Debug.LogWarning($"Dialogue line not found: {lineID}");
            return "";
        }

        // Проверка привязки к персонажу
        if (entry.characterID != characterID)
        {
            Debug.LogWarning($"Line {lineID} not for character {characterID}");
            return "";
        }

        // Уникальная реплика уже использовалась
        if (entry.isUnique && WasLineUsed(lineID))
        {
            return "";
        }

        // Уникальная реплика, но условие не выполнено
        if (entry.isUnique && !conditionMet)
        {
            return "";
        }

        // Отмечаем как использованную
        if (entry.isUnique && conditionMet)
        {
            MarkLineAsUsed(lineID);
        }

        return currentLanguage == "ru" ? entry.russian : entry.english;
    }

    // Получить случайную уникальную реплику персонажа (которая ещё не использовалась)
    public string GetRandomUniqueLine(string characterID, bool conditionMet = true)
    {
        var entries = database.GetByCharacter(characterID);
        List<DialogEntry> available = new List<DialogEntry>();

        foreach (var entry in entries)
        {
            if (entry.isUnique && !WasLineUsed(entry.lineID) && conditionMet)
            {
                available.Add(entry);
            }
        }

        if (available.Count == 0) return "";

        DialogEntry randomEntry = available[Random.Range(0, available.Count)];
        MarkLineAsUsed(randomEntry.lineID);

        return currentLanguage == "ru" ? randomEntry.russian : randomEntry.english;
    }

    // Получить случайную общую (не уникальную) реплику персонажа
    public string GetRandomCommonLine(string characterID)
    {
        var entries = database.GetByCharacter(characterID);
        List<DialogEntry> common = new List<DialogEntry>();

        foreach (var entry in entries)
        {
            if (!entry.isUnique)
            {
                common.Add(entry);
            }
        }

        if (common.Count == 0) return "";

        DialogEntry randomEntry = common[Random.Range(0, common.Count)];
        return currentLanguage == "ru" ? randomEntry.russian : randomEntry.english;
    }

    // Получить реплику с фолбэком: сначала уникальная (если условие), потом общая
    public string GetLineWithFallback(string lineID, string characterID, bool conditionMet = false)
    {
        string text = GetLineText(lineID, characterID, conditionMet);

        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Фолбэк: случайная общая реплика персонажа
        return GetRandomCommonLine(characterID);
    }
}