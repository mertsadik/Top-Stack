using UnityEngine;
using UnityEngine.UI;

// Oyun ici arayuz. Sahnede hazir bir Canvas yok, hepsi burada kodla kuruluyor.
// Blok birakmak icin ekranin herhangi bir yerine dokunmak yeterli; sadece "Menü"
// ve "Reklam izle, devam et" icin gercek dugme var (EventSystem'i StackGame kuruyor).
public class GameUI : MonoBehaviour
{
    Font buyukFont;     // basliklar ve skor
    Font kucukFont;     // kucuk yazilar

    Sprite panelSprite;
    Sprite yildizSprite;
    Sprite yildizBosSprite;

    Image seviyeHapi;
    Text seviyeYazisi;
    Text sayacYazisi;
    Text rekorYazisi;
    Text comboYazisi;
    Text ipucuYazisi;

    Image cubukArkasi;
    RectTransform cubukDolgusu;
    Image cubukDolguResmi;
    float cubukGenisligi = 620f;

    GameObject panel;
    Image panelKarti;
    Text panelBaslik;
    Text panelAlt;
    Text panelDevam;
    Image[] yildizlar;

    Color vurguRengi = Color.white;

    float comboSayaci;
    float yanipSonme;
    float sayacVurgusu;     // skor degisince kisa buyume animasyonu

    StackGame oyun;
    Button devamDugmesi;

    // Ekran kenarina yapisik arayuz (ust bilgi, ipucu, Menü dugmesi) bu alanlara konuyor;
    // centikli telefonlarda centigin altina kacmiyor.
    RectTransform hudAlani;
    RectTransform menuDugmeAlani;

    // Canvas'i ve icindeki her seyi olusturur.
    public static GameUI Olustur(Transform ebeveyn, StackGame sahip)
    {
        GameObject go = new GameObject("Arayuz");
        go.transform.SetParent(ebeveyn, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler olcek = go.AddComponent<CanvasScaler>();
        olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        olcek.referenceResolution = new Vector2(1080f, 1920f);
        olcek.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();    // menu dugmesi tiklanabilsin

        GameUI arayuz = go.AddComponent<GameUI>();
        arayuz.oyun = sahip;
        arayuz.Kur();
        return arayuz;
    }

    public void Goster()
    {
        gameObject.SetActive(true);
    }

    public void Gizle()
    {
        gameObject.SetActive(false);
    }

    void Kur()
    {
        // Poppins-Bold hem kalin hem de Turkce karakterleri tam,
        // buyuk ve kucuk yazilarda ayni fontu kullaniyoruz.
        buyukFont = FontYukle("Fonts/Poppins-Bold");
        kucukFont = buyukFont;

        panelSprite = Resources.Load<Sprite>("UI/panel");
        yildizSprite = Resources.Load<Sprite>("UI/star");
        yildizBosSprite = Resources.Load<Sprite>("UI/star_bos");

        hudAlani = GuvenliAlan.Olustur(transform, "Guvenli Alan (HUD)");

        // ust kisimdaki seviye adi hapi
        seviyeHapi = KutuOlustur(hudAlani, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(620f, 96f), new Color(1f, 1f, 1f, 0.9f));
        seviyeHapi.sprite = panelSprite;
        seviyeHapi.type = Image.Type.Sliced;

        seviyeYazisi = YaziOlustur(seviyeHapi.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(600f, 70f), 46, kucukFont);
        seviyeYazisi.color = new Color(0.16f, 0.19f, 0.32f);
        YaziGolgesiniKaldir(seviyeYazisi);

        sayacYazisi = YaziOlustur(hudAlani, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(1000f, 190f), 140, buyukFont);
        YaziyaCerceve(sayacYazisi, 4f);

        // ilerleme cubugu
        cubukArkasi = KutuOlustur(hudAlani, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(cubukGenisligi, 30f), new Color(1f, 1f, 1f, 0.25f));
        cubukArkasi.sprite = panelSprite;
        cubukArkasi.type = Image.Type.Sliced;

        GameObject dolgu = new GameObject("Dolgu", typeof(RectTransform));
        dolgu.transform.SetParent(cubukArkasi.transform, false);
        cubukDolgusu = (RectTransform)dolgu.transform;
        cubukDolgusu.anchorMin = new Vector2(0f, 0.5f);
        cubukDolgusu.anchorMax = new Vector2(0f, 0.5f);
        cubukDolgusu.pivot = new Vector2(0f, 0.5f);
        cubukDolgusu.anchoredPosition = new Vector2(4f, 0f);
        cubukDolgusu.sizeDelta = new Vector2(0f, 22f);
        cubukDolguResmi = dolgu.AddComponent<Image>();
        cubukDolguResmi.sprite = panelSprite;
        cubukDolguResmi.type = Image.Type.Sliced;
        cubukDolguResmi.color = Color.white;
        cubukDolguResmi.raycastTarget = false;

        rekorYazisi = YaziOlustur(hudAlani, new Vector2(0.5f, 1f), new Vector2(0f, -460f), new Vector2(1000f, 60f), 38, kucukFont);
        rekorYazisi.color = new Color(1f, 1f, 1f, 0.85f);

        comboYazisi = YaziOlustur(transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1000f, 120f), 76, buyukFont);
        comboYazisi.text = "";
        YaziyaCerceve(comboYazisi, 3f);

        ipucuYazisi = YaziOlustur(hudAlani, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1000f, 80f), 44, kucukFont);
        ipucuYazisi.text = "Bloğu bırakmak için ekrana dokun";
        YaziyaCerceve(ipucuYazisi, 4f);

