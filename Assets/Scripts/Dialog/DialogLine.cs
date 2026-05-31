using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DialogEntry
{
    [Header("Identification")]
    public string lineID = "line_001";      // Уникальный ID реплики
    public string characterID = "guest_01"; // ID персонажа

    [Header("Content")]
    [TextArea(2, 4)] public string russian = "Бормотание...";
    [TextArea(2, 4)] public string english = "Murmuring...";
    [TextArea(2, 4)] public string turkey = "lala...";
    [Header("Behavior")]
    public bool isUnique = false; // Уникальная реплика (1 раз за игру)
}

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Dialogue/Database", order = 1)]
public class DialogueDatabase : ScriptableObject
{
    public List<DialogEntry> entries = new List<DialogEntry>();

    // Кэш для быстрого поиска по ID
    private Dictionary<string, DialogEntry> cacheByID;
    // Кэш для поиска по персонажу + условию уникальности
    private Dictionary<string, List<DialogEntry>> cacheByCharacter;

    public void BuildCache()
    {
        cacheByID = new Dictionary<string, DialogEntry>();
        cacheByCharacter = new Dictionary<string, List<DialogEntry>>();

        foreach (var entry in entries)
        {
            // Кэш по ID
            if (!cacheByID.ContainsKey(entry.lineID))
            {
                cacheByID[entry.lineID] = entry;
            }

            // Кэш по персонажу
            if (!cacheByCharacter.ContainsKey(entry.characterID))
            {
                cacheByCharacter[entry.characterID] = new List<DialogEntry>();
            }
            cacheByCharacter[entry.characterID].Add(entry);
        }
    }

    // Получить реплику по ID
    public DialogEntry GetByID(string lineID)
    {
        if (cacheByID == null) BuildCache();
        return cacheByID.TryGetValue(lineID, out var entry) ? entry : null;
    }

    // Получить все реплики персонажа
    public List<DialogEntry> GetByCharacter(string characterID)
    {
        if (cacheByCharacter == null) BuildCache();
        return cacheByCharacter.TryGetValue(characterID, out var list) ? list : new List<DialogEntry>();
    }
}