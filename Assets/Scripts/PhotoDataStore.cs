using UnityEngine;
using System.Collections.Generic;

// Define enum outside the class so all scripts can access it
public enum DiaryEntryReason { Photo, Escape }

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

    public Texture2D LastPhoto;
    public string LastOpportunityName;
    public Sprite TextCardSprite;
    public Vector2 PhotoPosition;
    public Vector2 TextCardPosition;
    public bool IsPortrait;

    public int Body;
    public int Mind;
    public int Charisma;

    public string PreviousScene;
    public string CurrentScene;

    public DiaryEntryReason EntryReason;

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
            photo            = LastPhoto,
            textCard         = TextCardSprite,
            opportunityName  = LastOpportunityName,
            isPortrait       = IsPortrait,
            photoPosition    = PhotoPosition,
            textCardPosition = TextCardPosition
        };
        DiaryEntries.Add(entry);
    }

    public void ResetAll()
    {
        LastPhoto           = null;
        LastOpportunityName = string.Empty;
        TextCardSprite      = null;
        PhotoPosition       = Vector2.zero;
        TextCardPosition    = Vector2.zero;
        IsPortrait          = false;

        Body     = 0;
        Mind     = 0;
        Charisma = 0;

        PreviousScene = string.Empty;
        CurrentScene  = string.Empty;
        EntryReason   = DiaryEntryReason.Photo;

        DiaryEntries.Clear();
    }
}