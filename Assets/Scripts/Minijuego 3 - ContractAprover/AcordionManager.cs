using UnityEngine;

public class AccordionManager : MonoBehaviour
{
    [SerializeField] private AccordionSection[] secciones;

    private void Awake()
    {
        foreach (var seccion in secciones)
            seccion.OnAbierto += CloseOthers;
    }

    private void CloseOthers(AccordionSection openedSection)
    {
        foreach (var seccion in secciones)
        {
            if (seccion != openedSection && seccion.EstaAbierto)
                seccion.Plegar();
        }
    }
}