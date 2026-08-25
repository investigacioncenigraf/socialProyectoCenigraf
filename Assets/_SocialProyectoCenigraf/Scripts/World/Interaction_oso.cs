using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Interaction_oso : MonoBehaviour
{
    [Serializable]
    public class LineaDialogo
    {
        [Header("Personaje")]
        public string nombre;

        public Sprite retrato;

        [Header("Texto")]
        [TextArea(2, 5)]
        public string texto;
    }

    [Header("UI")]
    [SerializeField] private GameObject teclaE;
    [SerializeField] private GameObject panelDialogo;

    [SerializeField] private TMP_Text nombrePersonaje;
    [SerializeField] private TMP_Text textoDialogo;

    [SerializeField] private Image retratoPersonaje;

    [Header("Diálogo")]
    [SerializeField] private LineaDialogo[] dialogos;

    private bool jugadorEnZona = false;
    private bool dialogoActivo = false;

    private int dialogoActual = 0;

    private void Start()
    {
        if (teclaE != null)
            teclaE.SetActive(false);

        if (panelDialogo != null)
            panelDialogo.SetActive(false);
    }

    private void Update()
    {
        // Si estamos dentro de un diálogo
        if (dialogoActivo)
        {
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                SiguienteDialogo();
            }

            return;
        }

        // Si el jugador no está en la zona
        if (!jugadorEnZona)
            return;

        // E para comenzar el diálogo
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            IniciarDialogo();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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

        // Cerrar el diálogo si el jugador se aleja
        if (dialogoActivo)
        {
            CerrarDialogo();
        }
    }

    private void IniciarDialogo()
    {
        if (dialogos == null || dialogos.Length == 0)
        {
            Debug.LogWarning("No hay diálogos configurados.");
            return;
        }

        Debug.Log("¡COMENZÓ EL DIÁLOGO!");

        dialogoActivo = true;
        dialogoActual = 0;

        if (teclaE != null)
            teclaE.SetActive(false);

        if (panelDialogo != null)
            panelDialogo.SetActive(true);

        MostrarDialogoActual();
    }

    private void SiguienteDialogo()
    {
        dialogoActual++;

        // Si ya no quedan líneas
        if (dialogoActual >= dialogos.Length)
        {
            CerrarDialogo();
            return;
        }

        MostrarDialogoActual();
    }

    private void MostrarDialogoActual()
    {
        LineaDialogo linea = dialogos[dialogoActual];

        // Nombre
        if (nombrePersonaje != null)
            nombrePersonaje.text = linea.nombre;

        // Texto
        if (textoDialogo != null)
            textoDialogo.text = linea.texto;

        // Retrato
        if (retratoPersonaje != null)
        {
            retratoPersonaje.sprite = linea.retrato;

            // Mostrar la imagen
            retratoPersonaje.gameObject.SetActive(linea.retrato != null);
        }
    }

    private void CerrarDialogo()
    {
        Debug.Log("Diálogo terminado.");

        dialogoActivo = false;

        if (panelDialogo != null)
            panelDialogo.SetActive(false);

        if (textoDialogo != null)
            textoDialogo.text = "";

        if (nombrePersonaje != null)
            nombrePersonaje.text = "";

        if (retratoPersonaje != null)
            retratoPersonaje.sprite = null;

        // Mostrar E nuevamente si el jugador sigue dentro
        if (jugadorEnZona && teclaE != null)
            teclaE.SetActive(true);
    }
}
