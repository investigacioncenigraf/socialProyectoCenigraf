using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * ===================================================================================
 * DIRECTOR CENTRAL DE RED (NetworkManager.cs)
 * ===================================================================================
 * 
 * ¿QUÉ HACE ESTE SCRIPT?
 * Es el orquestador central. Conecta el módulo de red ('NetworkClient') con los objetos 
 * de la escena (Jugador Local y Prefabs Remotos).
 * 
 * RESPONSABILIDADES:
 * 1. Asignar un ID único a esta sesión.
 * 2. Enviar periódicamente la posición de nuestro jugador local.
 * 3. Crear avatares para nuevos jugadores conectados.
 * 4. Usar 'DisableControlsOnRemote' para quitar los controles de entrada a avatares remotos.
 * 
 * INSTRUCCIONES DE USO:
 * 1. Adjunta este script al objeto 'NetworkManager' de la escena.
 * 2. Asigna la referencia del componente 'NetworkClient' en el Inspector.
 * 3. Asigna tu objeto 'Local Player' en la escena y tu 'Player Prefab'.
 * ===================================================================================
 */

public class NetworkManager : MonoBehaviour
{
    [Header("Módulos")]
    [Tooltip("Referencia al script NetworkClient de este mismo GameObject")]
    public NetworkClient networkClient;

    [Header("Referencias de Escena")]
    [Tooltip("El objeto de tu jugador en la escena")]
    public Transform localPlayer;

    [Tooltip("Prefab del jugador que representará a otros clientes")]
    public GameObject playerPrefab;

    [Header("Ajustes de Sincronización")]
    [Tooltip("Intervalo de envío en segundos (0.05s = 20 veces por segundo)")]
    public float sendInterval = 0.05f;

    private string myPlayerId;
    private readonly Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();

    private void Start()
    {
        if (networkClient == null)
        {
            Debug.LogError("[NETWORK MANAGER] ¡Falta la referencia a NetworkClient en el Inspector!");
            return;
        }

        // 1. Generar ID único para este cliente (8 caracteres aleatorios)
        myPlayerId = Guid.NewGuid().ToString().Substring(0, 8);

        // 2. Suscribirse al evento de recepción de datos de red
        networkClient.OnPositionReceived += HandlePositionReceived;

        // 3. Inicializar el socket UDP
        networkClient.Initialize();

        // 4. Iniciar envío recurrente de nuestra posición
        InvokeRepeating(nameof(SendMyPosition), sendInterval, sendInterval);
    }

    private void SendMyPosition()
    {
        if (localPlayer != null)
        {
            networkClient.SendPosition(myPlayerId, localPlayer.position);
        }
    }

    private void HandlePositionReceived(string id, Vector3 position)
    {
        // Descartar si el paquete recibido pertenece a nosotros mismos
        if (id == myPlayerId) return;

        // Si es un jugador nuevo que no teníamos registrado
        if (!remotePlayers.ContainsKey(id))
        {
            // Instanciar avatar remoto en pantalla
            GameObject newRemotePlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            newRemotePlayer.name = $"RemotePlayer_{id}";

            // DESHABILITAR CONTROLES: Usar el módulo especializado que creamos
            DisableControlsOnRemote.DisableControls(newRemotePlayer);

            // Guardar en el registro de jugadores activos
            remotePlayers.Add(id, newRemotePlayer);
            Debug.Log($"[NETWORK MANAGER] Nuevo jugador remoto registrado con ID: {id}");
        }
        else
        {
            // Si el jugador ya existe, actualizar su posición
            if (remotePlayers[id] != null)
            {
                remotePlayers[id].transform.position = position;
            }
        }
    }

    private void OnDestroy()
    {
        if (networkClient != null)
        {
            networkClient.OnPositionReceived -= HandlePositionReceived;
        }
    }
}