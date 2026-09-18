using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;

// Bulut kayit (Unity Gaming Services - Cloud Save).
// Oyuncu hesap acmadan "anonim" olarak giris yapiyor, ilerlemesi buluta yaziliyor.
//
// Internet yoksa ya da proje Unity Cloud'a bagli degilse oyun hic etkilenmez;
// her sey eskisi gibi cihazdaki PlayerPrefs ile calismaya devam eder.
//
// NOT: Anonim hesap o cihazdaki uygulama kurulumuna bagli. Uygulama silinip
// tekrar kurulursa yeni bir anonim hesap acilir. Telefon degistirince ilerleme
// tasinsin istenirse Google Play Games / Apple ile giris baglanmasi gerekir.
public class CloudSave : MonoBehaviour
{
    const string KayitAnahtari = "ilerleme";

    [Header("Kayit sikligi")]
    public float kayitlarArasiEnAzSaniye = 5f;     // kisa surede gelen degisiklikler tek seferde gitsin
    public float hataSonrasiBekleSaniye = 30f;     // internet yokken log'u doldurmasin
    public float tekrarBaglanmaSaniye = 60f;       // acilista baglanilamazsa ne siklikla tekrar denensin

    StackGame oyun;
    LevelSystem seviyeler;
    ThemeSystem temalar;

    bool hazir;
    bool baslatiliyor;
    float sonrakiDenemeZamani = -1f;
    bool kaydediliyor;
    bool bekleyenKayitVar;
    float siradakiKayitZamani;

    public static CloudSave Olustur(Transform ebeveyn, StackGame sahip, LevelSystem seviyeSistemi, ThemeSystem temaSistemi)
    {
        GameObject go = new GameObject("Bulut Kayit");
        go.transform.SetParent(ebeveyn, false);

        CloudSave bulut = go.AddComponent<CloudSave>();
        bulut.oyun = sahip;
        bulut.seviyeler = seviyeSistemi;
        bulut.temalar = temaSistemi;
        return bulut;
    }

    // Oyun acilirken bir kere cagriliyor.
    // Acilista internet yoksa Update belli araliklarla tekrar cagiriyor.
    public async void Baslat()
    {
        if (baslatiliyor || hazir)
        {
            return;
        }
        baslatiliyor = true;

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Bulut kayit: giris yapildi.");

            await BuluttanYukle();

            // Kaydetmeye ancak buluttaki kayit alinip birlestirildikten sonra izin
            // veriyoruz; yoksa yukleme surerken cihazdaki hal buluttakinin ustune yazilabilirdi.
            hazir = true;
        }
        catch (System.Exception hata)
        {
            hazir = false;
            Debug.LogWarning("Bulut kayit kullanilamiyor, oyun cihazdaki kayitla devam ediyor -> " + hata.Message
                + " (" + tekrarBaglanmaSaniye + " sn sonra tekrar denenecek)");
            sonrakiDenemeZamani = Time.realtimeSinceStartup + tekrarBaglanmaSaniye;
        }

        baslatiliyor = false;
    }

    // Buluttaki kaydi alip bu cihazdakiyle birlestirir.
    async Task BuluttanYukle()
    {
        HashSet<string> anahtarlar = new HashSet<string>();
        anahtarlar.Add(KayitAnahtari);

        Dictionary<string, Item> gelen = await CloudSaveService.Instance.Data.Player.LoadAsync(anahtarlar);

        SaveData bulut = null;
        Item kayit;
        if (gelen.TryGetValue(KayitAnahtari, out kayit))
        {
            bulut = SaveData.JsondanOku(kayit.Value.GetAs<string>());
        }

        // Cihazdaki verinin zamani olarak "su an"i degil, cihazda en son ne zaman
        // ilerleme oldugunu kullaniyoruz. Yoksa yeni kurulmus bir uygulama
        // buluttaki tema secimini ezerdi.
        SaveData yerel = SaveData.Topla(seviyeler, temalar);
        yerel.kayitZamani = YerelDegisiklikZamani();

        SaveData birlesik = SaveData.Birlestir(yerel, bulut);
        birlesik.Uygula(temalar);

        if (bulut != null)
        {
            Debug.Log("Bulut kayit: buluttaki ilerleme yuklendi (acik seviye " + (bulut.acikSeviye + 1) + ").");
        }

        oyun.BulutVerisiGeldi();

        // birlesmis hali buluta da yaz ki iki taraf ayni olsun
        bekleyenKayitVar = true;
    }

    // Ilerleme her degistiginde cagriliyor (seviye bitti, tema alindi...).
    // Hemen gondermiyor; birkac saniye icindeki degisiklikleri tek seferde yaziyor.
    public void Kaydet()
    {
        PlayerPrefs.SetString("son_degisiklik", Simdi().ToString());
        PlayerPrefs.Save();
        bekleyenKayitVar = true;
    }

    // Uygulama arka plana atilirken bekleyen kaydi beklemeden gonder.
    public void HemenKaydet()
    {
        if (hazir && !kaydediliyor && bekleyenKayitVar)
        {
            BulutaYaz();
        }
    }

    void Update()
    {
        // acilista baglanilamadiysa (internet yok vs.) arada bir tekrar dene
        if (!hazir && !baslatiliyor && sonrakiDenemeZamani > 0f && Time.realtimeSinceStartup > sonrakiDenemeZamani)
        {
            sonrakiDenemeZamani = -1f;
            Baslat();
        }

        if (!hazir || kaydediliyor || !bekleyenKayitVar)
        {
            return;
        }
        if (Time.realtimeSinceStartup < siradakiKayitZamani)
        {
            return;
        }
        BulutaYaz();
    }

    async void BulutaYaz()
    {
        bekleyenKayitVar = false;
        kaydediliyor = true;
        siradakiKayitZamani = Time.realtimeSinceStartup + kayitlarArasiEnAzSaniye;

        try
        {
            SaveData veri = SaveData.Topla(seviyeler, temalar);

            Dictionary<string, object> gonderilecek = new Dictionary<string, object>();
            gonderilecek[KayitAnahtari] = veri.JsonYap();

            await CloudSaveService.Instance.Data.Player.SaveAsync(gonderilecek);
        }
        catch (System.Exception hata)
        {
            Debug.LogWarning("Bulut kayit: yazilamadi, biraz sonra tekrar denenecek -> " + hata.Message);
            bekleyenKayitVar = true;
            siradakiKayitZamani = Time.realtimeSinceStartup + hataSonrasiBekleSaniye;
        }

        kaydediliyor = false;
    }

    long YerelDegisiklikZamani()
    {
        long zaman;
        if (long.TryParse(PlayerPrefs.GetString("son_degisiklik", "0"), out zaman))
        {
            return zaman;
        }
        return 0;
    }

    long Simdi()
    {
        return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
