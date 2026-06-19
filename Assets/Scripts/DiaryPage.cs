using UnityEngine;

public class DiaryPage : MonoBehaviour
{
    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _contentRenderer;
    private GameObject _contentObject;

    public void Initialize(DiaryEntry entry, bool isLeftPage,
        Vector2 landscapeSize, Vector2 portraitSize, Vector2 textCardSize)
    {
        // Background — first child
        Transform bg = transform.Find("Background");
        if (bg != null)
        {
            _backgroundRenderer = bg.GetComponent<SpriteRenderer>();
            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.sortingLayerName = "AboveUI";
            }
        }

        // Content — second child
        Transform content = transform.Find("Content");
        if (content != null)
        {
            _contentObject   = content.gameObject;
            _contentRenderer = content.GetComponent<SpriteRenderer>();
            if (_contentRenderer != null)
            {
                _contentRenderer.sortingLayerName = "AboveUI";

                if (isLeftPage)
                    SetupPhoto(entry, landscapeSize, portraitSize);
                else
                    SetupTextCard(entry, textCardSize);
            }
        }
    }

    private void SetupPhoto(DiaryEntry entry, Vector2 landscapeSize, Vector2 portraitSize)
    {
        if (entry.photo == null) return;

        Texture2D readable = new Texture2D(
            entry.photo.width, entry.photo.height, TextureFormat.RGB24, false
        );
        readable.SetPixels(entry.photo.GetPixels());
        readable.Apply();

        _contentRenderer.sprite = Sprite.Create(
            readable,
            new Rect(0, 0, readable.width, readable.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        Vector2 size = entry.isPortrait ? portraitSize : landscapeSize;
        ScaleToSize(_contentObject, readable.width, readable.height, size);
    }

    private void SetupTextCard(DiaryEntry entry, Vector2 textCardSize)
    {
        if (entry.textCard == null) return;

        _contentRenderer.sprite = entry.textCard;

        ScaleToSize(
            _contentObject,
            (int)entry.textCard.rect.width,
            (int)entry.textCard.rect.height,
            textCardSize
        );
    }

    public void SetSortingOrder(int order)
    {
        if (_backgroundRenderer != null)
        {
            _backgroundRenderer.sortingOrder = order;
        }
        if (_contentRenderer != null)
        {
            _contentRenderer.sortingOrder = order + 1;
        }
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