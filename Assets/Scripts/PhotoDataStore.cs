using UnityEngine;

public class PhotoDataStore : MonoBehaviour
{
    public static PhotoDataStore Instance;

    public Texture2D LastPhoto;
    public string LastOpportunityName;
    public Sprite TextCardSprite;
    public Vector2 PhotoPosition;
    public Vector2 TextCardPosition;
    public bool IsPortrait;

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
}