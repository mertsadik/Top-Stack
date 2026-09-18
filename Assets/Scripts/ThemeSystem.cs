using UnityEngine;

// Bir blok temasi. Seviyeden gelen renge bu ayarlar uygulaniyor,
// boylece temalar butun seviyelerde calisiyor.
[System.Serializable]
public class Theme
{
    public string isim;
    public int fiyat;           // acilmasi icin gereken yildiz
    public float tonKaymasi;    // blok basina renk tonu ne kadar kaysin
    public float doygunluk;     // -1 ise seviyenin kendi doygunlugu kullanilir
    public float parlaklik;     // -1 ise seviyenin kendi parlakligi kullanilir
    public bool sabitTon;       // true ise seviyeden bagimsiz tek renk ailesi
    public float ton;
}

// Temalari tutar, yildizla acilmasini ve secilmesini yonetir.
public class ThemeSystem : MonoBehaviour
{
    public Theme[] temalar;

    public void TemalariHazirla()
    {
        if (temalar != null && temalar.Length > 0)
        {
            return;
        }

        temalar = new Theme[5];
        temalar[0] = TemaYap("Klasik", 0, 0.018f, -1f, -1f, false, 0f);
        temalar[1] = TemaYap("Pastel", 10, 0.018f, 0.40f, 1f, false, 0f);
        temalar[2] = TemaYap("Neon", 25, 0.030f, 1f, 1f, false, 0f);
        temalar[3] = TemaYap("Altın", 45, 0.006f, 0.85f, 1f, true, 0.12f);
        temalar[4] = TemaYap("Gökkuşağı", 70, 0.085f, 0.90f, 1f, false, 0f);
    }

    Theme TemaYap(string isim, int fiyat, float tonKaymasi, float doygunluk, float parlaklik, bool sabitTon, float ton)
    {
        Theme tema = new Theme();
        tema.isim = isim;
        tema.fiyat = fiyat;
        tema.tonKaymasi = tonKaymasi;
        tema.doygunluk = doygunluk;
        tema.parlaklik = parlaklik;
        tema.sabitTon = sabitTon;
        tema.ton = ton;
        return tema;
    }

    // Seviyenin ana rengini temaya gore blok rengine cevirir.
    // Hem oyundaki bloklar hem de menudeki onizleme bunu kullaniyor.
    public Color BlokRengiUret(Theme tema, Color seviyeRengi, int sira)
    {
        float h;
        float s;
        float v;
        Color.RGBToHSV(seviyeRengi, out h, out s, out v);

        if (tema.sabitTon)
        {
            h = tema.ton;
        }
        h = Mathf.Repeat(h + sira * tema.tonKaymasi, 1f);

        if (tema.doygunluk >= 0f)
        {
            s = tema.doygunluk;
        }
        if (tema.parlaklik >= 0f)
        {
            v = tema.parlaklik;
        }

        // ardisik bloklar arasinda hafif kontrast
        v = v + Mathf.Sin(sira * 0.8f) * 0.06f;
        if (v > 1f)
        {
            v = 1f;
        }
        if (v < 0.3f)
        {
            v = 0.3f;
        }

        return Color.HSVToRGB(h, s, v);
    }

    public Theme TemaGetir(int index)
    {
        TemalariHazirla();
        if (index < 0 || index >= temalar.Length)
        {
            return temalar[0];
        }
        return temalar[index];
    }

    public int SeciliIndex()
    {
        int index = PlayerPrefs.GetInt("secili_tema", 0);
        if (!AcikMi(index))
        {
            return 0;
        }
        return index;
    }

    public Theme SeciliTema()
    {
        return TemaGetir(SeciliIndex());
    }

    public void TemaSec(int index)
    {
        if (!AcikMi(index))
        {
            return;
        }
        PlayerPrefs.SetInt("secili_tema", index);
        PlayerPrefs.Save();
    }

    public bool AcikMi(int index)
    {
        TemalariHazirla();
        if (index < 0 || index >= temalar.Length)
        {
            return false;
        }
        if (temalar[index].fiyat <= 0)
        {
            return true;        // Klasik hep acik
        }
        return PlayerPrefs.GetInt("tema_" + index + "_acik", 0) == 1;
    }

    public int HarcananYildiz()
    {
        return PlayerPrefs.GetInt("harcanan_yildiz", 0);
    }

    public int KullanilabilirYildiz(LevelSystem seviyeSistemi)
    {
        int kalan = seviyeSistemi.ToplamYildiz() - HarcananYildiz();

        // Iki cihazda farkli temalar alinip bulut kaydinda birlestirilirse
        // harcanan yildiz toplamdan fazla gorunebiliyor. Eksi bakiye gostermiyoruz.
        if (kalan < 0)
        {
            kalan = 0;
        }
        return kalan;
    }

    // Yildiz yetiyorsa temayi acar ve secer. Actiysa true doner.
    public bool SatinAl(int index, LevelSystem seviyeSistemi)
    {
        TemalariHazirla();
        if (AcikMi(index))
        {
            return false;
        }

        Theme tema = TemaGetir(index);
        if (KullanilabilirYildiz(seviyeSistemi) < tema.fiyat)
        {
            return false;       // yildiz yetmiyor
        }

        PlayerPrefs.SetInt("tema_" + index + "_acik", 1);
        PlayerPrefs.SetInt("harcanan_yildiz", HarcananYildiz() + tema.fiyat);
        PlayerPrefs.SetInt("secili_tema", index);
        PlayerPrefs.Save();
        return true;
    }
}
