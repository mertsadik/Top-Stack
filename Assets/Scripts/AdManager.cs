using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

// Reklamlar (Google AdMob).
//   - Gecis reklami: birkac seviye sonunda bir, arada en az belli bir sure varsa
//   - Odullu reklam: kule yikilinca "reklam izle, kaldigin yerden devam et"
//
// !!! YAYINDAN ONCE !!!
// Asagidaki ID'ler Google'in herkese acik TEST ID'leri. Magazaya cikmadan once
// kendi AdMob hesabindaki reklam birimi ID'leriyle degistir. Tersi de onemli:
// gelistirirken gercek ID kullanip kendi reklamina tiklamak hesabin kapanmasina
// sebep olabiliyor, o yuzden gelistirme boyunca test ID'leri kalsin.
public class AdManager : MonoBehaviour
{
#if UNITY_IOS
    const string GecisReklamiId = "ca-app-pub-3940256099942544/4411468910";
    const string OdulluReklamId = "ca-app-pub-3940256099942544/1712485313";
#else
    const string GecisReklamiId = "ca-app-pub-3940256099942544/1033173712";
    const string OdulluReklamId = "ca-app-pub-3940256099942544/5224354917";
#endif

    [Header("Gecis reklami sikligi")]
    public int kacSeviyeSonundaBir = 3;         // her 3 seviye sonunda (gecilse de yikilsa da) bir
    public float gecislerArasiEnAzSaniye = 60f; // arka arkaya hizli yikilmalarda reklam yagmasin
    public float yuklenemezseTekrarSaniye = 30f;

    InterstitialAd gecisReklami;
    RewardedAd odulluReklam;

    bool baslatildi;
    bool reklamAcik;
    bool sdkHazirOldu;

    int seviyeSonuSayaci;
    float sonGecisZamani;
    float gecisTekrarDeneZamani = -1f;
    float odulluTekrarDeneZamani = -1f;

    // Reklam ve onay callback'leri telefonda Unity'nin ana thread'inde GELMEYEBILIR.
    // Orada Unity API'si (Time, GameObject, MobileAds.Initialize...) cagirmak hata
    // firlatir. Bu yuzden callback'lerde sadece bayrak kaldiriyoruz, asil isi Update yapiyor.
    bool gecisKapandi;
    bool odulluKapandi;
    bool odulKazanildi;
    bool gecisYuklendi;
    bool odulluYuklendi;
    bool gecisYuklenemedi;
    bool odulluYuklenemedi;
    bool onayBilgisiGeldi;
    bool onayFormuKapandi;
    InterstitialAd bekleyenGecis;
    RewardedAd bekleyenOdullu;
    System.Action odulVerilinceYapilacak;

    public static AdManager Olustur(Transform ebeveyn)
    {
        GameObject go = new GameObject("Reklamlar");
        go.transform.SetParent(ebeveyn, false);
        return go.AddComponent<AdManager>();
    }

    public void Baslat()
    {
        // ilk gecis reklami oyunun ilk dakikasinda cikmasin
        sonGecisZamani = Time.realtimeSinceStartup;

        // Avrupa'daki oyuncular icin (GDPR) reklamdan once izin gerekiyor.
        // Google'in onay formu gerekiyorsa kendini gosteriyor, gerekmiyorsa hemen geciyor.
        ConsentRequestParameters istek = new ConsentRequestParameters();
        ConsentInformation.Update(istek, OnayBilgisiGeldi);

        // Onceki acilista izin alindiysa guncellemeyi beklemeden baslayabiliriz.
        IzinVarsaBaslat("onceki oturumdaki izin");
    }

    void OnayBilgisiGeldi(FormError hata)
    {
        if (hata != null)
        {
            Debug.LogWarning("Reklam: onay bilgisi alinamadi -> " + hata.Message);
        }

        // Callback ana thread disinda gelebilir; asil isi Update yapiyor.
        onayBilgisiGeldi = true;
    }

    void OnayFormuBitti(FormError hata)
    {
        if (hata != null)
        {
            Debug.LogWarning("Reklam: onay formu gosterilemedi -> " + hata.Message);
        }

        onayFormuKapandi = true;
    }

