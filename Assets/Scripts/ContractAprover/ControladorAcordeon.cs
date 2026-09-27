using UnityEngine;

public class SeccionAcordeon : MonoBehaviour
{
    [Header("Panel que se debe ocultar")]
    [SerializeField] private GameObject contenido;

    public void Alternar()
    {
        if (contenido == null) return;

        bool abrir = !contenido.activeSelf;

        // Buscar todas las secciones hermanas dentro del mismo menú y cerrar sus contenidos
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

        // Si este panel estaba cerrado, lo abrimos
        contenido.SetActive(abrir);
    }
}