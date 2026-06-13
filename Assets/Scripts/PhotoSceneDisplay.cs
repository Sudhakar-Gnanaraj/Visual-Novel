using UnityEngine;

public class PhotoSceneDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject photoObject;
    [SerializeField] private GameObject textCardObject;

    [Header("Display Settings")]
    [SerializeField] private Vector2 photoSize = new Vector2(3f, 2f);
    [SerializeField] private Vector2 textCardSize = new Vector2(3f, 2f);

    private void Awake()
    {
        Debug.Log("PhotoSceneDisplay Awake fired.");
        Debug.Log($"PhotoDataStore.Instance is null: {PhotoDataStore.Instance == null}");
    }

    private void Start()
    {
        Debug.Log("PhotoSceneDisplay Start fired.");

        if (PhotoDataStore.Instance == null)
        {
            Debug.LogError("PhotoDataStore Instance is NULL.");
            return;
        }

        Debug.Log($"LastPhoto null: {PhotoDataStore.Instance.LastPhoto == null}");
        Debug.Log($"TextCardSprite null: {PhotoDataStore.Instance.TextCardSprite == null}");
        Debug.Log($"PhotoPosition: {PhotoDataStore.Instance.PhotoPosition}");
        Debug.Log($"TextCardPosition: {PhotoDataStore.Instance.TextCardPosition}");
        Debug.Log($"photoObject null: {photoObject == null}");
        Debug.Log($"textCardObject null: {textCardObject == null}");

        DisplayPhoto();
        DisplayTextCard();
    }

    private void DisplayPhoto()
    {
        if (PhotoDataStore.Instance.LastPhoto == null) return;

        Texture2D originalPhoto = PhotoDataStore.Instance.LastPhoto;

        // Copy into a new readable texture
        Texture2D readablePhoto = new Texture2D(originalPhoto.width, originalPhoto.height, TextureFormat.RGB24, false);
        readablePhoto.SetPixels(originalPhoto.GetPixels());
        readablePhoto.Apply();

        GameObject photoObject = new GameObject("Photo");
        SpriteRenderer sr = photoObject.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(
            readablePhoto,
            new Rect(0, 0, readablePhoto.width, readablePhoto.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;

        ScaleToSize(photoObject, readablePhoto.width, readablePhoto.height, photoSize);

        photoObject.transform.position = new Vector3(
            PhotoDataStore.Instance.PhotoPosition.x,
            PhotoDataStore.Instance.PhotoPosition.y,
            0f
        );

        Debug.Log($"Photo created at {photoObject.transform.position}, size: {readablePhoto.width}x{readablePhoto.height}");
    }

private void DisplayTextCard()
{
    if (PhotoDataStore.Instance.TextCardSprite == null) return;

    Sprite textCard = PhotoDataStore.Instance.TextCardSprite;

    GameObject textCardObject = new GameObject("TextCard");
    SpriteRenderer sr = textCardObject.AddComponent<SpriteRenderer>();
    sr.sprite = textCard;
    sr.sortingLayerName = "Picture";
    sr.sortingOrder = 10;

    ScaleToSize(
        textCardObject,
        (int)textCard.rect.width,
        (int)textCard.rect.height,
        textCardSize
    );

    textCardObject.transform.position = new Vector3(
        PhotoDataStore.Instance.TextCardPosition.x,
        PhotoDataStore.Instance.TextCardPosition.y,
        0f
    );
}

    private Sprite Texture2DToSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private void ScaleToSize(GameObject obj, int texWidth, int texHeight, Vector2 targetSize)
    {
        if (texWidth == 0 || texHeight == 0) return;

        float nativeWidth  = texWidth  / 100f;
        float nativeHeight = texHeight / 100f;

        obj.transform.localScale = new Vector3(
            targetSize.x / nativeWidth,
            targetSize.y / nativeHeight,
            1f
        );
    }
}