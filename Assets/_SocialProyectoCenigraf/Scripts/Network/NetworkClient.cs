using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/*
 * ===================================================================================
 * MÓDULO DE RED UDP (NetworkClient.cs)
 * ===================================================================================
 * 
 * ¿QUÉ HACE ESTE SCRIPT?
 * Maneja EXCLUSIVAMENTE la conexión de red por UDP. Se encarga de empaquetar 
 * posiciones en formato 'ID|X|Y|Z', enviarlas al servidor y recibir las 
 * posiciones de otros jugadores.
 * 
 * ¿CÓMO SE COMUNICA CON OTROS SCRIPTS?
 * No conoce nada sobre Prefabs, cámaras ni transformadas. Cuando recibe datos 
 * válidos del backend, dispara el evento público 'OnPositionReceived'. 
 * El 'NetworkManager' se suscribe a este evento para reaccionar.
 * 
 * INSTRUCCIONES DE USO:
 * 1. Adjunta este script a tu GameObject 'NetworkManager'.
 * 2. Asigna la IP y el Puerto del Servidor en el Inspector.
 * 3. Llama a 'Initialize()' desde el script central (NetworkManager).
 * ===================================================================================
 */

public class NetworkClient : MonoBehaviour
{
    [Header("Configuración de Servidor UDP")]
    [Tooltip("Usa '127.0.0.1' para Docker local. Cambia a la IP pública cuando despliegues en Azure.")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 7777;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    // Cola para transferir datos desde el hilo secundario (Sockets) al Main Thread de Unity
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    // Evento que notifica al orquestador cuando llega una posición recibida: (string id, Vector3 posicion)
    public event Action<string, Vector3> OnPositionReceived;

    /// <summary>
    /// Inicia la conexión UDP y comienza a escuchar paquetes en segundo plano.
    /// </summary>
    public void Initialize()
    {
        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

            // Iniciar escucha asíncrona de paquetes
            udpClient.BeginReceive(OnDataReceived, null);
            Debug.Log($"[RED UDP] Cliente iniciado escuchando hacia {serverIP}:{serverPort}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RED UDP] Error al inicializar el cliente UDP: {ex.Message}");
        }
    }

    private void Update()
    {
        // Procesar las acciones encoladas en el Main Thread de Unity
        while (mainThreadActions.Count > 0)
        {
            Action actionToExecute = null;
            lock (mainThreadActions)
            {
                if (mainThreadActions.Count > 0)
                {
                    actionToExecute = mainThreadActions.Dequeue();
                }
            }
            actionToExecute?.Invoke();
        }
    }

    /// <summary>
    /// Envía la posición del jugador local al servidor backend.
    /// </summary>
    public void SendPosition(string playerId, Vector3 position)
    {
        if (udpClient == null) return;

        try
        {
            // Formato de cadena: ID|X|Y|Z
            string payload = $"{playerId}|{position.x:F2}|{position.y:F2}|{position.z:F2}";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            udpClient.Send(bytes, bytes.Length, serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RED UDP] Error al enviar posición: {ex.Message}");
        }
    }

    private void OnDataReceived(IAsyncResult ar)
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] bytes = udpClient.EndReceive(ar, ref remoteEP);
            string message = Encoding.UTF8.GetString(bytes);

            // Desglosar la cadena recibida (ID|X|Y|Z)
            string[] data = message.Split('|');
            if (data.Length == 4)
            {
                string id = data[0];
                float x = float.Parse(data[1]);
                float y = float.Parse(data[2]);
                float z = float.Parse(data[3]);
                Vector3 receivedPosition = new Vector3(x, y, z);

                // Encolar notificación para el hilo principal
                lock (mainThreadActions)
                {
                    mainThreadActions.Enqueue(() => OnPositionReceived?.Invoke(id, receivedPosition));
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Ocurre al cerrar el UdpClient limpiamente al salir del juego
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RED UDP] Error al recibir paquete: {ex.Message}");
        }

        // Seguir escuchando paquetes si el cliente está activo
        if (udpClient != null)
        {
            try
            {
                udpClient.BeginReceive(OnDataReceived, null);
            }
            catch { }
        }
    }

    private void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}