using TMPro;
using UnityEngine;

// Barra del bloque: igual que BarraProgreso pero medida en unidades de gas
public class BarraGas : BarraProgreso
{
    public int unidadesMax = 20; // Unidades de gas que caben en el bloque
    [SerializeField] private TextMeshProUGUI textoUnidades;

    protected override void Update()
    {
        base.Update();
        ActualizarTextoUnidades();
    }

    public override void Resetear()
    {
        base.Resetear();
        ActualizarTextoUnidades();
    }

    // El contador sigue al relleno visible para que suba a la vez que la barra
    public void ActualizarTextoUnidades()
    {
        if (textoUnidades == null) return;
        int unidades = Mathf.RoundToInt(barra.fillAmount * unidadesMax);
        textoUnidades.text = unidades + "/" + unidadesMax + " <sprite=\"gas\" index=0 color=#FFFFFFB3>"; // Icono al 70% de opacidad
    }
}
