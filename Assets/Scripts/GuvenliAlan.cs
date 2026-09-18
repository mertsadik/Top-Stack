using UnityEngine;

// Ekranin centik / kamera deligi ve alttaki hareket cubugu disinda kalan "guvenli" bolgesi.
//
// Projede "Render Outside Safe Area" acik: oyun butun ekrana ciziliyor (arka plan
// kenardan kenara guzel duruyor) ama ust kosedeki dugmeler ve yazilar centigin
// altina kacabiliyor. Bu bilesen kendi RectTransform'unu Screen.safeArea'ya
// oturtuyor; icine konan arayuz guvende kaliyor.
public class GuvenliAlan : MonoBehaviour
{
    RectTransform rect;
    Rect sonAlan;
    int sonGenislik;
    int sonYukseklik;

    // Canvas'in altina tam ekran bir guvenli alan objesi olusturur.
    public static RectTransform Olustur(Transform canvas, string ad)
    {
        GameObject go = new GameObject(ad, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        go.AddComponent<GuvenliAlan>();
        return (RectTransform)go.transform;
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        EkranaGoreUygula();
    }

    void Update()
    {
        // cozunurluk ya da guvenli alan degisirse (ornegin ekran bolunurse) yeniden hesapla
        if (Screen.safeArea != sonAlan || Screen.width != sonGenislik || Screen.height != sonYukseklik)
        {
            EkranaGoreUygula();
        }
    }

    void EkranaGoreUygula()
    {
        sonAlan = Screen.safeArea;
        sonGenislik = Screen.width;
        sonYukseklik = Screen.height;
        AlanUygula(sonAlan, sonGenislik, sonYukseklik);
    }

    // Guvenli alani (piksel) 0-1 arasi capalara cevirir.
    public void AlanUygula(Rect alan, float ekranGenislik, float ekranYukseklik)
    {
        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
        }
        if (ekranGenislik <= 0f || ekranYukseklik <= 0f)
        {
            return;
        }

        Vector2 altSol = new Vector2(alan.xMin / ekranGenislik, alan.yMin / ekranYukseklik);
        Vector2 ustSag = new Vector2(alan.xMax / ekranGenislik, alan.yMax / ekranYukseklik);

        rect.anchorMin = altSol;
        rect.anchorMax = ustSag;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