    // Reklam istemeye izin varsa SDK'yi bir kez baslatir. Uc farkli yerden
    // cagriliyor; hangisi once izin gorurse o baslatiyor.
    void IzinVarsaBaslat(string neden)
    {
        if (baslatildi)
        {
            return;
        }
        if (!ConsentInformation.CanRequestAds())
        {
            return;
        }

        baslatildi = true;
        Debug.Log("Reklam: SDK baslatiliyor (" + neden + ")");
        MobileAds.Initialize(SdkHazir);
    }

    void SdkHazir(InitializationStatus durum)
    {
        Debug.Log("Reklam: SDK hazir, reklamlar yukleniyor.");
        sdkHazirOldu = true;
    }

    void Update()
    {
        // --- onay (GDPR) adimlari: SDK'yi ana thread'de baslatmak icin burada ---
        if (onayBilgisiGeldi)
        {
            onayBilgisiGeldi = false;

            // Izin gerekmeyen oyuncular (Avrupa disi) formu beklemesin.
            IzinVarsaBaslat("onay bilgisi guncellendi");
            ConsentForm.LoadAndShowConsentFormIfRequired(OnayFormuBitti);
        }
        if (onayFormuKapandi)
        {
            onayFormuKapandi = false;

            // Oyuncu izin vermediyse bile Google kisisellestirilmemis reklama izin
            // verebiliyor; karari CanRequestAds veriyor.
            IzinVarsaBaslat("onay formu kapandi");
        }

        // --- yuklenemeyen reklam: tekrar deneme zamanini burada hesapla ---
        if (gecisYuklenemedi)
        {
            gecisYuklenemedi = false;
            gecisTekrarDeneZamani = Time.realtimeSinceStartup + yuklenemezseTekrarSaniye;
        }
        if (odulluYuklenemedi)
        {
            odulluYuklenemedi = false;
            odulluTekrarDeneZamani = Time.realtimeSinceStartup + yuklenemezseTekrarSaniye;
        }

        if (sdkHazirOldu)
        {
            sdkHazirOldu = false;
            GecisReklamiYukle();
            OdulluReklamYukle();
        }

        // --- yukleme sonuclari ---
        if (gecisYuklendi)
        {
            gecisYuklendi = false;
            gecisReklami = bekleyenGecis;
            bekleyenGecis = null;
            gecisReklami.OnAdFullScreenContentClosed += GecisKapandi;
            gecisReklami.OnAdFullScreenContentFailed += GecisAcilamadi;
        }

        if (odulluYuklendi)
        {
            odulluYuklendi = false;
            odulluReklam = bekleyenOdullu;
            bekleyenOdullu = null;
            odulluReklam.OnAdFullScreenContentClosed += OdulluKapandi;
            odulluReklam.OnAdFullScreenContentFailed += OdulluAcilamadi;
        }

        // --- yuklenemeyenleri bir sure sonra tekrar dene (internet yoksa vs.) ---
        if (gecisTekrarDeneZamani > 0f && Time.realtimeSinceStartup > gecisTekrarDeneZamani)
        {
            gecisTekrarDeneZamani = -1f;
            GecisReklamiYukle();
        }
        if (odulluTekrarDeneZamani > 0f && Time.realtimeSinceStartup > odulluTekrarDeneZamani)
        {
            odulluTekrarDeneZamani = -1f;
            OdulluReklamYukle();
        }

        // --- reklam kapandi ---
        if (gecisKapandi)
        {
            gecisKapandi = false;
            reklamAcik = false;
            GecisReklamiYukle();    // bir sonrakine hazirla
        }

        if (odulluKapandi)
        {
            odulluKapandi = false;
            reklamAcik = false;

            // Odullu reklamin hemen ardindan gecis reklami cikmasin; bekleme suresi
            // sadece gecis reklamlarini degil, oyuncunun izledigi her reklami saysin.
            sonGecisZamani = Time.realtimeSinceStartup;

            // Odulu reklam KAPANINCA veriyoruz; yoksa oyun reklamin arkasinda baslar.
            System.Action yapilacak = odulVerilinceYapilacak;
            bool kazandi = odulKazanildi;
            odulVerilinceYapilacak = null;
            odulKazanildi = false;

            if (kazandi && yapilacak != null)
            {
                yapilacak();
            }
            OdulluReklamYukle();
        }
    }

