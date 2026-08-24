using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interaction_oso : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject teclaE;
    [SerializeField] private GameObject panelDialogo;
    [SerializeField] private TMP_Text textoDialogo;

    [Header("Diálogo")]
    [TextArea(2, 5)]
    [SerializeField] private string mensaje = "Bienvenido a la recepción.";

    private bool jugadorEnZona = false;
    private bool dialogoActivo = false;

    private void Start()
    {
        if (teclaE != null)
            teclaE.SetActive(false);

        if (panelDialogo != null)
            panelDialogo.SetActive(false);
    }

    private void Update()
    {
        // Si el diálogo está activo, esperar cualquier tecla para cerrarlo
        if (dialogoActivo)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                CerrarDialogo();
            }

            return;
        }

        // Si el jugador no está en la zona, no hacer nada
        if (!jugadorEnZona)
            return;

        // Presionar E para iniciar el diálogo
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            MostrarDialogo();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Algo entró al área: " + other.name);

        if (!other.CompareTag("Player"))
            return;

        Debug.Log("¡EL PLAYER ENTRÓ AL ÁREA!");

        jugadorEnZona = true;

        if (teclaE != null)
            teclaE.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("¡EL PLAYER SALIÓ DEL ÁREA!");

        jugadorEnZona = false;

        if (teclaE != null)
            teclaE.SetActive(false);

        // Si sale mientras el diálogo está abierto, también lo cerramos
        if (dialogoActivo)
        {
            CerrarDialogo();
        }
    }

    private void MostrarDialogo()
    {
        Debug.Log("¡SE PRESIONÓ E!");

        dialogoActivo = true;

        if (teclaE != null)
            teclaE.SetActive(false);

        if (textoDialogo != null)
            textoDialogo.text = mensaje;

        if (panelDialogo != null)
            panelDialogo.SetActive(true);
    }

    private void CerrarDialogo()
    {
        Debug.Log("Diálogo cerrado.");

        dialogoActivo = false;

        if (panelDialogo != null)
            panelDialogo.SetActive(false);

        if (textoDialogo != null)
            textoDialogo.text = "";

        // Volver a mostrar E si el jugador sigue dentro del área
        if (jugadorEnZona && teclaE != null)
            teclaE.SetActive(true);
    }
}