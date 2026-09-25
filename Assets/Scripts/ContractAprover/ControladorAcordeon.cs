using UnityEngine;

public class SeccionAcordeon : MonoBehaviour
{
    [Header("Arrastra solo el panel que se debe ocultar")]
    [SerializeField] private GameObject contenido;

    public void Alternar()
    {
        if (contenido == null) return;

        bool abrir = !contenido.activeSelf;

        // 1. Busca todas las secciones hermanas dentro del mismo menú y cierra sus contenidos
        if (transform.parent != null)
        {
            SeccionAcordeon[] todasLasSecciones = transform.parent.GetComponentsInChildren<SeccionAcordeon>(true);
            foreach (var sec in todasLasSecciones)
            {
                if (sec.contenido != null)
                {
                    sec.contenido.SetActive(false);
                }
            }
        }

        // 2. Si este panel estaba cerrado, lo abrimos
        contenido.SetActive(abrir);
    }
}