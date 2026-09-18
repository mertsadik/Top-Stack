using UnityEngine;
using UnityEngine.UI;

// Ana menu ve seviye secme ekrani.
// Arayuzun tamami burada kodla kuruluyor, sahnede hazir bir sey yok.
public class MainMenu : MonoBehaviour
{
    StackGame oyun;
    LevelSystem seviyeSistemi;
    ThemeSystem temaSistemi;

    Font font;
    Sprite panelSprite;
    Sprite yildizSprite;
    Sprite yildizBosSprite;

    // Seviye sayisi sinirsiz oldugu icin seviye ekrani sayfa sayfa geziliyor.
    const int SayfadakiSeviye = 8;

    GameObject anaPanel;
    GameObject seviyePanel;
    GameObject temaPanel;
    RectTransform temaListesi;
    Text yildizYazisi;
    RectTransform seviyeIzgarasi;   // seviye kartlarinin konuldugu alan
    Button oncekiButon;
    Button sonrakiButon;
    Text sayfaYazisi;
    int sayfa;

    Text baslikYazisi;
    Text rekorYazisi;
    Text sesYazisi;
    Text titresimYazisi;
    int sonRekor;

    float zaman;

    // Menu renkleri
    Color koyuYazi = new Color(0.16f, 0.19f, 0.32f);
    Color anaButonRengi = new Color(1f, 0.82f, 0.25f);
    Color ikinciButonRengi = new Color(0.95f, 0.96f, 1f);
    Color kilitliRenk = new Color(0.55f, 0.57f, 0.68f);

