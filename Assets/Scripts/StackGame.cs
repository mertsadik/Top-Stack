using System.Collections;
using UnityEngine;

// Oyunun ana kodu.
// Sahnede sadece bu scriptin oldugu tek bir obje var; kamera, isik, arka plan
// ve arayuz oyun baslarken burada kodla olusturuluyor.
public class StackGame : MonoBehaviour
{
    [Header("Blok ayarlari")]
    public float blokBoyu = 3f;                 // ilk blogun en/derinlik olcusu
    public float blokYuksekligi = 0.9f;
    public float hareketMesafesi = 3.3f;        // blok saga sola ne kadar aciliyor
    public float mukemmelTolerans = 0.14f;      // en dusuk "mukemmel" payi
    public float toleransHizCarpani = 0.045f;   // hiz arttikca pay da buyusun
    public float geriBuyume = 0.12f;            // mukemmelde blok ne kadar geri buyur
    public float enYuksekHiz = 9f;

    [Header("Kamera ayarlari")]
    public float kameraAcisi = 24f;
    public float kameraFov = 40f;
    public float kameraTakipHizi = 6f;

    [Header("Diger")]
    public float yenidenBaslamaGecikmesi = 0.6f;
    public float ilkDokunusBeklemesi = 0.25f;   // seviye baslayinca bu sure icindeki dokunuslar blok birakmaz

    // --- oyun sirasinda kullanilan degiskenler ---
    Camera anaKamera;
    GameUI arayuz;
    SoundManager sesler;
    LevelSystem seviyeSistemi;
    ThemeSystem temaSistemi;
    MainMenu anaMenu;
    AdManager reklamlar;
    CloudSave bulutKayit;

    bool devamHakkiKullanildi;      // her denemede bir kez reklamla devam edilebilir

    // Seviye basinda secili olan tema. Seviye oynanirken tema degisse bile
    // (ornegin buluttan baska cihazin secimi gelirse) kule yarida renk degistirmesin.
    Theme aktifTema;

    bool menudeyiz;                 // ana menu acikken oynanis calismiyor
    float menuZamani;
    float menuKameraYuksekligi;

    Transform kuleKok;              // butun bloklarin parenti, seviye basinda silinir
    GameObject hareketliBlok;

    Vector3 sonMerkez;              // en ustteki yerlesmis blogun merkezi
    float sonGenislikX;
    float sonGenislikZ;

    int eksen;                      // 0 = X ekseninde hareket, 1 = Z ekseninde
    float kayma;                    // hareketli blogun alttaki bloktan farki
    int yon = 1;
    float hiz;

    int seviyeIndex;
    Level aktifSeviye;
    Color seviyeRengi;
    float seviyeBlokBoyu;           // bu seviyedeki ilk blogun genisligi

    int yerlesenBlok;
    int mukemmelSayisi;
    int combo;
    int rekor;

    bool oynaniyor;
    float oyunBaslamaZamani;        // seviye (ya da reklamla devam) ne zaman basladi
    bool seviyeBitti;               // panel "seviye tamamlandi" mi yoksa "kule yikildi" mi
    float bitisZamani;

    Vector3 kameraOfseti;
    float kameraHedefY;
    float sarsinti;
    int oncekiEkranGenisligi;
    int oncekiEkranYuksekligi;

    // arka plan
    Transform gradyanKaresi;
    Transform desenKaresi;
    Material gradyanMateryali;
    Material desenMateryali;
    Texture2D gradyanDokusu;
    Texture2D desenDokusu;
    float arkaPlanMesafesi = 150f;

