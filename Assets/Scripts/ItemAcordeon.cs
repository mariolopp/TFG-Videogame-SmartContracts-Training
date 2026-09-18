using UnityEngine;

public class ItemAcordeon : MonoBehaviour
{
    [SerializeField] private GameObject miContenido;

    public void Alternar()
    {
        if (miContenido == null) return;

        // Comprobamos si este contenido en concreto ya estaba activo
        bool yaEstabaAbierto = miContenido.activeSelf;

        // 1. Buscamos todas las secciones hermanas dentro de PanelInfo y cerramos sus contenidos
        if (transform.parent != null)
        {
            ItemAcordeon[] todasLasSecciones = transform.parent.GetComponentsInChildren<ItemAcordeon>(true);
            foreach (var seccion in todasLasSecciones)
            {
                if (seccion.miContenido != null)
                {
                    seccion.miContenido.SetActive(false);
                }
            }
        }

        // 2. Si estaba cerrado, lo abrimos. Si ya estaba abierto, se queda cerrado.
        miContenido.SetActive(!yaEstabaAbierto);
    }
}