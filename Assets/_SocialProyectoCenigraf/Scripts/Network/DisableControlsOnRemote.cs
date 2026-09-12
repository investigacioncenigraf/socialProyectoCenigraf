using SocialProyectoCenigraf.Player.Movement;
using UnityEngine;

/* 
 * ===================================================================================
 * GUÍA DE FUNCIONAMIENTO Y USO: DisableControlsOnRemote.cs
 * ===================================================================================
 * 
 * ¿QUÉ HACE ESTE SCRIPT?
 * Se encarga EXCLUSIVAMENTE de desactivar la lógica de control local en las 
 * copias (prefabs) de otros jugadores que se crean al recibir señal de red.
 * 
 * PASOS QUE EJECUTA:
 * 1. Desactiva el script 'PlayerMovementController' para que el jugador remoto NO
 *    escuche los eventos del teclado/mouse del cliente local.
 * 2. Cambia el Rigidbody2D a tipo 'Kinematic' para que la física local no 
 *    interfiera con las coordenadas enviadas por el servidor.
 * 
 * INSTRUCCIONES DE USO:
 * Invócalo desde tu NetworkManager inmediatamente después de instanciar un remoto:
 * DisableControlsOnRemote.DisableControls(remotePlayerObject);
 * ===================================================================================
 */

public class DisableControlsOnRemote : MonoBehaviour
{
    /// <summary>
    /// Desactiva el script de movimiento y ajusta la física del objeto remoto recibido.
    /// </summary>
    /// <param name="remotePlayerObject">El GameObject instanciado para el jugador remoto.</param>
    public static void DisableControls(GameObject remotePlayerObject)
    {
        if (remotePlayerObject == null) return;

        // 1. Deshabilitar el script de movimiento local (PlayerMovementController)
        PlayerMovementController movementController = remotePlayerObject.GetComponent<PlayerMovementController>();
        if (movementController != null)
        {
            movementController.enabled = false;
            Debug.Log($"[DESHABILITADOR] PlayerMovementController desactivado en: {remotePlayerObject.name}");
        }

        // 2. Cambiar Rigidbody2D a Kinematic para evitar desplazamientos por física
        Rigidbody2D rb = remotePlayerObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }
}