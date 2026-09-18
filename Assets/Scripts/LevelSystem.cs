using UnityEngine;

// Tek bir seviyenin ayarlari.
// Seviyeler elle yazilmiyor, LevelSystem seviye numarasindan hesapliyor.
public class Level
{
    public string isim;                 // ekranda gozuken seviye adi
    public int hedefBlok;               // seviyeyi gecmek icin gereken blok sayisi
    public float baslangicHizi;         // blogun ilk hareket hizi
    public float hizArtisi;             // her blok yerlestiginde hiza eklenen miktar
    public float baslangicGenisligi;    // ilk blogun en/derinlik olcusu

    public Color blokRengi;
    public Color arkaUst;
    public Color arkaAlt;
    public Color vurguRengi;
}

// Seviyeleri uretir ve kaydedilen ilerlemeyi PlayerPrefs'ten okur/yazar.
// Seviye sayisi sinirsiz; her seviye bir oncekinden zor.
public class LevelSystem : MonoBehaviour
{
    [Header("Zorluk ayarlari")]
    public int ilkHedefBlok = 8;            // 1. seviyede kac blok isteniyor
    public int hedefArtisi = 1;             // her seviyede hedefe eklenen blok

    public float ilkHiz = 2.4f;
    public float hizArtisiSeviyeBasina = 0.2f;
    public float enYuksekBaslangicHizi = 6.5f;

    public float ilkIvme = 0.05f;           // seviye icinde her blokta hizlanma
    public float ivmeArtisiSeviyeBasina = 0.005f;
    public float enYuksekIvme = 0.15f;

    public float ilkGenislik = 3f;          // ilk blogun genisligi
    public float daralmaSeviyeBasina = 0.06f;
    public float enDarGenislik = 1.4f;

    // Seviye adlari renk tonundan uretiliyor: once rengin adi, sonra takisi.
    string[] renkAdlari = { "Kızıl", "Turuncu", "Altın", "Limon", "Yeşil", "Zümrüt",
                            "Turkuaz", "Gök", "Lacivert", "Mor", "Leylak", "Pembe" };
    string[] takilar = { "Vadi", "Kule", "Diyar", "Zirve", "Sahil" };

    // Istenen seviyeyi hesaplar. Seviye sayisi sinirsiz oldugu icin
    // hep gecerli bir seviye doner.
    public Level SeviyeGetir(int index)
    {
        if (index < 0)
        {
            index = 0;
        }

        Level seviye = new Level();

        // --- zorluk ---
        // hedef blok sayisi hep artiyor, boylece her seviye bir oncekinden zor
        seviye.hedefBlok = ilkHedefBlok + index * hedefArtisi;

        seviye.baslangicHizi = ilkHiz + index * hizArtisiSeviyeBasina;
        if (seviye.baslangicHizi > enYuksekBaslangicHizi)
        {
            seviye.baslangicHizi = enYuksekBaslangicHizi;
        }

        seviye.hizArtisi = ilkIvme + index * ivmeArtisiSeviyeBasina;
        if (seviye.hizArtisi > enYuksekIvme)
        {
            seviye.hizArtisi = enYuksekIvme;
        }

        // bloklar da seviye ilerledikce inceliyor
        seviye.baslangicGenisligi = ilkGenislik - index * daralmaSeviyeBasina;
        if (seviye.baslangicGenisligi < enDarGenislik)
        {
            seviye.baslangicGenisligi = enDarGenislik;
        }

        // --- gorunum ---
        // ton her seviyede sabit bir miktar kayiyor, boylece ard arda gelen
        // seviyeler birbirine benzemiyor
        float ton = Mathf.Repeat(index * 0.137f, 1f);
        float karsitTon = Mathf.Repeat(ton + 0.5f, 1f);

        // arka plan hep koyu kalmali; acik tonlar (ozellikle sari-yesil) camurlu duruyor
        seviye.blokRengi = Color.HSVToRGB(ton, 0.85f, 1f);
        seviye.arkaUst = Color.HSVToRGB(karsitTon, 0.72f, 0.3f);
        seviye.arkaAlt = Color.HSVToRGB(karsitTon, 0.85f, 0.11f);
        seviye.vurguRengi = Color.HSVToRGB(Mathf.Repeat(ton + 0.08f, 1f), 0.5f, 1f);

        seviye.isim = SeviyeAdiUret(index, ton);
        return seviye;
    }

    string SeviyeAdiUret(int index, float ton)
    {
        int renkIndex = (int)(ton * renkAdlari.Length);
        if (renkIndex >= renkAdlari.Length)
        {
            renkIndex = renkAdlari.Length - 1;
        }

        int takiIndex = (index / renkAdlari.Length) % takilar.Length;
        return renkAdlari[renkIndex] + " " + takilar[takiIndex];
    }

    // Oyuncunun actigi en yuksek seviye.
    public int AcikSeviye()
    {
        return PlayerPrefs.GetInt("acik_seviye", 0);
    }

    public void SeviyeAc(int index)
    {
        if (index > AcikSeviye())
        {
            PlayerPrefs.SetInt("acik_seviye", index);
            PlayerPrefs.Save();
        }
    }

    public int YildizGetir(int index)
    {
        return PlayerPrefs.GetInt("seviye_" + index + "_yildiz", 0);
    }

    // Oyuncunun bugune kadar topladigi toplam yildiz (tema almakta kullaniliyor).
    public int ToplamYildiz()
    {
        return PlayerPrefs.GetInt("toplam_yildiz", 0);
    }

    // Sadece daha iyi bir sonuc alindiysa kaydeder.
    public void YildizKaydet(int index, int yildiz)
    {
        int eski = YildizGetir(index);
        if (yildiz > eski)
        {
            PlayerPrefs.SetInt("seviye_" + index + "_yildiz", yildiz);
            // toplama sadece artan kismi ekliyoruz, ayni seviye tekrar oynanirsa sisirmesin
            PlayerPrefs.SetInt("toplam_yildiz", ToplamYildiz() + (yildiz - eski));
            PlayerPrefs.Save();
        }
    }
}