    public static MainMenu Olustur(Transform ebeveyn, StackGame sahip, LevelSystem sistem, ThemeSystem temalar)
    {
        GameObject go = new GameObject("Ana Menu");
        go.transform.SetParent(ebeveyn, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;       // oyun arayuzunun ustunde dursun

        CanvasScaler olcek = go.AddComponent<CanvasScaler>();
        olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        olcek.referenceResolution = new Vector2(1080f, 1920f);
        olcek.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        MainMenu menu = go.AddComponent<MainMenu>();
        menu.oyun = sahip;
        menu.seviyeSistemi = sistem;
        menu.temaSistemi = temalar;
        menu.Kur();
        return menu;
    }

    void Kur()
    {
        font = FontYukle("Fonts/Poppins-Bold");
        panelSprite = Resources.Load<Sprite>("UI/panel");
        yildizSprite = Resources.Load<Sprite>("UI/star");
        yildizBosSprite = Resources.Load<Sprite>("UI/star_bos");

        AnaPaneliKur();
        SeviyePaneliKur();
        TemaPaneliKur();
        seviyePanel.SetActive(false);
        temaPanel.SetActive(false);
    }

    void AnaPaneliKur()
    {
        anaPanel = PanelOlustur("Ana Panel");

        // arka plandaki kule gorunsun ama yazilar okunakli kalsin
        Image karartma = anaPanel.AddComponent<Image>();
        karartma.color = new Color(0.04f, 0.05f, 0.13f, 0.3f);

        baslikYazisi = YaziOlustur(anaPanel.transform, new Vector2(0f, 560f), new Vector2(1000f, 200f), 132);
        baslikYazisi.text = "TAP STACK";
        YaziyaCerceve(baslikYazisi, 6f);

        Text altBaslik = YaziOlustur(anaPanel.transform, new Vector2(0f, 430f), new Vector2(1000f, 80f), 44);
        altBaslik.text = "Doğru anda bırak, kuleyi yükselt";
        altBaslik.color = new Color(1f, 1f, 1f, 0.85f);
        YaziyaCerceve(altBaslik, 3f);

        ButonOlustur(anaPanel.transform, "OYNA", new Vector2(0f, 120f), new Vector2(560f, 170f), anaButonRengi, 72, koyuYazi, OynaBasildi);
        ButonOlustur(anaPanel.transform, "SEVİYELER", new Vector2(0f, -70f), new Vector2(560f, 130f), ikinciButonRengi, 52, koyuYazi, SeviyelerBasildi);
        ButonOlustur(anaPanel.transform, "TEMALAR", new Vector2(0f, -215f), new Vector2(560f, 130f), ikinciButonRengi, 52, koyuYazi, TemalarBasildi);

        // ses ve titresim yan yana iki kucuk anahtar
        Button sesButonu = ButonOlustur(anaPanel.transform, "", new Vector2(-145f, -360f), new Vector2(280f, 100f), ikinciButonRengi, 32, koyuYazi, SesBasildi);
        sesYazisi = sesButonu.GetComponentInChildren<Text>();

        Button titresimButonu = ButonOlustur(anaPanel.transform, "", new Vector2(145f, -360f), new Vector2(280f, 100f), ikinciButonRengi, 32, koyuYazi, TitresimBasildi);
        titresimYazisi = titresimButonu.GetComponentInChildren<Text>();

        // rekor yazisi ustte dursun, asagisi dekor kuleye kaliyor
        rekorYazisi = YaziOlustur(anaPanel.transform, new Vector2(0f, 320f), new Vector2(1000f, 80f), 46);
        YaziyaCerceve(rekorYazisi, 3f);
    }

    void SeviyePaneliKur()
    {
        seviyePanel = PanelOlustur("Seviye Panel");

        Image karartma = seviyePanel.AddComponent<Image>();
        karartma.color = new Color(0.04f, 0.05f, 0.13f, 0.7f);

        Text baslik = YaziOlustur(seviyePanel.transform, new Vector2(0f, 720f), new Vector2(1000f, 120f), 86);
        baslik.text = "Seviyeler";
        YaziyaCerceve(baslik, 5f);

        // kartlarin konulacagi bos alan
        GameObject izgara = new GameObject("Izgara", typeof(RectTransform));
        izgara.transform.SetParent(seviyePanel.transform, false);
        seviyeIzgarasi = (RectTransform)izgara.transform;
        seviyeIzgarasi.anchorMin = new Vector2(0.5f, 0.5f);
        seviyeIzgarasi.anchorMax = new Vector2(0.5f, 0.5f);
        seviyeIzgarasi.sizeDelta = new Vector2(1000f, 1100f);
        seviyeIzgarasi.anchoredPosition = new Vector2(0f, 100f);

        // sayfa gezinme satiri
        oncekiButon = ButonOlustur(seviyePanel.transform, "‹", new Vector2(-330f, -480f), new Vector2(200f, 120f), ikinciButonRengi, 56, koyuYazi, OncekiSayfa);
        sonrakiButon = ButonOlustur(seviyePanel.transform, "›", new Vector2(330f, -480f), new Vector2(200f, 120f), ikinciButonRengi, 56, koyuYazi, SonrakiSayfa);
        sayfaYazisi = YaziOlustur(seviyePanel.transform, new Vector2(0f, -480f), new Vector2(400f, 100f), 44);
        YaziyaCerceve(sayfaYazisi, 3f);

        ButonOlustur(seviyePanel.transform, "GERİ", new Vector2(0f, -720f), new Vector2(400f, 120f), ikinciButonRengi, 50, koyuYazi, GeriBasildi);
    }

    void TemaPaneliKur()
    {
        temaPanel = PanelOlustur("Tema Panel");

        Image karartma = temaPanel.AddComponent<Image>();
        karartma.color = new Color(0.04f, 0.05f, 0.13f, 0.7f);

        Text baslik = YaziOlustur(temaPanel.transform, new Vector2(0f, 680f), new Vector2(1000f, 120f), 86);
        baslik.text = "Temalar";
        YaziyaCerceve(baslik, 5f);

        yildizYazisi = YaziOlustur(temaPanel.transform, new Vector2(0f, 570f), new Vector2(1000f, 80f), 44);
        YaziyaCerceve(yildizYazisi, 3f);

        GameObject liste = new GameObject("Tema Listesi", typeof(RectTransform));
        liste.transform.SetParent(temaPanel.transform, false);
        temaListesi = (RectTransform)liste.transform;
        temaListesi.anchorMin = new Vector2(0.5f, 0.5f);
        temaListesi.anchorMax = new Vector2(0.5f, 0.5f);
        temaListesi.sizeDelta = new Vector2(1000f, 900f);
        temaListesi.anchoredPosition = Vector2.zero;

        ButonOlustur(temaPanel.transform, "GERİ", new Vector2(0f, -600f), new Vector2(400f, 120f), ikinciButonRengi, 50, koyuYazi, TemaGeriBasildi);
    }

    // Tema kartlari her acilista yeniden kuruluyor ki yildiz ve secim guncel olsun.
    void TemaListesiniKur()
    {
        for (int i = temaListesi.childCount - 1; i >= 0; i--)
        {
            Destroy(temaListesi.GetChild(i).gameObject);
        }

        int kullanilabilir = temaSistemi.KullanilabilirYildiz(seviyeSistemi);
        yildizYazisi.text = "Kullanılabilir yıldız: " + kullanilabilir;

        for (int i = 0; i < temaSistemi.temalar.Length; i++)
        {
            TemaKartiOlustur(i, new Vector2(0f, 340f - i * 170f));
        }
    }

    void TemaKartiOlustur(int index, Vector2 konum)
    {
        Theme tema = temaSistemi.TemaGetir(index);
        bool acik = temaSistemi.AcikMi(index);
        bool secili = temaSistemi.SeciliIndex() == index;
        int kullanilabilir = temaSistemi.KullanilabilirYildiz(seviyeSistemi);

        Color kartRengi = ikinciButonRengi;
        Color yaziRengi = koyuYazi;
        if (secili)
        {
            kartRengi = anaButonRengi;
        }
        else if (!acik)
        {
            kartRengi = kilitliRenk;
            yaziRengi = new Color(0.95f, 0.96f, 1f);
        }

        Button kart = ButonOlustur(temaListesi, "", konum, new Vector2(890f, 150f), kartRengi, 40, koyuYazi, null);
        Text bosYazi = kart.GetComponentInChildren<Text>();
        bosYazi.gameObject.SetActive(false);

        Text isim = YaziOlustur(kart.transform, new Vector2(-280f, 0f), new Vector2(300f, 80f), 44);
        isim.text = tema.isim;
        isim.color = yaziRengi;
        YaziGolgesiniKaldir(isim);

        // onizleme: temanin uc blok rengi
        Color ornekRenk = seviyeSistemi.SeviyeGetir(0).blokRengi;
        for (int i = 0; i < 3; i++)
        {
            Image kutu = ResimOlustur(kart.transform, new Vector2(10f + i * 72f, 0f), new Vector2(62f, 62f));
            kutu.sprite = panelSprite;
            kutu.type = Image.Type.Sliced;
            kutu.color = temaSistemi.BlokRengiUret(tema, ornekRenk, i * 4);
        }

        Text durum = YaziOlustur(kart.transform, new Vector2(320f, 0f), new Vector2(230f, 80f), 36);
        durum.color = yaziRengi;
        YaziGolgesiniKaldir(durum);

        int secilen = index;

        if (secili)
        {
            durum.text = "Seçili";
            kart.interactable = false;
        }
        else if (acik)
        {
            durum.text = "Seç";
            kart.onClick.AddListener(delegate { TemaSecildi(secilen); });
        }
        else
        {
            durum.text = tema.fiyat + " yıldız";
            if (kullanilabilir >= tema.fiyat)
            {
                durum.color = new Color(1f, 0.88f, 0.3f);
                kart.onClick.AddListener(delegate { TemaSatinAlindi(secilen); });
            }
            else
            {
                kart.interactable = false;
            }
        }
    }

    void TemalarBasildi()
    {
        anaPanel.SetActive(false);
        temaPanel.SetActive(true);
        TemaListesiniKur();
    }

    void TemaGeriBasildi()
    {
        temaPanel.SetActive(false);
        anaPanel.SetActive(true);
        RekorYazisiniTazele();
    }

    void TemaSecildi(int index)
    {
        temaSistemi.TemaSec(index);
        oyun.DekorKuleyiYenile();   // arkadaki kule yeni temayla gozuksun
        oyun.IlerlemeDegisti();     // buluta kaydedilsin
        TemaListesiniKur();
    }

    void TemaSatinAlindi(int index)
    {
        if (temaSistemi.SatinAl(index, seviyeSistemi))
        {
            oyun.DekorKuleyiYenile();
            oyun.IlerlemeDegisti();
        }
        TemaListesiniKur();
    }

    void OncekiSayfa()
    {
        sayfa = sayfa - 1;
        SeviyeIzgarasiniKur();
    }

    void SonrakiSayfa()
    {
        sayfa = sayfa + 1;
        SeviyeIzgarasiniKur();
    }

    // Seviye kartlari her acilista yeniden kuruluyor ki yildizlar ve
    // acilan seviyeler guncel olsun.
    void SeviyeIzgarasiniKur()
    {
        for (int i = seviyeIzgarasi.childCount - 1; i >= 0; i--)
        {
            Destroy(seviyeIzgarasi.GetChild(i).gameObject);
        }

        int acikSeviye = seviyeSistemi.AcikSeviye();
        int sonSayfa = acikSeviye / SayfadakiSeviye;

        // oyuncunun gelmedigi sayfalari gezmesine gerek yok
        if (sayfa > sonSayfa)
        {
            sayfa = sonSayfa;
        }
        if (sayfa < 0)
        {
            sayfa = 0;
        }

        int ilkSeviye = sayfa * SayfadakiSeviye;

        for (int i = 0; i < SayfadakiSeviye; i++)
        {
            int seviyeNo = ilkSeviye + i;

            int sutun = i % 2;
            int satir = i / 2;
            float x = -230f;
            if (sutun == 1)
            {
                x = 230f;
            }
            float y = 380f - satir * 250f;

            SeviyeKartiOlustur(seviyeNo, new Vector2(x, y), seviyeNo <= acikSeviye);
        }

        sayfaYazisi.text = (ilkSeviye + 1) + " - " + (ilkSeviye + SayfadakiSeviye);
        oncekiButon.interactable = sayfa > 0;
        sonrakiButon.interactable = sayfa < sonSayfa;
    }

    void SeviyeKartiOlustur(int index, Vector2 konum, bool acikMi)
    {
        Level seviye = seviyeSistemi.SeviyeGetir(index);

        // kilitli kartlar koyu gri, uzerindeki yazilar beyaz olsun ki okunsun
        Color kartRengi = ikinciButonRengi;
        Color numaraRengi = koyuYazi;
        Color isimRengi = new Color(0.4f, 0.43f, 0.55f);

        if (!acikMi)
        {
            kartRengi = kilitliRenk;
            numaraRengi = new Color(0.95f, 0.96f, 1f);
            isimRengi = new Color(0.9f, 0.91f, 0.97f);
        }

        Button kart = ButonOlustur(seviyeIzgarasi, "", konum, new Vector2(430f, 220f), kartRengi, 40, koyuYazi, null);
        kart.interactable = acikMi;

        // butonun kendi yazisini kullanmiyoruz, icine ayri yazilar koyuyoruz
        Text bosYazi = kart.GetComponentInChildren<Text>();
        bosYazi.gameObject.SetActive(false);

        Text numara = YaziOlustur(kart.transform, new Vector2(0f, 55f), new Vector2(400f, 90f), 64);
        numara.text = "" + (index + 1);
        numara.color = numaraRengi;
        YaziGolgesiniKaldir(numara);

        Text isim = YaziOlustur(kart.transform, new Vector2(0f, -5f), new Vector2(400f, 60f), 34);
        isim.text = seviye.isim;
        isim.color = isimRengi;
        YaziGolgesiniKaldir(isim);

        if (acikMi)
        {
            int kazanilan = seviyeSistemi.YildizGetir(index);
            for (int y = 0; y < 3; y++)
            {
                Image yildiz = ResimOlustur(kart.transform, new Vector2((y - 1) * 62f, -70f), new Vector2(56f, 54f));
                if (y < kazanilan)
                {
                    yildiz.sprite = yildizSprite;
                    yildiz.color = Color.white;
                }
                else
                {
                    yildiz.sprite = yildizBosSprite;
                    yildiz.color = new Color(0.58f, 0.61f, 0.72f);
                }
            }

            int secilen = index;
            kart.onClick.AddListener(delegate { SeviyeSecildi(secilen); });
        }
        else
        {
            Text kilit = YaziOlustur(kart.transform, new Vector2(0f, -70f), new Vector2(400f, 60f), 32);
            kilit.text = "Kilitli";
            kilit.color = new Color(0.92f, 0.93f, 0.98f);
            YaziGolgesiniKaldir(kilit);
        }
    }

    void Update()
    {
        // baslik hafifce inip kalksin
        if (baslikYazisi != null && anaPanel.activeSelf)
        {
            zaman = zaman + Time.deltaTime;
            float kayma = Mathf.Sin(zaman * 1.6f) * 10f;
            baslikYazisi.rectTransform.anchoredPosition = new Vector2(0f, 560f + kayma);
        }
    }

    // --- dugme islemleri ---

    void OynaBasildi()
    {
        oyun.OyunuBaslat(seviyeSistemi.AcikSeviye());
    }

    void SeviyelerBasildi()
    {
        anaPanel.SetActive(false);
        seviyePanel.SetActive(true);
        sayfa = seviyeSistemi.AcikSeviye() / SayfadakiSeviye;   // oyuncunun kaldigi sayfa
        SeviyeIzgarasiniKur();
    }

    void GeriBasildi()
    {
        seviyePanel.SetActive(false);
        anaPanel.SetActive(true);
    }

    void SeviyeSecildi(int index)
    {
        oyun.OyunuBaslat(index);
    }

    void SesBasildi()
    {
        oyun.SesiAcKapat();
        AyarYazilariniTazele();
    }

    void TitresimBasildi()
    {
        Haptics.AcKapat();
        Haptics.Hafif();        // titresim acildiysa ornek bir titresim ver (kapatilinca titremez)
        AyarYazilariniTazele();
    }

    void AyarYazilariniTazele()
    {
        if (oyun.SesAcikMi())
        {
            sesYazisi.text = "Ses: Açık";
        }
        else
        {
            sesYazisi.text = "Ses: Kapalı";
        }

        if (Haptics.AcikMi())
        {
            titresimYazisi.text = "Titreşim: Açık";
        }
        else
        {
            titresimYazisi.text = "Titreşim: Kapalı";
        }
    }

    // --- oyun tarafindan cagriliyor ---

    public void Goster(int rekor)
    {
        gameObject.SetActive(true);
        anaPanel.SetActive(true);
        seviyePanel.SetActive(false);
        temaPanel.SetActive(false);

        sonRekor = rekor;
        RekorYazisiniTazele();
        AyarYazilariniTazele();
    }

    void RekorYazisiniTazele()
    {
        int yildiz = temaSistemi.KullanilabilirYildiz(seviyeSistemi);

        if (sonRekor > 0)
        {
            rekorYazisi.text = "Rekor: " + sonRekor + " blok   ·   Yıldız: " + yildiz;
        }
        else if (yildiz > 0)
        {
            rekorYazisi.text = "Yıldız: " + yildiz;
        }
        else
        {
            rekorYazisi.text = "İlk kuleni dik!";
        }
    }

    // Bulut kaydi gelip ilerleme degisince menudeki yazilar ve acik ekran tazelensin.
    public void VerilerDegisti(int rekor)
    {
        sonRekor = rekor;
        RekorYazisiniTazele();

        if (seviyePanel.activeSelf)
        {
            SeviyeIzgarasiniKur();
        }
        if (temaPanel.activeSelf)
        {
            TemaListesiniKur();
        }
    }

    // Geri tusuna basilinca StackGame cagiriyor. Seviyeler ya da Temalar ekranindaysak
    // ana menuye doner ve true verir; zaten ana menudeysek false verir (oyun kapanir).
    public bool GeriTusu()
    {
        if (seviyePanel.activeSelf)
        {
            GeriBasildi();
            return true;
        }
        if (temaPanel.activeSelf)
        {
            TemaGeriBasildi();
            return true;
        }
        return false;
    }

    public void Gizle()
    {
        gameObject.SetActive(false);
    }

    // --- kucuk yardimcilar ---

    GameObject PanelOlustur(string ad)
    {
        GameObject go = new GameObject(ad, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    Button ButonOlustur(Transform ebeveyn, string yazi, Vector2 konum, Vector2 boyut, Color renk, int punto, Color yaziRengi, System.Action tiklama)
    {
        GameObject go = new GameObject("Buton", typeof(RectTransform));
        go.transform.SetParent(ebeveyn, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = boyut;
        rect.anchoredPosition = konum;

        Image resim = go.AddComponent<Image>();
        resim.sprite = panelSprite;
        resim.type = Image.Type.Sliced;
        resim.color = renk;

        Button buton = go.AddComponent<Button>();
        buton.targetGraphic = resim;

        // basilinca hafif koyulassin
        ColorBlock renkler = buton.colors;
        renkler.normalColor = Color.white;
        renkler.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
        renkler.pressedColor = new Color(0.82f, 0.82f, 0.82f);
        renkler.disabledColor = new Color(0.85f, 0.85f, 0.85f);
        buton.colors = renkler;

        Text etiket = YaziOlustur(go.transform, Vector2.zero, new Vector2(boyut.x - 40f, boyut.y), punto);
        etiket.text = yazi;
        etiket.color = yaziRengi;
        YaziGolgesiniKaldir(etiket);

        if (tiklama != null)
        {
            buton.onClick.AddListener(delegate { tiklama(); });
        }

        return buton;
    }

    Text YaziOlustur(Transform ebeveyn, Vector2 konum, Vector2 boyut, int punto)
    {
        GameObject go = new GameObject("Yazi", typeof(RectTransform));
        go.transform.SetParent(ebeveyn, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
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

    Image ResimOlustur(Transform ebeveyn, Vector2 konum, Vector2 boyut)
    {
        GameObject go = new GameObject("Resim", typeof(RectTransform));
        go.transform.SetParent(ebeveyn, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = boyut;
        rect.anchoredPosition = konum;

        Image resim = go.AddComponent<Image>();
        resim.raycastTarget = false;
        return resim;
    }

    void YaziyaCerceve(Text yazi, float kalinlik)
    {
        Outline cerceve = yazi.gameObject.AddComponent<Outline>();
        cerceve.effectColor = new Color(0.11f, 0.13f, 0.24f, 0.85f);
        cerceve.effectDistance = new Vector2(kalinlik, -kalinlik);
    }

    void YaziGolgesiniKaldir(Text yazi)
    {
        Shadow golge = yazi.GetComponent<Shadow>();
        if (golge != null)
        {
            Destroy(golge);
        }
    }

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
