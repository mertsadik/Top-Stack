using UnityEngine;

// Oyun seslerini calan basit sinif. Ses dosyalari Resources/Sounds icinde.
public class SoundManager : MonoBehaviour
{
    AudioSource kaynak;

    AudioClip yerlestirSesi;
    AudioClip mukemmelSesi;
    AudioClip basarisizSesi;
    AudioClip seviyeSesi;

    bool sesAcik = true;

    // Ses objesini kodla olusturur.
    public static SoundManager Olustur(Transform ebeveyn)
    {
        GameObject go = new GameObject("Sesler");
        go.transform.SetParent(ebeveyn, false);

        SoundManager sesler = go.AddComponent<SoundManager>();
        sesler.kaynak = go.AddComponent<AudioSource>();
        sesler.kaynak.playOnAwake = false;

        sesler.yerlestirSesi = Resources.Load<AudioClip>("Sounds/place");
        sesler.mukemmelSesi = Resources.Load<AudioClip>("Sounds/perfect");
        sesler.basarisizSesi = Resources.Load<AudioClip>("Sounds/fail");
        sesler.seviyeSesi = Resources.Load<AudioClip>("Sounds/levelup");

        // ses acik mi kapali mi, oyuncu kapatmissa oyle acilsin
        sesler.sesAcik = PlayerPrefs.GetInt("ses_acik", 1) == 1;

        return sesler;
    }

    public bool SesAcikMi()
    {
        return sesAcik;
    }

    public void SesiAcKapat()
    {
        sesAcik = !sesAcik;

        if (sesAcik)
        {
            PlayerPrefs.SetInt("ses_acik", 1);
        }
        else
        {
            PlayerPrefs.SetInt("ses_acik", 0);
        }
        PlayerPrefs.Save();
    }

    // Normal blok yerlestirme sesi. Her seferinde hafif farkli tonda calsin diye
    // pitch'i biraz rastgele degistiriyoruz.
    public void Yerlestir()
    {
        if (yerlestirSesi == null || !sesAcik)
        {
            return;
        }
        kaynak.pitch = Random.Range(0.95f, 1.05f);
        kaynak.PlayOneShot(yerlestirSesi, 0.8f);
    }

    // Mukemmel yerlestirmede combo arttikca ses tizlesir.
    public void Mukemmel(int combo)
    {
        if (mukemmelSesi == null || !sesAcik)
        {
            return;
        }
        int kademe = combo;
        if (kademe > 10)
        {
            kademe = 10;
        }
        kaynak.pitch = 1f + kademe * 0.06f;
        kaynak.PlayOneShot(mukemmelSesi, 0.7f);
    }

    public void Basarisiz()
    {
        if (basarisizSesi == null || !sesAcik)
        {
            return;
        }
        kaynak.pitch = 1f;
        kaynak.PlayOneShot(basarisizSesi, 0.9f);
    }

    public void SeviyeGecildi()
    {
        if (seviyeSesi == null || !sesAcik)
        {
            return;
        }
        kaynak.pitch = 1f;
        kaynak.PlayOneShot(seviyeSesi, 0.8f);
    }
}
