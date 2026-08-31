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

    [Header("Diálogo inicial")]
    [SerializeField] private LineaDialogo[] dialogos;

    [Header("Diálogo después de obtener el carnet")]
    [SerializeField] private LineaDialogo[] dialogosConCarnet;

    [Header("Recompensa")]
    [SerializeField] private GameObject carnetID;

    private bool jugadorEnZona = false;
    private bool dialogoActivo = false;

    private int dialogoActual = 0;

    // Indica si el jugador ya recibió el carnet durante esta partida
    private bool tieneCarnet = false;

    private void Start()
    {
        // Al comenzar una partida nueva NO tiene el carnet
        tieneCarnet = false;

        // Ocultar tecla E al comenzar
        if (teclaE != null)
            teclaE.SetActive(false);

        // Ocultar panel de diálogo
        if (panelDialogo != null)
            panelDialogo.SetActive(false);

        // Ocultar carnet ID
        if (carnetID != null)
            carnetID.SetActive(false);
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
        // IMPORTANTE: aquí NO mostramos el carnet
        if (dialogoActivo)
        {
            CerrarDialogo(false);
        }
    }

    private void IniciarDialogo()
    {
        // Elegir qué conjunto de diálogos utilizar
        LineaDialogo[] dialogosActuales;

        if (tieneCarnet)
        {
            dialogosActuales = dialogosConCarnet;
        }
        else
        {
            dialogosActuales = dialogos;
        }

        // Comprobar que existen diálogos
        if (dialogosActuales == null || dialogosActuales.Length == 0)
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

        // Elegir qué conjunto de diálogos estamos utilizando
        LineaDialogo[] dialogosActuales;

        if (tieneCarnet)
        {
            dialogosActuales = dialogosConCarnet;
        }
        else
        {
            dialogosActuales = dialogos;
        }

        // Si ya no quedan líneas
        if (dialogoActual >= dialogosActuales.Length)
        {
            // Terminó normalmente
            CerrarDialogo(true);
            return;
        }

        MostrarDialogoActual();
    }

    private void MostrarDialogoActual()
    {
        // Elegir el diálogo correspondiente
        LineaDialogo[] dialogosActuales;

        if (tieneCarnet)
        {
            dialogosActuales = dialogosConCarnet;
        }
        else
        {
            dialogosActuales = dialogos;
        }

        LineaDialogo linea = dialogosActuales[dialogoActual];

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

            // Mostrar la imagen solo si existe un retrato
            retratoPersonaje.gameObject.SetActive(linea.retrato != null);
        }
    }

    private void CerrarDialogo(bool mostrarCarnet)
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
        {
            retratoPersonaje.sprite = null;
            retratoPersonaje.gameObject.SetActive(false);
        }

        // Mostrar el carnet SOLO la primera vez
        if (mostrarCarnet && !tieneCarnet && carnetID != null)
        {
            carnetID.SetActive(true);

            Debug.Log("¡CARNET ID DESBLOQUEADO!");

            // Marcar que ya recibió el carnet
            // Esto dura solamente durante esta partida
            tieneCarnet = true;
        }

        // Mostrar E nuevamente si el jugador sigue dentro
        if (jugadorEnZona && teclaE != null)
            teclaE.SetActive(true);
    }
}
