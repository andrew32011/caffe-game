using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    [Header("Database")]
    public DialogueDatabase database;

    [Header("Settings")]
    public string currentLanguage = "ru"; // "ru" ��� "en"

    // ����������� ��������� ���������� ������ (���� = lineID)
    private HashSet<string> usedUniqueLines = new HashSet<string>();

    private void Awake()
    {
        if (database != null)
        {
            database.BuildCache();
        }

        // Язык от плагина Яндекс Игр (YG2, модуль Localization); без модуля — "ru"
        currentLanguage = Loc.IsRu ? "ru" : "en";

        LoadUsedLines();
    }

    // ��������� �������������� ������� �� ����������
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

    // ��������� �������������� �������
    private void SaveUsedLines()
    {
        string saved = string.Join("|", usedUniqueLines);
        PlayerPrefs.SetString("Dialogue_UsedLines", saved);
        PlayerPrefs.Save();
    }

    // ���������, �������������� �� ���������� �������
    public bool WasLineUsed(string lineID)
    {
        return usedUniqueLines.Contains(lineID);
    }

    // �������� ������� ��� ��������������
    public void MarkLineAsUsed(string lineID)
    {
        if (!usedUniqueLines.Contains(lineID))
        {
            usedUniqueLines.Add(lineID);
            SaveUsedLines();
        }
    }

    // �������� ����� ������� � ������ ���� �������
    public string GetLineText(string lineID, string characterID, bool conditionMet = false)
    {
        if (database == null) return "";

        DialogEntry entry = database.GetByID(lineID);

        if (entry == null)
        {
            Debug.LogWarning($"Dialogue line not found: {lineID}");
            return "";
        }

        // �������� �������� � ���������
        if (entry.characterID != characterID)
        {
            Debug.LogWarning($"Line {lineID} not for character {characterID}");
            return "";
        }

        // ���������� ������� ��� ��������������
        if (entry.isUnique && WasLineUsed(lineID))
        {
            return "";
        }

        // ���������� �������, �� ������� �� ���������
        if (entry.isUnique && !conditionMet)
        {
            return "";
        }

        // �������� ��� ��������������
        if (entry.isUnique && conditionMet)
        {
            MarkLineAsUsed(lineID);
        }

        return currentLanguage == "ru" ? entry.russian : entry.english;
    }

    // �������� ��������� ���������� ������� ��������� (������� ��� �� ��������������)
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

    // �������� ��������� ����� (�� ����������) ������� ���������
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

    // �������� ������� � ��������: ������� ���������� (���� �������), ����� �����
    public string GetLineWithFallback(string lineID, string characterID, bool conditionMet = false)
    {
        string text = GetLineText(lineID, characterID, conditionMet);

        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        // ������: ��������� ����� ������� ���������
        return GetRandomCommonLine(characterID);
    }
}