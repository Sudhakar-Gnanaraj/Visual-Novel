using UnityEngine;
using UnityEngine.UI;

public class TutorialPhotoDisplay : MonoBehaviour
{
    [Header("Display References")]
    [SerializeField] private RawImage _landscapeDisplay;
    [SerializeField] private RawImage _portraitDisplay;

    [Header("Positions")]
    [SerializeField] private Vector2 landscapePosition = new Vector2(100f, 100f);
    [SerializeField] private Vector2 portraitPosition  = new Vector2(100f, 100f);

    private void Start()
    {
        _landscapeDisplay.gameObject.SetActive(false);
        _portraitDisplay.gameObject.SetActive(false);
    }

    public void ShowPhoto(Texture2D photo, bool isPortrait)
    {
        // Hide both first
        _landscapeDisplay.gameObject.SetActive(false);
        _portraitDisplay.gameObject.SetActive(false);

        if (photo == null) return;

        // Copy into readable texture
        Texture2D readable = new Texture2D(
            photo.width, photo.height, TextureFormat.RGB24, false
        );
        readable.SetPixels(photo.GetPixels());
        readable.Apply();

        if (isPortrait)
        {
            _portraitDisplay.texture = readable;
            _portraitDisplay.gameObject.SetActive(true);
            _portraitDisplay.rectTransform.anchoredPosition = portraitPosition;
        }
        else
        {
            _landscapeDisplay.texture = readable;
            _landscapeDisplay.gameObject.SetActive(true);
            _landscapeDisplay.rectTransform.anchoredPosition = landscapePosition;
        }
    }
}