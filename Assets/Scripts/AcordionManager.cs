using UnityEngine;

public class AccordionManager : MonoBehaviour
{
    [SerializeField] private AccordionSection[] secciones;

    private void Awake()
    {
        foreach (var seccion in secciones)
            seccion.OnAbierto += CerrarLasDemas;
    }

    private void CerrarLasDemas(AccordionSection seccionAbierta)
    {
        foreach (var seccion in secciones)
        {
            if (seccion != seccionAbierta && seccion.EstaAbierto)
                seccion.Plegar();
        }
    }
}