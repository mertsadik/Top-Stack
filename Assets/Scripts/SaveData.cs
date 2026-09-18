using UnityEngine;

// Bulut kaydinin tasidigi veri: oyuncunun butun ilerlemesi.
//
// Onemli kural: toplam yildiz ve harcanan yildiz buraya YAZILMIYOR.
// Ikisi de baska verilerden hesaplanabiliyor:
//   toplam yildiz  = her seviyenin yildizlarinin toplami
//   harcanan yildiz = acik temalarin fiyatlarinin toplami
// Boylece iki cihazin kaydi birlestirilince sayilar sisip bozulmuyor.
[System.Serializable]
public class SaveData
{
    public int acikSeviye;
    public int rekor;
    public int seciliTema;
    public int[] seviyeYildizlari = new int[0];     // index = seviye numarasi
    public bool[] acikTemalar = new bool[0];        // index = tema numarasi
    public long kayitZamani;                        // unix saniye

    // Bu cihazdaki PlayerPrefs'ten guncel ilerlemeyi toplar.
    public static SaveData Topla(LevelSystem seviyeler, ThemeSystem temalar)
    {
        SaveData veri = new SaveData();
        veri.acikSeviye = seviyeler.AcikSeviye();
        veri.rekor = PlayerPrefs.GetInt("rekor", 0);
        veri.seciliTema = temalar.SeciliIndex();

        // yildiz sadece gecilmis seviyelerde olabiliyor, acik seviyeye kadar bakmak yetiyor
        veri.seviyeYildizlari = new int[veri.acikSeviye + 1];
        for (int i = 0; i < veri.seviyeYildizlari.Length; i++)
        {
            veri.seviyeYildizlari[i] = seviyeler.YildizGetir(i);
        }

        temalar.TemalariHazirla();
        veri.acikTemalar = new bool[temalar.temalar.Length];
        for (int i = 0; i < veri.acikTemalar.Length; i++)
        {
            veri.acikTemalar[i] = temalar.AcikMi(i);
        }

        veri.kayitZamani = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return veri;
    }

    // Veriyi bu cihazin PlayerPrefs'ine yazar ve toplamlari yeniden hesaplar.
    public void Uygula(ThemeSystem temalar)
    {
        PlayerPrefs.SetInt("acik_seviye", acikSeviye);
        PlayerPrefs.SetInt("rekor", rekor);

        int toplamYildiz = 0;
        for (int i = 0; i < seviyeYildizlari.Length; i++)
        {
            PlayerPrefs.SetInt("seviye_" + i + "_yildiz", seviyeYildizlari[i]);
            toplamYildiz = toplamYildiz + seviyeYildizlari[i];
        }
        PlayerPrefs.SetInt("toplam_yildiz", toplamYildiz);

        temalar.TemalariHazirla();
        int harcanan = 0;
        for (int i = 0; i < acikTemalar.Length && i < temalar.temalar.Length; i++)
        {
            if (acikTemalar[i])
            {
                PlayerPrefs.SetInt("tema_" + i + "_acik", 1);
                harcanan = harcanan + temalar.temalar[i].fiyat;
            }
            else
            {
                PlayerPrefs.SetInt("tema_" + i + "_acik", 0);
            }
        }
        PlayerPrefs.SetInt("harcanan_yildiz", harcanan);

        // secili tema acik degilse klasige don
        int secilecek = seciliTema;
        if (secilecek < 0 || secilecek >= acikTemalar.Length || !acikTemalar[secilecek])
        {
            secilecek = 0;
        }
        PlayerPrefs.SetInt("secili_tema", secilecek);

        PlayerPrefs.Save();
    }

    // Iki kaydi birlestirir. Ilerleme oyununda hicbir ilerleme kaybolmasin diye
    // her alanda "daha iyi olan" aliniyor. Sadece tema secimi ilerleme degil,
    // o yuzden en son kaydedilen cihazin secimi geciyor.
    public static SaveData Birlestir(SaveData a, SaveData b)
    {
        if (a == null)
        {
            return b;
        }
        if (b == null)
        {
            return a;
        }

        SaveData sonuc = new SaveData();
        sonuc.acikSeviye = Mathf.Max(a.acikSeviye, b.acikSeviye);
        sonuc.rekor = Mathf.Max(a.rekor, b.rekor);
        sonuc.kayitZamani = System.Math.Max(a.kayitZamani, b.kayitZamani);

        // seviye yildizlari: her seviyede yuksek olan
        int seviyeSayisi = Mathf.Max(a.seviyeYildizlari.Length, b.seviyeYildizlari.Length);
        sonuc.seviyeYildizlari = new int[seviyeSayisi];
        for (int i = 0; i < seviyeSayisi; i++)
        {
            int ay = 0;
            int by = 0;
            if (i < a.seviyeYildizlari.Length)
            {
                ay = a.seviyeYildizlari[i];
            }
            if (i < b.seviyeYildizlari.Length)
            {
                by = b.seviyeYildizlari[i];
            }
            sonuc.seviyeYildizlari[i] = Mathf.Max(ay, by);
        }

        // temalar: herhangi bir cihazda acildiysa acik
        int temaSayisi = Mathf.Max(a.acikTemalar.Length, b.acikTemalar.Length);
        sonuc.acikTemalar = new bool[temaSayisi];
        for (int i = 0; i < temaSayisi; i++)
        {
            bool aAcik = i < a.acikTemalar.Length && a.acikTemalar[i];
            bool bAcik = i < b.acikTemalar.Length && b.acikTemalar[i];
            sonuc.acikTemalar[i] = aAcik || bAcik;
        }

        if (a.kayitZamani >= b.kayitZamani)
        {
            sonuc.seciliTema = a.seciliTema;
        }
        else
        {
            sonuc.seciliTema = b.seciliTema;
        }

        return sonuc;
    }

    public string JsonYap()
    {
        return JsonUtility.ToJson(this);
    }

    public static SaveData JsondanOku(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        // Bozuk bir kayit okunamazsa yok sayiyoruz; yoksa hata firlatip bulut
        // kaydini her acilista kilitler ve bozuk kayit hic duzelmezdi.
        SaveData veri;
        try
        {
            veri = JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Bulut kayit: kayit okunamadi (bozuk), yok sayiliyor ve cihazdaki ilerlemeyle yeniden yazilacak.");
            return null;
        }

        if (veri == null)
        {
            return null;
        }

        // eski/bozuk kayitlarda diziler bos gelebilir
        if (veri.seviyeYildizlari == null)
        {
            veri.seviyeYildizlari = new int[0];
        }
        if (veri.acikTemalar == null)
        {
            veri.acikTemalar = new bool[0];
        }
        return veri;
    }
}
