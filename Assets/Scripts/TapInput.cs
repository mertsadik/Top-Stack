using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Ekrana dokunuldu mu / tiklandi mi diye bakan kucuk yardimci.
// Projede eski Input Manager acik ama ileride yeni Input System'e gecilirse
// asagidaki ust kisim devreye girer, kod yine derlenir.
public class TapInput
{
    static List<RaycastResult> isinSonuclari = new List<RaycastResult>();

    public static bool Dokunuldu()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return !ArayuzeDokunulduMu(UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue());
        }
        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return !ArayuzeDokunulduMu(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        }
        return false;
#else
        if (Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }

        // Dokunmatik ekran: parmaklardan biri arayuz disina yeni dokunduysa sayilir.
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch dokunus = Input.GetTouch(i);
            if (dokunus.phase == TouchPhase.Began && !ArayuzeDokunulduMu(dokunus.position))
            {
                return true;
            }
        }

        // Dokunus yoksa fare (editor ve bilgisayar).
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && !ArayuzeDokunulduMu(Input.mousePosition))
        {
            return true;
        }

        return false;
#endif
    }

    // Android'in geri tusu Unity'ye Escape tusu olarak geliyor (bilgisayarda da Esc).
    public static bool GeriTusunaBasildi()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return UnityEngine.InputSystem.Keyboard.current != null &&
               UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    // Dokunulan noktada bir dugme ("Menü", "Reklam izle, devam et") var mi?
    //
    // EventSystem.IsPointerOverGameObject() parametresiz cagrilinca SADECE fareye
    // bakiyor; telefonda dokunuslari hic gormuyor. Parmak numarasiyla sormak da
    // yetmiyor, cunku dokunusun ilk karesinde EventSystem o parmagi henuz
    // islememis olabiliyor. Bu yuzden dokunulan noktaya arayuz isinini kendimiz
    // atiyoruz; hangi scriptin once calistigindan bagimsiz calisiyor.
    static bool ArayuzeDokunulduMu(Vector2 ekranKonumu)
    {
        EventSystem olaySistemi = EventSystem.current;
        if (olaySistemi == null)
        {
            return false;
        }

        PointerEventData veri = new PointerEventData(olaySistemi);
        veri.position = ekranKonumu;

        isinSonuclari.Clear();
        olaySistemi.RaycastAll(veri, isinSonuclari);
        return isinSonuclari.Count > 0;
    }
}