        PanelKur();
        panel.SetActive(false);

        // Menu dugmesi en son kuruluyor ki panel acikken bile ustte kalsin.
        menuDugmeAlani = GuvenliAlan.Olustur(transform, "Guvenli Alan (Menu Dugmesi)");
        MenuDugmesiKur();
    }

    // Sol ust kosedeki kucuk "Menü" dugmesi.
    void MenuDugmesiKur()
    {
        GameObject go = new GameObject("Menu Dugmesi", typeof(RectTransform));
        go.transform.SetParent(menuDugmeAlani, false);

        // Kare ve dar: uzun telefonlarda (20:9 ve ustu) ekran daraliyor, genis bir
        // "Menü" yazili dugme ortadaki seviye hapinin ustune biniyordu.
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(96f, 96f);
        rect.anchoredPosition = new Vector2(88f, -110f);

        Image resim = go.AddComponent<Image>();
        resim.sprite = panelSprite;
        resim.type = Image.Type.Sliced;
        resim.color = new Color(1f, 1f, 1f, 0.9f);

        Button dugme = go.AddComponent<Button>();
        dugme.targetGraphic = resim;

        // yazi yerine uc cizgili menu simgesi
        for (int i = 0; i < 3; i++)
        {
            KutuOlustur(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f - i * 16f), new Vector2(46f, 8f), new Color(0.16f, 0.19f, 0.32f));
        }

        dugme.onClick.AddListener(MenuyeDon);
    }

    void MenuyeDon()
    {
        oyun.AnaMenuyuAc();
    }

    void PanelKur()
    {
        panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image karartma = panel.AddComponent<Image>();
        karartma.color = new Color(0.04f, 0.05f, 0.13f, 0.6f);
        karartma.raycastTarget = false;

        // ortadaki kart
        panelKarti = KutuOlustur(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(820f, 540f), Color.white);
        panelKarti.sprite = panelSprite;
        panelKarti.type = Image.Type.Sliced;

        panelBaslik = YaziOlustur(panelKarti.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(760f, 140f), 74, buyukFont);
        panelBaslik.color = new Color(0.16f, 0.19f, 0.32f);
        YaziGolgesiniKaldir(panelBaslik);

        panelAlt = YaziOlustur(panelKarti.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(760f, 120f), 42, kucukFont);
        panelAlt.color = new Color(0.4f, 0.43f, 0.55f);
        YaziGolgesiniKaldir(panelAlt);

        panelDevam = YaziOlustur(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(900f, 80f), 44, kucukFont);
        YaziyaCerceve(panelDevam, 4f);

        DevamDugmesiKur();

        // uc yildiz
        yildizlar = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 185f;
            float y = 160f;
            if (i == 1)
            {
                y = 205f;   // ortadaki yildiz biraz yukarida dursun
            }

            Image yildiz = KutuOlustur(panelKarti.transform, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(170f, 160f), Color.white);
            yildiz.sprite = yildizBosSprite;
            yildiz.type = Image.Type.Simple;
            yildizlar[i] = yildiz;
        }
    }

    void Update()
    {
        // combo yazisi kisa sure gorunup kayboluyor
        if (comboSayaci > 0f)
        {
            comboSayaci = comboSayaci - Time.deltaTime;
            float saydamlik = comboSayaci / 0.9f;
            if (saydamlik > 1f)
            {
                saydamlik = 1f;
            }
            if (saydamlik < 0f)
            {
                saydamlik = 0f;
            }
            comboYazisi.color = new Color(1f, 1f, 1f, saydamlik);
            float buyume = 1f + saydamlik * 0.15f;
            comboYazisi.transform.localScale = new Vector3(buyume, buyume, 1f);
        }

        // skor degisince kisa bir zipla
        if (sayacVurgusu > 0f)
        {
            sayacVurgusu = sayacVurgusu - Time.deltaTime * 4f;
            if (sayacVurgusu < 0f)
            {
                sayacVurgusu = 0f;
            }
            float buyume = 1f + sayacVurgusu * 0.18f;
            sayacYazisi.transform.localScale = new Vector3(buyume, buyume, 1f);
        }

        // panel acikken "dokun" yazisi yanip sonsun
        if (panel.activeSelf)
        {
            yanipSonme = yanipSonme + Time.deltaTime * 2.5f;
            float a = 0.55f + Mathf.Sin(yanipSonme) * 0.35f;
            panelDevam.color = new Color(1f, 1f, 1f, a);
        }
    }

    // --- oyun tarafindan cagrilan metodlar ---

    public void SeviyeyiGoster(string seviyeAdi, int seviyeNo, int hedef, int rekor, Color vurgu)
    {
        vurguRengi = vurgu;
        cubukDolguResmi.color = vurgu;

        panel.SetActive(false);
        devamDugmesi.gameObject.SetActive(false);
        seviyeYazisi.text = "Seviye " + seviyeNo + " · " + seviyeAdi;
        ipucuYazisi.gameObject.SetActive(true);
        comboYazisi.text = "";
        SayaciGuncelle(0, hedef);
        RekorGoster(rekor);
    }

    public void SayaciGuncelle(int yerlesen, int hedef)
    {
        sayacYazisi.text = yerlesen + " / " + hedef;

        float oran = (float)yerlesen / hedef;
        if (oran > 1f)
        {
            oran = 1f;
        }
        cubukDolgusu.sizeDelta = new Vector2((cubukGenisligi - 8f) * oran, 22f);

        sayacVurgusu = 1f;
    }

    public void RekorGoster(int rekor)
    {
        if (rekor > 0)
        {
            rekorYazisi.text = "Rekor: " + rekor + " blok";
        }
        else
        {
            rekorYazisi.text = "";
        }
    }

    public void MukemmelGoster(int combo)
    {
        if (combo > 1)
        {
            comboYazisi.text = "MÜKEMMEL x" + combo;
        }
        else
        {
            comboYazisi.text = "MÜKEMMEL";
        }
        comboSayaci = 0.9f;
    }

    public void IpucuGizle()
    {
        ipucuYazisi.gameObject.SetActive(false);
    }

    public void SeviyeTamamlandiPaneli(int seviyeNo, int yildiz, int mukemmelSayisi)
    {
        panel.SetActive(true);
        panelKarti.color = Color.white;
        panelBaslik.text = "Seviye " + seviyeNo + " Tamam!";
        panelAlt.text = mukemmelSayisi + " mükemmel yerleştirme";
        devamDugmesi.gameObject.SetActive(false);
        panelDevam.text = "Devam etmek için dokun";

        YildizlariGoster(yildiz);
    }

    // Kule yikilinca cikan "Reklam izle, devam et" dugmesi.
    void DevamDugmesiKur()
    {
        GameObject go = new GameObject("Devam Dugmesi", typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(640f, 140f);
        rect.anchoredPosition = new Vector2(0f, -430f);

        Image resim = go.AddComponent<Image>();
        resim.sprite = panelSprite;
        resim.type = Image.Type.Sliced;
        resim.color = new Color(1f, 0.82f, 0.25f);

        devamDugmesi = go.AddComponent<Button>();
        devamDugmesi.targetGraphic = resim;

        Text etiket = YaziOlustur(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(600f, 100f), 46, kucukFont);
        etiket.text = "Reklam izle, devam et";
        etiket.color = new Color(0.16f, 0.19f, 0.32f);
        YaziGolgesiniKaldir(etiket);

        devamDugmesi.onClick.AddListener(DevamaBasildi);
        go.SetActive(false);
    }

    void DevamaBasildi()
    {
        devamDugmesi.gameObject.SetActive(false);   // iki kez basilmasin
        oyun.ReklamlaDevamEt();
    }

    public void DevamDugmesiniGoster(bool gorunsun)
    {
        devamDugmesi.gameObject.SetActive(gorunsun);
    }

    // Reklamla devam edilince paneli kapatir.
    public void PaneliKapat()
    {
        panel.SetActive(false);
        devamDugmesi.gameObject.SetActive(false);
    }

    public void BasarisizPaneli(int yerlesen, int hedef, int rekor)
    {
        panel.SetActive(true);
        panelKarti.color = Color.white;
        panelBaslik.text = "Kule Yıkıldı";

        panelAlt.text = yerlesen + " / " + hedef + " blok koydun";
        panelDevam.text = "Tekrar denemek için dokun";
        YildizlariGoster(0);
    }

    void YildizlariGoster(int yildiz)
    {
        for (int i = 0; i < yildizlar.Length; i++)
        {
            if (i < yildiz)
            {
                yildizlar[i].sprite = yildizSprite;
                yildizlar[i].color = Color.white;
            }
            else
            {
                yildizlar[i].sprite = yildizBosSprite;
                yildizlar[i].color = new Color(1f, 1f, 1f, 0.55f);
            }
        }
    }

    // --- kucuk yardimcilar ---

    Text YaziOlustur(Transform ebeveyn, Vector2 ankor, Vector2 konum, Vector2 boyut, int punto, Font font)
    {
        GameObject go = new GameObject("Yazi", typeof(RectTransform));
        go.transform.SetParent(ebeveyn, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = ankor;
        rect.anchorMax = ankor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = boyut;
        rect.anchoredPosition = konum;

        Text yazi = go.AddComponent<Text>();
        yazi.font = font;
        yazi.fontSize = punto;
        yazi.alignment = TextAnchor.MiddleCenter;
        yazi.color = Color.white;
        yazi.raycastTarget = false;
        yazi.horizontalOverflow = HorizontalWrapMode.Overflow;
        yazi.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow golge = go.AddComponent<Shadow>();
        golge.effectColor = new Color(0f, 0f, 0f, 0.35f);
        golge.effectDistance = new Vector2(3f, -3f);

        return yazi;
    }

    // Renkli arka planlarin uzerinde okunakli olmasi icin yaziya koyu cerceve.
    void YaziyaCerceve(Text yazi, float kalinlik)
    {
        Outline cerceve = yazi.gameObject.AddComponent<Outline>();
        cerceve.effectColor = new Color(0.11f, 0.13f, 0.24f, 0.85f);
        cerceve.effectDistance = new Vector2(kalinlik, -kalinlik);
    }

    // Acik renkli kartlarin uzerindeki koyu yazilarda golge iyi durmuyor.
    void YaziGolgesiniKaldir(Text yazi)
    {
        Shadow golge = yazi.GetComponent<Shadow>();
        if (golge != null)
        {
            Destroy(golge);
        }
    }

    Image KutuOlustur(Transform ebeveyn, Vector2 ankor, Vector2 konum, Vector2 boyut, Color renk)
    {
        GameObject go = new GameObject("Kutu", typeof(RectTransform));
        go.transform.SetParent(ebeveyn, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = ankor;
        rect.anchorMax = ankor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = boyut;
        rect.anchoredPosition = konum;

        Image resim = go.AddComponent<Image>();
        resim.color = renk;
        resim.raycastTarget = false;
        return resim;
    }

    // Once projedeki fontu dener, bulamazsa Unity'nin hazir fontuna duser.
    Font FontYukle(string yol)
    {
        Font bulunan = Resources.Load<Font>(yol);
        if (bulunan != null)
        {
            return bulunan;
        }

        bulunan = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (bulunan != null)
        {
            return bulunan;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 32);
    }
}
