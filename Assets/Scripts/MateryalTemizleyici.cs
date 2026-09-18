using UnityEngine;

// renderer.material her blok icin materyalin bir KOPYASINI olusturuyor.
// Unity bu kopyayi blok silinince kendisi silmiyor; seviye seviye oynandikca
// binlerce materyal birikip telefonun bellegini doldurur.
// Bu bilesen blok silinirken kendi materyal kopyasini da siliyor.
public class MateryalTemizleyici : MonoBehaviour
{
    void OnDestroy()
    {
        Renderer ciz = GetComponent<Renderer>();
        if (ciz != null && ciz.sharedMaterial != null)
        {
            Destroy(ciz.sharedMaterial);
        }
    }
}
