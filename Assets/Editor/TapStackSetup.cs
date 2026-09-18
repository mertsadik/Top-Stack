using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Projeyi kuran editor scripti.
// Menuden "TapStack > Projeyi Kur" ile calistirilabilir, ayrica komut satirindan
// -executeMethod TapStackSetup.ApplyAll seklinde de cagriliyor.
public class TapStackSetup
{
    const string SahneKlasoru = "Assets/Scenes";
    const string SahneYolu = "Assets/Scenes/Main.unity";

    [MenuItem("TapStack/Projeyi Kur")]
    public static void ApplyAll()
    {
        DokuAyarlariniUygula();
        SahneyiOlustur();
        AyarlariUygula();
        ReklamAyarlariniUygula();
        AssetDatabase.SaveAssets();
        Debug.Log("TapStack: kurulum tamamlandi.");
    }

    // Google'in ornek (test) AdMob uygulama ID'leri.
    // !!! YAYINDAN ONCE: Assets > Google Mobile Ads > Settings ekranindan kendi ID'lerini gir.
    // App ID bos kalirsa Android build'i hata verir (bos gecilirse de uygulama acilista coker).
    const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
    const string TestIosAppId = "ca-app-pub-3940256099942544~1458002511";

    static void ReklamAyarlariniUygula()
    {
        // Eklentinin ayar sinifi "internal" oldugu icin dogrudan kullanamiyoruz.
        // Eklentinin kendi LoadInstance metodunu cagiriyoruz (ayar dosyasini dogru
        // yere o olusturuyor), alanlari da SerializedObject ile dolduruyoruz.
        System.Type ayarTipi = System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
        if (ayarTipi == null)
        {
            Debug.LogWarning("TapStack: Google Mobile Ads eklentisi bulunamadi, App ID ayarlanmadi.");
            return;
        }

        System.Reflection.BindingFlags bayraklar = System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.MethodInfo yukle = ayarTipi.GetMethod("LoadInstance", bayraklar);
        if (yukle == null)
        {
            Debug.LogWarning("TapStack: GoogleMobileAdsSettings.LoadInstance bulunamadi, eklenti surumu degismis olabilir.");
            return;
        }

        ScriptableObject ayarlar = yukle.Invoke(null, null) as ScriptableObject;
        if (ayarlar == null)
        {
            Debug.LogWarning("TapStack: AdMob ayar dosyasi olusturulamadi.");
            return;
        }

        SerializedObject serilestirilmis = new SerializedObject(ayarlar);
        bool degisti = false;
        if (BossaDoldur(serilestirilmis, "adMobAndroidAppId", TestAndroidAppId))
        {
            degisti = true;
        }
        if (BossaDoldur(serilestirilmis, "adMobIOSAppId", TestIosAppId))
        {
            degisti = true;
        }

        if (degisti)
        {
            serilestirilmis.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ayarlar);
            AssetDatabase.SaveAssets();
            Debug.Log("TapStack: AdMob TEST App ID'leri yazildi (yayindan once kendi ID'lerinle degistir).");
        }
        else
        {
            Debug.Log("TapStack: AdMob App ID'leri zaten dolu, dokunulmadi.");
        }
    }

    // Sadece bos alani dolduruyor; ileride girilen gercek ID'yi ezmesin.
    static bool BossaDoldur(SerializedObject nesne, string alan, string deger)
    {
        SerializedProperty ozellik = nesne.FindProperty(alan);
        if (ozellik == null)
        {
            Debug.LogWarning("TapStack: AdMob ayarinda alan yok -> " + alan);
            return false;
        }
        if (!string.IsNullOrEmpty(ozellik.stringValue))
        {
            return false;
        }
        ozellik.stringValue = deger;
        return true;
    }

    // Indirilen PNG'ler dogru turde iceri alinmali:
    // arayuz resimleri Sprite olmali, arka plan deseni de tekrar edebilmeli.
    static void DokuAyarlariniUygula()
    {
        // panelin kenarlari 9 parcaya bolunuyor ki kose yuvarlaklari bozulmadan esnesin
        SpriteYap("Assets/Resources/UI/panel.png", new Vector4(16f, 20f, 16f, 16f));
        SpriteYap("Assets/Resources/UI/star.png", Vector4.zero);
        SpriteYap("Assets/Resources/UI/star_bos.png", Vector4.zero);

        DesenYap("Assets/Resources/Textures/pattern.png");

        Debug.Log("TapStack: doku ayarlari uygulandi.");
    }

    static void SpriteYap(string yol, Vector4 kenar)
    {
        TextureImporter iceriAktarici = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (iceriAktarici == null)
        {
            Debug.LogWarning("TapStack: doku bulunamadi -> " + yol);
            return;
        }

        iceriAktarici.textureType = TextureImporterType.Sprite;
        iceriAktarici.spriteImportMode = SpriteImportMode.Single;
        iceriAktarici.spriteBorder = kenar;
        iceriAktarici.spritePixelsPerUnit = 100f;
        iceriAktarici.alphaIsTransparency = true;
        iceriAktarici.mipmapEnabled = false;
        iceriAktarici.filterMode = FilterMode.Bilinear;
        iceriAktarici.SaveAndReimport();
    }

    static void DesenYap(string yol)
    {
        TextureImporter iceriAktarici = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (iceriAktarici == null)
        {
            Debug.LogWarning("TapStack: doku bulunamadi -> " + yol);
            return;
        }

        iceriAktarici.textureType = TextureImporterType.Default;
        iceriAktarici.wrapMode = TextureWrapMode.Repeat;
        iceriAktarici.filterMode = FilterMode.Bilinear;
        iceriAktarici.mipmapEnabled = true;
        iceriAktarici.SaveAndReimport();
    }

    static void SahneyiOlustur()
    {
        if (!AssetDatabase.IsValidFolder(SahneKlasoru))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene sahne = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject oyunObjesi = new GameObject("Game");
        oyunObjesi.AddComponent<LevelSystem>();
        oyunObjesi.AddComponent<ThemeSystem>();
        oyunObjesi.AddComponent<StackGame>();

        EditorSceneManager.SaveScene(sahne, SahneYolu);

        EditorBuildSettingsScene[] sahneler = new EditorBuildSettingsScene[1];
        sahneler[0] = new EditorBuildSettingsScene(SahneYolu, true);
        EditorBuildSettings.scenes = sahneler;

        Debug.Log("TapStack: sahne olusturuldu -> " + SahneYolu);
    }

    static void AyarlariUygula()
    {
        PlayerSettings.companyName = "Mert Sadik";
        PlayerSettings.productName = "Tap Stack";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mertsadik.tapstack");

        // mobil oyun dikey calisiyor
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        Debug.Log("TapStack: player ayarlari uygulandi.");
    }

    // Blok kesme hesabinin dogru calistigini kontrol eder.
    // Komut satirindan: -executeMethod TapStackSetup.SelfTest
    [MenuItem("TapStack/Kesim Hesabini Test Et")]
    public static void SelfTest()
    {
        GameObject gecici = new GameObject("TestObjesi");
        StackGame oyun = gecici.AddComponent<StackGame>();

        int hata = 0;

        // 1) tam ortaya denk gelme -> mukemmel, blok kesilmiyor
        float[] a = oyun.KesimHesapla(0.05f, 3f);
        hata += Kontrol("mukemmel: devam", a[0], 1f);
        hata += Kontrol("mukemmel: bayrak", a[1], 1f);
        hata += Kontrol("mukemmel: yeni genislik", a[2], 3f);       // zaten tam boy, daha fazla buyuyemez
        hata += Kontrol("mukemmel: dusen parca yok", a[4], 0f);

        // 2) hic kesisme yok -> oyun biter
        float[] b = oyun.KesimHesapla(3.1f, 3f);
        hata += Kontrol("kacirma: devam etmemeli", b[0], 0f);

        // 3) saga 1.0 kayma -> 2 birim kaliyor, 1 birim dusuyor
        float[] c = oyun.KesimHesapla(1f, 3f);
        hata += Kontrol("sag kayma: yeni genislik", c[2], 2f);
        hata += Kontrol("sag kayma: merkez kaymasi", c[3], 0.5f);
        hata += Kontrol("sag kayma: dusen genislik", c[4], 1f);
        hata += Kontrol("sag kayma: dusen merkez", c[5], 2f);

        // 4) sola 1.0 kayma -> ayni sonuc, ters yonde
        float[] d = oyun.KesimHesapla(-1f, 3f);
        hata += Kontrol("sol kayma: yeni genislik", d[2], 2f);
        hata += Kontrol("sol kayma: merkez kaymasi", d[3], -0.5f);
        hata += Kontrol("sol kayma: dusen genislik", d[4], 1f);
        hata += Kontrol("sol kayma: dusen merkez", d[5], -2f);

        Object.DestroyImmediate(gecici);

        if (hata == 0)
        {
            Debug.Log("TapStack SelfTest: OK - butun testler gecti.");
        }
        else
        {
            Debug.LogError("TapStack SelfTest: BASARISIZ - " + hata + " test hatali.");
        }
    }

    static int Kontrol(string ad, float bulunan, float beklenen)
    {
        if (Mathf.Abs(bulunan - beklenen) < 0.001f)
        {
            Debug.Log("  OK   " + ad + " = " + bulunan);
            return 0;
        }
        Debug.LogError("  HATA " + ad + " = " + bulunan + " (beklenen " + beklenen + ")");
        return 1;
    }
}
