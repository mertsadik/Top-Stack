using UnityEngine;

// Kisa titresimler.
// Unity'nin Handheld.Vibrate() metodu her seferinde ayni uzunlukta ve cok sert
// titrestiriyor; blok basina kullanilinca rahatsiz edici oluyor. Bu yuzden
// Android'de titresim suresini kendimiz veriyoruz.
//
// Not: Editorde ve masaustunde titresim yok, metodlar sessizce bos doner.
public class Haptics
{
    static bool acik = true;
    static bool ayarOkundu;

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject titresimServisi;
    static AndroidJavaClass efektSinifi;
#endif

    public static bool AcikMi()
    {
        AyarlariOku();
        return acik;
    }

    public static void AcKapat()
    {
        AyarlariOku();
        acik = !acik;

        if (acik)
        {
            PlayerPrefs.SetInt("titresim_acik", 1);
        }
        else
        {
            PlayerPrefs.SetInt("titresim_acik", 0);
        }
        PlayerPrefs.Save();
    }

    // blok yerlesti
    public static void Hafif()
    {
        Titret(15);
    }

    // mukemmel yerlestirme / seviye gecildi
    public static void Orta()
    {
        Titret(35);
    }

    // kule yikildi
    public static void Guclu()
    {
        Titret(120);
    }

    static void AyarlariOku()
    {
        if (ayarOkundu)
        {
            return;
        }
        ayarOkundu = true;
        acik = PlayerPrefs.GetInt("titresim_acik", 1) == 1;
    }

    static void Titret(long milisaniye)
    {
        AyarlariOku();
        if (!acik)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (titresimServisi == null)
            {
                // bunlar bir kere aliniyor ve oyun boyunca saklaniyor
                using (AndroidJavaClass oynatici = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject aktivite = oynatici.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    titresimServisi = aktivite.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                efektSinifi = new AndroidJavaClass("android.os.VibrationEffect");
            }

            // Her titresimde olusan Java nesnesini hemen birakiyoruz; birakilmazsa
            // her blokta bir Java referansi birikiyor. (-1 = cihazin varsayilan siddeti)
            using (AndroidJavaObject efekt = efektSinifi.CallStatic<AndroidJavaObject>("createOneShot", milisaniye, -1))
            {
                titresimServisi.Call("vibrate", efekt);
            }
        }
        catch (System.Exception)
        {
            // Cok eski Android surumlerinde VibrationEffect yok.
            // Ayrica bu satir sayesinde Unity manifest'e VIBRATE iznini kendisi ekliyor.
            Handheld.Vibrate();
        }
#endif
    }
}
