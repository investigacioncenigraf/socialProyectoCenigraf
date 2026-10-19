using System.Collections;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;

namespace Networking.Runtime.Controllers
{
    /// <summary>
    /// Componente MonoBehaviour para la sincronización periódica de posición en red de un jugador en Unity.
    /// Soporta envío de posición local con frecuencia configurable (*Tick Rate*) e interpolación suave (*Lerp*) para jugadores remotos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerNetworkSync : MonoBehaviour
    {
        [Header("Configuración del Jugador")]
        [Tooltip("Indica si este GameObject representa al jugador local controlado por la máquina del usuario.")]
        [SerializeField] private bool _isLocalPlayer = true;

        [Tooltip("Identificador único del usuario remoto en caso de que isLocalPlayer sea falso.")]
        [SerializeField] private string _remoteUserId;

        [Header("Parámetros de Replicación de Red")]
        [Tooltip("Frecuencia de sincronización de posición por segundo (Hz). Por defecto 20 Hz (cada 50 ms).")]
        [SerializeField] private float _syncRateHz = 20f;

        [Tooltip("Distancia mínima recorrida en metros para disparar un paquete de red.")]
        [SerializeField] private float _minimumMovementThreshold = 0.01f;

        [Tooltip("Velocidad de interpolación suave (Lerp) para jugadores remotos.")]
        [SerializeField] private float _interpolationSpeed = 15f;

        private Vector3 _lastSentPosition;
        private Vector3 _targetRemotePosition;
        private Coroutine _syncCoroutine;

        /// <summary>
        /// Obtiene o establece si este GameObject corresponde al jugador local.
        /// </summary>
        public bool IsLocalPlayer
        {
            get => _isLocalPlayer;
            set => _isLocalPlayer = value;
        }

        /// <summary>
        /// Obtiene o establece el identificador remoto asignado a este avatar.
        /// </summary>
        public string RemoteUserId
        {
            get => _remoteUserId;
            set => _remoteUserId = value;
        }

        private void Start()
        {
            _lastSentPosition = transform.position;
            _targetRemotePosition = transform.position;

            if (_isLocalPlayer)
            {
                _syncCoroutine = StartCoroutine(LocalPlayerSyncLoop());
            }
            else
            {
                if (NetworkManager.Instance != null && NetworkManager.Instance.Position != null)
                {
                    NetworkManager.Instance.Position.OnRemotePositionReceived += HandleRemotePositionReceived;
                }
            }
        }

        private void Update()
        {
            // Para jugadores remotos, se interpola suavemente la posición para eliminar tirones (Jitter)
            if (!_isLocalPlayer)
            {
                transform.position = Vector3.Lerp(transform.position, _targetRemotePosition, Time.deltaTime * _interpolationSpeed);
            }
        }

        private IEnumerator LocalPlayerSyncLoop()
        {
            var wait = new WaitForSeconds(1f / Mathf.Max(1f, _syncRateHz));

            while (true)
            {
                yield return wait;

                if (NetworkManager.Instance == null || NetworkManager.Instance.State != Core.NetworkClientState.Connected)
                {
                    continue;
                }

                Vector3 currentPos = transform.position;
                if (Vector3.Distance(currentPos, _lastSentPosition) >= _minimumMovementThreshold)
                {
                    _lastSentPosition = currentPos;
                    _ = NetworkManager.Instance.SendPositionUpdateAsync(currentPos);
                }
            }
        }

        private void HandleRemotePositionReceived(PositionUpdatePayload payload)
        {
            if (_isLocalPlayer || payload.userId != _remoteUserId)
            {
                return;
            }

            _targetRemotePosition = payload.position.ToVector3();
        }

        private void OnDestroy()
        {
            if (_syncCoroutine != null)
            {
                StopCoroutine(_syncCoroutine);
            }

            if (!_isLocalPlayer && NetworkManager.Instance != null && NetworkManager.Instance.Position != null)
            {
                NetworkManager.Instance.Position.OnRemotePositionReceived -= HandleRemotePositionReceived;
            }
        }
    }
}