    // ------------------------------------------------------------------
    // GECIS REKLAMI
    // ------------------------------------------------------------------

    void GecisReklamiYukle()
    {
        if (gecisReklami != null)
        {
            gecisReklami.Destroy();
            gecisReklami = null;
        }
        InterstitialAd.Load(GecisReklamiId, new AdRequest(), GecisYuklemeBitti);
    }

    void GecisYuklemeBitti(InterstitialAd reklam, LoadAdError hata)
    {
        if (hata != null || reklam == null)
        {
            Debug.LogWarning("Reklam: gecis reklami yuklenemedi -> " + hata);
            // Time gibi Unity API'leri ana thread disinda cagrilamaz; sadece bayrak.
            gecisYuklenemedi = true;
            return;
        }
        bekleyenGecis = reklam;
        gecisYuklendi = true;
    }

    void GecisKapandi()
    {
        gecisKapandi = true;
    }

    // Gosterilemeyen reklam oyunu dondurmasin, kapanmis gibi davraniyoruz.
    void GecisAcilamadi(AdError hata)
    {
        Debug.LogWarning("Reklam: gecis reklami acilamadi -> " + hata);
        gecisKapandi = true;
    }

    // Seviye sonunda (gecilince de yikilinca da) oyun cagiriyor.
    // Reklam her seferinde degil, belli araliklarla gosteriliyor.
    public void GecisReklamiDene()
    {
        seviyeSonuSayaci = seviyeSonuSayaci + 1;

        if (seviyeSonuSayaci < kacSeviyeSonundaBir)
        {
            return;
        }
        if (Time.realtimeSinceStartup - sonGecisZamani < gecislerArasiEnAzSaniye)
        {
            return;
        }
        if (gecisReklami == null || !gecisReklami.CanShowAd())
        {
            return;     // hazir degilse sayac birikir, hazir olunca ilk firsatta cikar
        }

        seviyeSonuSayaci = 0;
        sonGecisZamani = Time.realtimeSinceStartup;
        reklamAcik = true;
        gecisReklami.Show();
    }

    // ------------------------------------------------------------------
    // ODULLU REKLAM
    // ------------------------------------------------------------------

    void OdulluReklamYukle()
    {
        if (odulluReklam != null)
        {
            odulluReklam.Destroy();
            odulluReklam = null;
        }
        RewardedAd.Load(OdulluReklamId, new AdRequest(), OdulluYuklemeBitti);
    }

    void OdulluYuklemeBitti(RewardedAd reklam, LoadAdError hata)
    {
        if (hata != null || reklam == null)
        {
            Debug.LogWarning("Reklam: odullu reklam yuklenemedi -> " + hata);
            odulluYuklenemedi = true;
            return;
        }
        bekleyenOdullu = reklam;
        odulluYuklendi = true;
    }

    void OdulluKapandi()
    {
        odulluKapandi = true;
    }

    void OdulluAcilamadi(AdError hata)
    {
        Debug.LogWarning("Reklam: odullu reklam acilamadi -> " + hata);
        odulluKapandi = true;
    }

    void OdulKazanildi(Reward odul)
    {
        odulKazanildi = true;
    }

    public bool OdulluHazirMi()
    {
        return odulluReklam != null && odulluReklam.CanShowAd();
    }

    // Odullu reklami gosterir. Oyuncu reklami sonuna kadar izleyip kapatinca
    // verilen islem calisir; yarida kapatirsa calismaz.
    public void OdulluGoster(System.Action odulVerilince)
    {
        if (!OdulluHazirMi())
        {
            return;
        }
        odulVerilinceYapilacak = odulVerilince;
        odulKazanildi = false;
        reklamAcik = true;
        odulluReklam.Show(OdulKazanildi);
    }

    // Reklam ekrandayken oyun girdi almasin ve durmali.
    public bool ReklamAcikMi()
    {
        return reklamAcik;
    }

    void OnDestroy()
    {
        if (gecisReklami != null)
        {
            gecisReklami.Destroy();
        }
        if (odulluReklam != null)
        {
            odulluReklam.Destroy();
        }
    }
}
