using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DiaryEntry
{
    public Texture2D photo;
    public Sprite textCard;
    public string opportunityName;
    public bool isPortrait;
    public Vector2 photoPosition;
    public Vector2 textCardPosition;
}

public class PhotoDataStore : MonoBehaviour
{
    public static PhotoDataStore Instance;

    // Current photo data
    public Texture2D LastPhoto;
    public string LastOpportunityName;
    public Sprite TextCardSprite;
    public Vector2 PhotoPosition;
    public Vector2 TextCardPosition;
    public bool IsPortrait;

    // Cumulative stats
    public int Body;
    public int Mind;
    public int Charisma;

    // Scene tracking
    public string PreviousScene;
    public string CurrentScene;

    // Diary entries
    public List<DiaryEntry> DiaryEntries = new List<DiaryEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddDiaryEntry()
    {
        DiaryEntry entry = new DiaryEntry
        {
            photo           = LastPhoto,
            textCard        = TextCardSprite,
            opportunityName = LastOpportunityName,
            isPortrait      = IsPortrait,
            photoPosition   = PhotoPosition,
            textCardPosition = TextCardPosition
        };
        DiaryEntries.Add(entry);
    }
}