    // parcaciklar
    ParticleSystem yildizParcacik;
    ParticleSystem konfetiParcacik;

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (Application.isMobilePlatform)
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }

        seviyeSistemi = GetComponent<LevelSystem>();
        if (seviyeSistemi == null)
        {
            seviyeSistemi = gameObject.AddComponent<LevelSystem>();
        }

        temaSistemi = GetComponent<ThemeSystem>();
        if (temaSistemi == null)
        {
            temaSistemi = gameObject.AddComponent<ThemeSystem>();
        }
        temaSistemi.TemalariHazirla();

        KamerayiKur();
        IsigiKur();
        ArkaPlaniKur();
        ParcaciklariKur();

        OlaySistemiKur();
        arayuz = GameUI.Olustur(transform, this);
        sesler = SoundManager.Olustur(transform);
        anaMenu = MainMenu.Olustur(transform, this, seviyeSistemi, temaSistemi);

        reklamlar = AdManager.Olustur(transform);
        reklamlar.Baslat();

        bulutKayit = CloudSave.Olustur(transform, this, seviyeSistemi, temaSistemi);
        bulutKayit.Baslat();

        rekor = PlayerPrefs.GetInt("rekor", 0);
        AnaMenuyuAc();
    }

    // Menudeki dugmelerin tiklanabilmesi icin sahnede bir EventSystem olmali.
    void OlaySistemiKur()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            return;
        }

        GameObject go = new GameObject("Olay Sistemi");
        go.transform.SetParent(transform, false);
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
    }

    void Update()
    {
        // ekran dondurulduyse kamera ve arka plan olculerini yeniden hesapla
        if (Screen.width != oncekiEkranGenisligi || Screen.height != oncekiEkranYuksekligi)
        {
            KameraMesafesiniHesapla();
            ArkaPlaniOlcekle();
        }

        // reklam ekrandayken oyun durur ve dokunuslari dinlemez
        if (reklamlar.ReklamAcikMi())
        {
            return;
        }

        // Android geri tusu
        if (TapInput.GeriTusunaBasildi())
        {
            GeriTusuIsle();
            return;
        }

        // ana menudeyken oynanis calismiyor, sadece dekor kule suzuluyor
        if (menudeyiz)
        {
            menuZamani = menuZamani + Time.deltaTime;
            kameraHedefY = menuKameraYuksekligi + Mathf.Sin(menuZamani * 0.6f) * 0.6f;
            KamerayiGuncelle();
            DeseniKaydir();
            return;
        }

        // Kule yikildiginda odullu reklam henuz yuklenmemis olabilir. Panel acikken
        // yuklenirse "Reklam izle, devam et" dugmesi sonradan da belirsin.
        if (!oynaniyor && !seviyeBitti && !devamHakkiKullanildi && reklamlar.OdulluHazirMi())
        {
            arayuz.DevamDugmesiniGoster(true);
        }

        if (oynaniyor && hareketliBlok != null)
        {
            kayma = kayma + yon * hiz * Time.deltaTime;
            if (kayma > hareketMesafesi)
            {
                kayma = hareketMesafesi;
                yon = -1;
            }
            if (kayma < -hareketMesafesi)
            {
                kayma = -hareketMesafesi;
                yon = 1;
            }

            Vector3 konum = hareketliBlok.transform.position;
            if (eksen == 0)
            {
                konum.x = sonMerkez.x + kayma;
            }
            else
            {
                konum.z = sonMerkez.z + kayma;
            }
            hareketliBlok.transform.position = konum;
        }

        if (TapInput.Dokunuldu())
        {
            DokunusIsle();
        }

        KamerayiGuncelle();
        DeseniKaydir();
    }

    // ------------------------------------------------------------------
    // KURULUM
    // ------------------------------------------------------------------

    void KamerayiKur()
    {
        GameObject go = new GameObject("Ana Kamera");
        go.transform.SetParent(transform, false);
        go.tag = "MainCamera";

        anaKamera = go.AddComponent<Camera>();
        anaKamera.clearFlags = CameraClearFlags.SolidColor;
        anaKamera.backgroundColor = Color.black;
        anaKamera.fieldOfView = kameraFov;
        anaKamera.nearClipPlane = 0.3f;
        anaKamera.farClipPlane = 400f;
        go.AddComponent<AudioListener>();

        go.transform.rotation = Quaternion.Euler(kameraAcisi, 0f, 0f);
        KameraMesafesiniHesapla();
    }

    // Kameranin ne kadar uzakta duracagini ekranin en/boy oranindan buluyoruz.
    // Boylece dik telefon ekraninda da editordeki genis Game view'da da
    // blogun gittigi en uc nokta ekranin icinde kaliyor.
    void KameraMesafesiniHesapla()
    {
        oncekiEkranGenisligi = Screen.width;
        oncekiEkranYuksekligi = Screen.height;

        float oran = EkranOrani();
        float gerekliYariGenislik = hareketMesafesi + blokBoyu * 0.5f + 0.5f;
        float yariGorusAlani = Mathf.Tan(kameraFov * 0.5f * Mathf.Deg2Rad) * oran;
        float mesafe = gerekliYariGenislik / yariGorusAlani;

        if (mesafe < 8f)
        {
            mesafe = 8f;
        }
        if (mesafe > 90f)
        {
            mesafe = 90f;
        }

        Vector3 ileri = Quaternion.Euler(kameraAcisi, 0f, 0f) * Vector3.forward;
        kameraOfseti = -ileri * mesafe;
    }

    float EkranOrani()
    {
        float oran = (float)Screen.width / Screen.height;
        if (oran < 0.2f)
        {
            oran = 0.2f;
        }
        return oran;
    }

    void IsigiKur()
    {
        GameObject go = new GameObject("Gunes");
        go.transform.SetParent(transform, false);

        Light isik = go.AddComponent<Light>();
        isik.type = LightType.Directional;
        isik.intensity = 1.05f;
        isik.color = new Color(1f, 0.98f, 0.94f);
        isik.shadows = LightShadows.Soft;
        isik.shadowStrength = 0.3f;
        go.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        // ortam isigi orta seviyede: bloklar ne kararsin ne de solsun
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.52f);
        RenderSettings.fog = false;
    }

    // Arka plan iki karenin ust uste binmesinden olusuyor:
    // arkada renk gradyani, onunde yavasca kayan Kenney deseni.
    void ArkaPlaniKur()
    {
        desenDokusu = Resources.Load<Texture2D>("Textures/pattern");

        gradyanMateryali = new Material(GolgelendiriciBul("Unlit/Texture"));
        gradyanKaresi = KareOlustur("Arka Plan", gradyanMateryali, arkaPlanMesafesi);

        Shader desenGolge = Shader.Find("TapStack/ArkaPlanDeseni");
        if (desenGolge != null && desenDokusu != null)
        {
            desenMateryali = new Material(desenGolge);
            desenMateryali.mainTexture = desenDokusu;
            desenMateryali.SetColor("_Color", new Color(1f, 1f, 1f, 0.05f));
            desenKaresi = KareOlustur("Arka Plan Deseni", desenMateryali, arkaPlanMesafesi - 1f);
        }

        ArkaPlaniOlcekle();
    }

    Transform KareOlustur(string ad, Material materyal, float mesafe)
    {
        GameObject kare = GameObject.CreatePrimitive(PrimitiveType.Quad);
        kare.name = ad;
        Destroy(kare.GetComponent<MeshCollider>());
        kare.transform.SetParent(anaKamera.transform, false);
        kare.transform.localPosition = new Vector3(0f, 0f, mesafe);
        kare.transform.localRotation = Quaternion.identity;

        Renderer ciz = kare.GetComponent<Renderer>();
        ciz.sharedMaterial = materyal;
        ciz.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ciz.receiveShadows = false;
        return kare.transform;
    }

    // Kareler kameranin gordugu alani tam dolduracak sekilde buyutuluyor.
    void ArkaPlaniOlcekle()
    {
        if (gradyanKaresi == null)
        {
            return;
        }

        float yukseklik = 2f * arkaPlanMesafesi * Mathf.Tan(kameraFov * 0.5f * Mathf.Deg2Rad);
        float genislik = yukseklik * EkranOrani();

        gradyanKaresi.localScale = new Vector3(genislik * 1.05f, yukseklik * 1.05f, 1f);
        if (desenKaresi != null)
        {
            desenKaresi.localScale = new Vector3(genislik, yukseklik, 1f);
            // desen ince cizgiler halinde kalsin, one cikmasin
            float tekrar = 16f;
            desenMateryali.mainTextureScale = new Vector2(tekrar * EkranOrani(), tekrar);
        }
    }

    void DeseniKaydir()
    {
        if (desenMateryali == null)
        {
            return;
        }
        Vector2 kaydirma = desenMateryali.mainTextureOffset;
        kaydirma.x = kaydirma.x + Time.deltaTime * 0.012f;
        kaydirma.y = kaydirma.y + Time.deltaTime * 0.006f;
        desenMateryali.mainTextureOffset = kaydirma;
    }

    void ParcaciklariKur()
    {
        Texture2D yildizDoku = Resources.Load<Texture2D>("Particles/star");
        Texture2D daireDoku = Resources.Load<Texture2D>("Particles/circle");

        yildizParcacik = ParcacikOlustur("Yildiz Parcacik", yildizDoku, 0.9f, 1.1f, 0.7f);
        konfetiParcacik = ParcacikOlustur("Konfeti", daireDoku, 0.6f, 2.6f, 1.4f);
    }

    ParticleSystem ParcacikOlustur(string ad, Texture2D doku, float boyut, float omur, float yercekimi)
    {
        GameObject go = new GameObject(ad);
        go.transform.SetParent(transform, false);

        ParticleSystem parcacik = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule ana = parcacik.main;
        ana.loop = false;
        ana.playOnAwake = false;
        ana.startLifetime = omur;
        ana.startSpeed = 5f;
        ana.startSize = boyut;
        ana.gravityModifier = yercekimi;
        ana.simulationSpace = ParticleSystemSimulationSpace.World;
        ana.maxParticles = 300;
        ana.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        ParticleSystem.EmissionModule cikis = parcacik.emission;
        cikis.enabled = false;      // sadece Emit ile elle cikaracagiz

        ParticleSystem.ShapeModule sekil = parcacik.shape;
        sekil.shapeType = ParticleSystemShapeType.Sphere;
        sekil.radius = 0.35f;

        // omrunun sonuna dogru saydamlassin
        ParticleSystem.ColorOverLifetimeModule solma = parcacik.colorOverLifetime;
        solma.enabled = true;
        Gradient gecis = new Gradient();
        GradientColorKey[] renkler = new GradientColorKey[2];
        renkler[0] = new GradientColorKey(Color.white, 0f);
        renkler[1] = new GradientColorKey(Color.white, 1f);
        GradientAlphaKey[] saydamlik = new GradientAlphaKey[3];
        saydamlik[0] = new GradientAlphaKey(1f, 0f);
        saydamlik[1] = new GradientAlphaKey(1f, 0.6f);
        saydamlik[2] = new GradientAlphaKey(0f, 1f);
        gecis.SetKeys(renkler, saydamlik);
        solma.color = new ParticleSystem.MinMaxGradient(gecis);

        ParticleSystemRenderer ciz = go.GetComponent<ParticleSystemRenderer>();
        ciz.material = ParcacikMateryali(doku);
        ciz.renderMode = ParticleSystemRenderMode.Billboard;
        ciz.sortingOrder = 10;

        return parcacik;
    }

    Material ParcacikMateryali(Texture2D doku)
    {
        Material materyal = new Material(GolgelendiriciBul("Sprites/Default"));
        if (doku != null)
        {
            materyal.mainTexture = doku;
        }
        return materyal;
    }

    // Istenen shader yoksa oyunun tamamen bozulmamasi icin yedekleri deniyoruz.
    Shader GolgelendiriciBul(string ad)
    {
        Shader bulunan = Shader.Find(ad);
        if (bulunan != null)
        {
            return bulunan;
        }
        bulunan = Shader.Find("Sprites/Default");
        if (bulunan != null)
        {
            return bulunan;
        }
        return Shader.Find("Standard");
    }

    // ------------------------------------------------------------------
    // MENU
    // ------------------------------------------------------------------

    // Ana menuyu acar. Oynanis durur, arkada dekor amacli bir kule kalir.
    public void AnaMenuyuAc()
    {
        menudeyiz = true;
        oynaniyor = false;
        seviyeBitti = false;
        hareketliBlok = null;
        sarsinti = 0f;

        // Seviye sonundan kalan konfeti ve yildizlar menunun ustune yagmasin.
        if (yildizParcacik != null)
        {
            yildizParcacik.Clear();
        }
        if (konfetiParcacik != null)
        {
            konfetiParcacik.Clear();
        }

        arayuz.Gizle();
        DekorKuleKur();
        anaMenu.Goster(rekor);
    }

    // Menuden secilen seviyeyle oyunu baslatir.
    public void OyunuBaslat(int index)
    {
        menudeyiz = false;
        anaMenu.Gizle();
        arayuz.Goster();
        SeviyeyiBaslat(index);
    }

    public void SesiAcKapat()
    {
        sesler.SesiAcKapat();
    }

    public bool SesAcikMi()
    {
        return sesler.SesAcikMi();
    }

    // Ekrana dokununca ne olacagina karar verir.
    void DokunusIsle()
    {
        if (oynaniyor)
        {
            // Seviye baslar baslamaz gelen dokunus (ornegin panelde yanlislikla cift
            // dokunma) blogu daha ekranin kenarindayken birakip denemeyi mahvetmesin.
            if (Time.time - oyunBaslamaZamani > ilkDokunusBeklemesi)
            {
                BloguBirak();
            }
        }
        else if (Time.time - bitisZamani > yenidenBaslamaGecikmesi)
        {
            // arada bir gecis reklami (ne siklikla cikacagini AdManager belirliyor)
            reklamlar.GecisReklamiDene();
            if (seviyeBitti)
            {
                SeviyeyiBaslat(seviyeIndex + 1);    // sonraki seviye
            }
            else
            {
                SeviyeyiBaslat(seviyeIndex);        // ayni seviyeyi tekrar dene
            }
        }
    }

    // "Reklam izle, devam et" dugmesine basilinca GameUI cagiriyor.
    public void ReklamlaDevamEt()
    {
        if (devamHakkiKullanildi || oynaniyor || seviyeBitti || menudeyiz)
        {
            return;
        }
        reklamlar.OdulluGoster(KaldigiYerdenDevamEt);
    }

    // Kacirilan blok kuleye hic eklenmedigi icin kule yikilmadan onceki haliyle
    // duruyor; yeni bir hareketli blok vermek yetiyor.
    void KaldigiYerdenDevamEt()
    {
        if (oynaniyor || seviyeBitti || menudeyiz)
        {
            return;
        }
        devamHakkiKullanildi = true;
        oynaniyor = true;
        oyunBaslamaZamani = Time.time;
        arayuz.PaneliKapat();
        YeniHareketliBlok();
    }

    // Menude ilerleme degisince (tema alma, secme) MainMenu cagiriyor.
    public void IlerlemeDegisti()
    {
        bulutKayit.Kaydet();
    }

    // Buluttaki kayit bu cihazdakiyle birlestirilince CloudSave cagiriyor.
    public void BulutVerisiGeldi()
    {
        rekor = PlayerPrefs.GetInt("rekor", 0);

        if (menudeyiz)
        {
            DekorKuleKur();
            anaMenu.VerilerDegisti(rekor);
        }
    }

    // Telefonda ana ekrana donulunce bekleyen kaydi hemen buluta gonder.
    void OnApplicationPause(bool durdu)
    {
        if (durdu && bulutKayit != null)
        {
            bulutKayit.HemenKaydet();
        }
    }

    // Android geri tusu:
    //   oyundaysak      -> ana menuye don
    //   menunun alt ekranindaysak (Seviyeler, Temalar) -> ana menuye don
    //   zaten ana menudeysek -> oyundan cik
    void GeriTusuIsle()
    {
        if (!menudeyiz)
        {
            AnaMenuyuAc();
            return;
        }

        if (anaMenu.GeriTusu())
        {
            return;
        }

        // Cikmadan once bekleyen bulut kaydini gondermeyi dene.
        // Internet yavassa yetismeyebilir; ilerleme zaten cihazda kayitli.
        bulutKayit.HemenKaydet();
        Application.Quit();
    }

    // Menude tema degistirilince arkadaki kule yeni renklerle kurulsun.
    public void DekorKuleyiYenile()
    {
        if (menudeyiz)
        {
            DekorKuleKur();
        }
    }

    // Menunun arkasinda duran, oynanmayan kule.
    void DekorKuleKur()
    {
        Level ilkSeviye = seviyeSistemi.SeviyeGetir(0);
        seviyeRengi = ilkSeviye.blokRengi;
        aktifTema = temaSistemi.SeciliTema();
        ArkaPlanRenginiDegistir(ilkSeviye.arkaUst, ilkSeviye.arkaAlt);

        if (kuleKok != null)
        {
            Destroy(kuleKok.gameObject);
        }
        GameObject kule = new GameObject("Menu Kulesi");
        kule.transform.SetParent(transform, false);
        kuleKok = kule.transform;

        GameObject taban = BlokOlustur(new Vector3(blokBoyu, 40f, blokBoyu),
                                       new Vector3(0f, -20f - blokYuksekligi * 0.5f, 0f),
                                       Koyulastir(BlokRengi(0), 0.4f));
        taban.name = "Menu Taban";

        // bloklar hafif kaydirmali dizilsin, gercekten oynanmis bir kule gibi dursun
        float yukseklik = 0f;
        for (int i = 0; i < 9; i++)
        {
            float kaymaX = Mathf.Sin(i * 1.3f) * 0.5f;
            float kaymaZ = Mathf.Cos(i * 0.9f) * 0.4f;

            GameObject blok = BlokOlustur(new Vector3(blokBoyu, blokYuksekligi, blokBoyu),
                                          new Vector3(kaymaX, yukseklik, kaymaZ), BlokRengi(i));
            blok.name = "Menu Blok " + i;
            yukseklik = yukseklik + blokYuksekligi;
        }

        // kamerayi kulenin biraz uzerine alalim ki kule alt tarafta kalsin,
        // menu yazilari da bos gokyuzunun uzerine gelsin
        menuKameraYuksekligi = yukseklik + 3.2f;
        kameraHedefY = menuKameraYuksekligi;
        anaKamera.transform.position = new Vector3(0f, kameraHedefY, 0f) + kameraOfseti;
    }

    // ------------------------------------------------------------------
    // SEVIYE AKISI
    // ------------------------------------------------------------------

    void SeviyeyiBaslat(int index)
    {
        seviyeIndex = index;
        aktifSeviye = seviyeSistemi.SeviyeGetir(index);

        // eski kuleyi temizle
        if (kuleKok != null)
        {
            Destroy(kuleKok.gameObject);
        }
        GameObject kule = new GameObject("Kule");
        kule.transform.SetParent(transform, false);
        kuleKok = kule.transform;

        seviyeRengi = aktifSeviye.blokRengi;
        aktifTema = temaSistemi.SeciliTema();
        seviyeBlokBoyu = aktifSeviye.baslangicGenisligi;
        ArkaPlanRenginiDegistir(aktifSeviye.arkaUst, aktifSeviye.arkaAlt);

        yerlesenBlok = 0;
        mukemmelSayisi = 0;
        combo = 0;
        hiz = aktifSeviye.baslangicHizi;
        eksen = 0;
        yon = 1;
        oynaniyor = true;
        oyunBaslamaZamani = Time.time;
        seviyeBitti = false;
        sarsinti = 0f;
        devamHakkiKullanildi = false;

        sonMerkez = Vector3.zero;
        sonGenislikX = seviyeBlokBoyu;
        sonGenislikZ = seviyeBlokBoyu;

        // kulenin altinda asagi dogru uzayan govde, kule havada durmasin diye
        GameObject taban = BlokOlustur(new Vector3(seviyeBlokBoyu, 40f, seviyeBlokBoyu),
                                       new Vector3(0f, -20f - blokYuksekligi * 0.5f, 0f),
                                       Koyulastir(BlokRengi(0), 0.4f));
        taban.name = "Taban";

        GameObject ilkBlok = BlokOlustur(new Vector3(seviyeBlokBoyu, blokYuksekligi, seviyeBlokBoyu), Vector3.zero, BlokRengi(0));
        ilkBlok.name = "Blok 0";

        kameraHedefY = 0f;
        anaKamera.transform.position = new Vector3(0f, kameraHedefY, 0f) + kameraOfseti;

        arayuz.SeviyeyiGoster(aktifSeviye.isim, seviyeIndex + 1, aktifSeviye.hedefBlok, rekor, aktifSeviye.vurguRengi);
        YeniHareketliBlok();
    }

    void SeviyeTamamlandi()
    {
        oynaniyor = false;
        seviyeBitti = true;
        bitisZamani = Time.time;

        int yildiz = YildizHesapla();
        seviyeSistemi.YildizKaydet(seviyeIndex, yildiz);
        seviyeSistemi.SeviyeAc(seviyeIndex + 1);

        KonfetiYagdir();
        sesler.SeviyeGecildi();
        Haptics.Orta();

        // rekor sadece yikilinca degil, seviye bitince de guncellenmeli
        if (yerlesenBlok > rekor)
        {
            rekor = yerlesenBlok;
            PlayerPrefs.SetInt("rekor", rekor);
            PlayerPrefs.Save();
        }

        bulutKayit.Kaydet();
        arayuz.SeviyeTamamlandiPaneli(seviyeIndex + 1, yildiz, mukemmelSayisi);
    }

    void SeviyeBasarisiz()
    {
        oynaniyor = false;
        seviyeBitti = false;
        bitisZamani = Time.time;
        combo = 0;
        sarsinti = 0.3f;

        // kacirilan blok asagi dussun
        if (hareketliBlok != null)
        {
            Rigidbody rb = hareketliBlok.AddComponent<Rigidbody>();
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
            Destroy(hareketliBlok, 5f);
            hareketliBlok = null;
        }

        if (yerlesenBlok > rekor)
        {
            rekor = yerlesenBlok;
            PlayerPrefs.SetInt("rekor", rekor);
            PlayerPrefs.Save();
        }

        sesler.Basarisiz();
        Haptics.Guclu();
        arayuz.BasarisizPaneli(yerlesenBlok, aktifSeviye.hedefBlok, rekor);

        // reklamla devam: her denemede bir kez, reklam hazirsa
        arayuz.DevamDugmesiniGoster(!devamHakkiKullanildi && reklamlar.OdulluHazirMi());
        bulutKayit.Kaydet();
    }

    // Mukemmel yerlestirme oranina gore 1-3 yildiz.
    int YildizHesapla()
    {
        if (yerlesenBlok <= 0)
        {
            return 1;
        }

        float oran = (float)mukemmelSayisi / yerlesenBlok;
        if (oran >= 0.6f)
        {
            return 3;
        }
        if (oran >= 0.3f)
        {
            return 2;
        }
        return 1;
    }

    // ------------------------------------------------------------------
    // BLOKLAR
    // ------------------------------------------------------------------

    void YeniHareketliBlok()
    {
        kayma = -hareketMesafesi;
        yon = 1;

        Vector3 merkez = sonMerkez;
        merkez.y = merkez.y + blokYuksekligi;
        if (eksen == 0)
        {
            merkez.x = sonMerkez.x + kayma;
        }
        else
        {
            merkez.z = sonMerkez.z + kayma;
        }

        hareketliBlok = BlokOlustur(new Vector3(sonGenislikX, blokYuksekligi, sonGenislikZ), merkez, BlokRengi(yerlesenBlok + 1));
        hareketliBlok.name = "Blok " + (yerlesenBlok + 1);
    }

    void BloguBirak()
    {
        if (hareketliBlok == null)
        {
            return;
        }

        arayuz.IpucuGizle();

        // hareket ettigimiz eksendeki degerleri al
        float eskiMerkez;
        float yeniMerkez;
        float genislik;

        if (eksen == 0)
        {
            eskiMerkez = sonMerkez.x;
            yeniMerkez = hareketliBlok.transform.position.x;
            genislik = sonGenislikX;
        }
        else
        {
            eskiMerkez = sonMerkez.z;
            yeniMerkez = hareketliBlok.transform.position.z;
            genislik = sonGenislikZ;
        }

        float fark = yeniMerkez - eskiMerkez;
        float[] sonuc = KesimHesapla(fark, genislik);

        // sonuc[0] == 0 ise iki blok hic kesismiyor, seviye biter
        if (sonuc[0] < 0.5f)
        {
            SeviyeBasarisiz();
            return;
        }

        bool mukemmel = sonuc[1] > 0.5f;
        float yeniGenislik = sonuc[2];
        float merkezKaymasi = sonuc[3];

        // kesilen parca varsa once onu dusur (hareketli blok hala eski halinde)
        if (sonuc[4] > 0f)
        {
            DusenParcaOlustur(eskiMerkez + sonuc[5], sonuc[4]);
        }

        // kalan parcayi yerine oturt
        Vector3 merkez = sonMerkez;
        merkez.y = merkez.y + blokYuksekligi;
        Vector3 olcu = hareketliBlok.transform.localScale;

        if (eksen == 0)
        {
            merkez.x = eskiMerkez + merkezKaymasi;
            olcu.x = yeniGenislik;
            sonGenislikX = yeniGenislik;
        }
        else
        {
            merkez.z = eskiMerkez + merkezKaymasi;
            olcu.z = yeniGenislik;
            sonGenislikZ = yeniGenislik;
        }

        hareketliBlok.transform.position = merkez;
        hareketliBlok.transform.localScale = olcu;

        sonMerkez = merkez;
        yerlesenBlok = yerlesenBlok + 1;
        kameraHedefY = merkez.y;
        eksen = 1 - eksen;

        // her blokta biraz hizlan
        hiz = hiz + aktifSeviye.hizArtisi;
        if (hiz > enYuksekHiz)
        {
            hiz = enYuksekHiz;
        }

        if (mukemmel)
        {
            mukemmelSayisi = mukemmelSayisi + 1;
            combo = combo + 1;
            sesler.Mukemmel(combo);
            Haptics.Orta();
            arayuz.MukemmelGoster(combo);
            YildizPatlat(merkez, yeniGenislik);
        }
        else
        {
            combo = 0;
            sesler.Yerlestir();
            Haptics.Hafif();
        }

        StartCoroutine(EzilmeEfekti(hareketliBlok.transform, olcu));
        hareketliBlok = null;

        arayuz.SayaciGuncelle(yerlesenBlok, aktifSeviye.hedefBlok);

        if (yerlesenBlok >= aktifSeviye.hedefBlok)
        {
            SeviyeTamamlandi();
            return;
        }

        YeniHareketliBlok();
    }

    // Blogun ne kadarinin kesilecegini hesaplar.
    // fark     : yeni blogun alttaki bloktan kaymasi
    // genislik : alttaki blogun o eksendeki boyu
    //
    // Donen dizi:
    //   [0] devam mi (1 = evet, 0 = kesisme yok, oyun bitti)
    //   [1] mukemmel mi (1 = evet)
    //   [2] kalan parcanin yeni genisligi
    //   [3] kalan parcanin merkezinin kaymasi
    //   [4] dusen parcanin genisligi (0 ise dusen parca yok)
    //   [5] dusen parcanin merkezinin kaymasi
    // "Mukemmel" sayilan pay sabit kalirsa hizli seviyelerde isabet penceresi
    // 30-40 milisaniyeye kadar duser ve 3 yildiz imkansizlasir.
    // Bu yuzden payi hizla birlikte buyutuyoruz; pencere hep ~100 ms civarinda kaliyor.
    public float EtkinTolerans()
    {
        float pay = mukemmelTolerans;
        float hizaGore = hiz * toleransHizCarpani;

        if (hizaGore > pay)
        {
            pay = hizaGore;
        }
        return pay;
    }

    public float[] KesimHesapla(float fark, float genislik)
    {
        float[] sonuc = new float[6];
        float mutlakFark = Mathf.Abs(fark);

        if (mutlakFark >= genislik)
        {
            sonuc[0] = 0f;
            return sonuc;
        }
        sonuc[0] = 1f;

        // Pay blogun genisliginin dortte birini gecmesin. Yoksa cok incelmis blokta
        // kalan her yerlestirme "mukemmel" sayilir ve kule kendiliginden genisler.
        float pay = EtkinTolerans();
        if (pay > genislik * 0.25f)
        {
            pay = genislik * 0.25f;
        }

        if (mutlakFark <= pay)
        {
            // tam ustune denk geldi: blok kesilmiyor, biraz da geri buyuyor
            sonuc[1] = 1f;
            // blok en fazla seviyenin baslangic genisligine kadar buyuyebilir
            float ustSinir = seviyeBlokBoyu;
            if (ustSinir <= 0f)
            {
                ustSinir = blokBoyu;    // henuz seviye baslamadiysa (testler icin)
            }

            float buyumusGenislik = genislik + geriBuyume;
            if (buyumusGenislik > ustSinir)
            {
                buyumusGenislik = ustSinir;
            }
            sonuc[2] = buyumusGenislik;
            sonuc[3] = 0f;
            return sonuc;
        }

        // kesisen bolge kaliyor, disarida kalan kisim kesilip dusuyor
        sonuc[2] = genislik - mutlakFark;
        sonuc[3] = fark * 0.5f;
        sonuc[4] = mutlakFark;
        sonuc[5] = fark * 0.5f + Mathf.Sign(fark) * genislik * 0.5f;
        return sonuc;
    }

    void DusenParcaOlustur(float eksenMerkezi, float parcaGenisligi)
    {
        Vector3 olcu = hareketliBlok.transform.localScale;
        Vector3 merkez = hareketliBlok.transform.position;

        if (eksen == 0)
        {
            olcu.x = parcaGenisligi;
            merkez.x = eksenMerkezi;
        }
        else
        {
            olcu.z = parcaGenisligi;
            merkez.z = eksenMerkezi;
        }

        Color renk = hareketliBlok.GetComponent<Renderer>().material.color;
        GameObject parca = BlokOlustur(olcu, merkez, renk);
        parca.name = "Dusen Parca";

        Rigidbody rb = parca.AddComponent<Rigidbody>();
        rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
        Destroy(parca, 4f);
    }

    // Mukemmel yerlestirmede blogun etrafina yildiz patlamasi.
    void YildizPatlat(Vector3 merkez, float genislik)
    {
        if (yildizParcacik == null)
        {
            return;
        }

        yildizParcacik.transform.position = merkez;

        ParticleSystem.ShapeModule sekil = yildizParcacik.shape;
        sekil.radius = genislik * 0.5f;

        ParticleSystem.EmitParams ayar = new ParticleSystem.EmitParams();
        ayar.startColor = new Color(1f, 0.95f, 0.55f);
        yildizParcacik.Emit(ayar, 20);
    }

    // Seviye bitince yukaridan renkli konfeti dokuluyor.
    void KonfetiYagdir()
    {
        if (konfetiParcacik == null)
        {
            return;
        }

        konfetiParcacik.transform.position = sonMerkez + new Vector3(0f, 9f, 0f);

        ParticleSystem.ShapeModule sekil = konfetiParcacik.shape;
        sekil.shapeType = ParticleSystemShapeType.Box;
        sekil.scale = new Vector3(12f, 0.5f, 6f);

        for (int i = 0; i < 110; i++)
        {
            ParticleSystem.EmitParams ayar = new ParticleSystem.EmitParams();
            ayar.startColor = Color.HSVToRGB(Random.value, 0.75f, 1f);
            ayar.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 2f), Random.Range(-1f, 1f));
            konfetiParcacik.Emit(ayar, 1);
        }
    }

    // Blok yerlesince kisa bir "ezilme" hareketi yapsin, daha tatmin edici duruyor.
    IEnumerator EzilmeEfekti(Transform blok, Vector3 hedefOlcu)
    {
        float sure = 0.12f;
        float gecen = 0f;

        while (gecen < sure)
        {
            if (blok == null)
            {
                yield break;
            }
            gecen = gecen + Time.deltaTime;
            float oran = gecen / sure;
            float ezilme = Mathf.Sin(oran * Mathf.PI) * 0.25f;

            Vector3 olcu = hedefOlcu;
            olcu.y = hedefOlcu.y * (1f - ezilme);
            blok.localScale = olcu;
            yield return null;
        }

        if (blok != null)
        {
            blok.localScale = hedefOlcu;
        }
    }

    GameObject BlokOlustur(Vector3 olcu, Vector3 merkez, Color renk)
    {
        GameObject blok = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(blok.GetComponent<BoxCollider>());   // bloklarin birbirine carpmasini istemiyoruz

        blok.transform.SetParent(kuleKok, true);
        blok.transform.localScale = olcu;
        blok.transform.position = merkez;

        Renderer ciz = blok.GetComponent<Renderer>();
        ciz.material.color = renk;
        blok.AddComponent<MateryalTemizleyici>();   // blok silinince materyal kopyasi da silinsin
        // duz ve canli dursun, parlama olmasin
        if (ciz.material.HasProperty("_Glossiness"))
        {
            ciz.material.SetFloat("_Glossiness", 0.05f);
        }
        if (ciz.material.HasProperty("_Smoothness"))
        {
            ciz.material.SetFloat("_Smoothness", 0.05f);
        }

        return blok;
    }

    // ------------------------------------------------------------------
    // RENKLER VE KAMERA
    // ------------------------------------------------------------------

    // Blogun rengi seviyenin renginden ve oyuncunun sectigi temadan geliyor.
    Color BlokRengi(int sira)
    {
        Theme tema = aktifTema;
        if (tema == null)
        {
            tema = temaSistemi.SeciliTema();    // henuz seviye/menu kurulmadiysa
        }
        return temaSistemi.BlokRengiUret(tema, seviyeRengi, sira);
    }

    Color Koyulastir(Color renk, float carpan)
    {
        return new Color(renk.r * carpan, renk.g * carpan, renk.b * carpan, 1f);
    }

    // Seviye degisince arka plan gradyanini yeniden uretiyoruz.
    void ArkaPlanRenginiDegistir(Color ust, Color alt)
    {
        if (gradyanDokusu != null)
        {
            Destroy(gradyanDokusu);
        }

        int yukseklik = 256;
        gradyanDokusu = new Texture2D(2, yukseklik, TextureFormat.RGB24, false);
        gradyanDokusu.wrapMode = TextureWrapMode.Clamp;
        gradyanDokusu.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < yukseklik; y++)
        {
            float oran = (float)y / (yukseklik - 1);
            Color satir = Color.Lerp(alt, ust, oran);
            gradyanDokusu.SetPixel(0, y, satir);
            gradyanDokusu.SetPixel(1, y, satir);
        }
        gradyanDokusu.Apply();

        gradyanMateryali.mainTexture = gradyanDokusu;
        anaKamera.backgroundColor = alt;
    }

    void KamerayiGuncelle()
    {
        Vector3 hedef = new Vector3(0f, kameraHedefY, 0f) + kameraOfseti;
        float yumusatma = 1f - Mathf.Exp(-kameraTakipHizi * Time.deltaTime);
        Vector3 yeniKonum = Vector3.Lerp(anaKamera.transform.position, hedef, yumusatma);

        if (sarsinti > 0f)
        {
            sarsinti = sarsinti - Time.deltaTime * 1.2f;
            if (sarsinti < 0f)
            {
                sarsinti = 0f;
            }
            yeniKonum = yeniKonum + Random.insideUnitSphere * sarsinti;
        }

        anaKamera.transform.position = yeniKonum;
    }
}
