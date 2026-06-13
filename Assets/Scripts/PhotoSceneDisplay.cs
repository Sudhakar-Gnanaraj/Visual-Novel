using UnityEngine;

public class PhotoSceneDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject photoObject;
    [SerializeField] private GameObject textCardObject;

    [Header("Display Settings")]
    [SerializeField] private Vector2 landscapePhotoSize = new Vector2(3f, 2f);
    [SerializeField] private Vector2 portraitPhotoSize  = new Vector2(2f, 3f);
    [SerializeField] private Vector2 textCardSize       = new Vector2(3f, 2f);

    private void Start()
    {
        if (PhotoDataStore.Instance == null)
        {
            Debug.LogError("PhotoDataStore.Instance is NULL.");
            return;
        }

        DisplayPhoto();
        DisplayTextCard();
    }

    private void DisplayPhoto()
    {
        if (PhotoDataStore.Instance.LastPhoto == null || photoObject == null) return;

        Texture2D originalPhoto = PhotoDataStore.Instance.LastPhoto;
        bool isPortrait         = PhotoDataStore.Instance.IsPortrait;
        Vector2 photoSize       = isPortrait ? portraitPhotoSize : landscapePhotoSize;

        // Copy into a new readable texture
        Texture2D readablePhoto = new Texture2D(
            originalPhoto.width,
            originalPhoto.height,
            TextureFormat.RGB24,
            false
        );
        readablePhoto.SetPixels(originalPhoto.GetPixels());
        readablePhoto.Apply();

        SpriteRenderer sr = photoObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = photoObject.AddComponent<SpriteRenderer>();

        sr.sprite           = Sprite.Create(
            readablePhoto,
            new Rect(0, 0, readablePhoto.width, readablePhoto.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        sr.sortingLayerName = "AboveUI";
        sr.sortingOrder     = 10;

        ScaleToSize(photoObject, readablePhoto.width, readablePhoto.height, photoSize);

        photoObject.transform.position = new Vector3(
            PhotoDataStore.Instance.PhotoPosition.x,
            PhotoDataStore.Instance.PhotoPosition.y,
            0f
        );

        Debug.Log($"Photo displayed — size: {readablePhoto.width}x{readablePhoto.height}, " +
                $"position: {photoObject.transform.position}, " +
                $"scale: {photoObject.transform.localScale}");
    }

    private void DisplayTextCard()
    {
        if (PhotoDataStore.Instance.TextCardSprite == null || textCardObject == null) return;

        SpriteRenderer sr = textCardObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = textCardObject.AddComponent<SpriteRenderer>();

        sr.sprite = PhotoDataStore.Instance.TextCardSprite;

        ScaleToSize(
            textCardObject,
            (int)PhotoDataStore.Instance.TextCardSprite.rect.width,
            (int)PhotoDataStore.Instance.TextCardSprite.rect.height,
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