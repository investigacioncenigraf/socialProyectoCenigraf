using System.Collections.Generic;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;

namespace Networking.Runtime.Controllers
{
    /// <summary>
    /// Administrador de instanciación y ciclo de vida de avatares en red para Unity.
    /// Se suscribe a los eventos de presencia de <see cref="NetworkManager"/> para crear y destruir
    /// automáticamente los avatares remotos (con isLocalPlayer = false) y opcionalmente instanciar al jugador local.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerSpawner : MonoBehaviour
    {
        [Header("Prefabs de Jugador")]
        [Tooltip("Prefab del jugador remoto. Debe contener el componente PlayerNetworkSync.")]
        [SerializeField] private GameObject _remotePlayerPrefab;

        [Tooltip("Prefab del jugador local (opcional). Solo se utiliza si spawnLocalPlayerAutomatically es true.")]
        [SerializeField] private GameObject _localPlayerPrefab;

        [Header("Configuración de Escena")]
        [Tooltip("Transform contenedor padre donde se organizarán los avatares instanciados.")]
        [SerializeField] private Transform _spawnParent;

        [Tooltip("Instanciar automáticamente al jugador local cuando la autenticación sea exitosa.")]
        [SerializeField] private bool _spawnLocalPlayerAutomatically = false;

        private readonly Dictionary<string, GameObject> _spawnedRemotePlayers = new Dictionary<string, GameObject>();
        private GameObject _spawnedLocalPlayer;

        /// <summary>
        /// Prefab utilizado para instanciar a los jugadores remotos.
        /// </summary>
        public GameObject RemotePlayerPrefab
        {
            get => _remotePlayerPrefab;
            set => _remotePlayerPrefab = value;
        }

        /// <summary>
        /// Prefab utilizado para instanciar al jugador local.
        /// </summary>
        public GameObject LocalPlayerPrefab
        {
            get => _localPlayerPrefab;
            set => _localPlayerPrefab = value;
        }

        /// <summary>
        /// Diccionario de sólo lectura con los avatares remotos actualmente presentes en la escena.
        /// </summary>
        public IReadOnlyDictionary<string, GameObject> SpawnedRemotePlayers => _spawnedRemotePlayers;

        /// <summary>
        /// Instancia del jugador local instanciada dinámicamente, si aplica.
        /// </summary>
        public GameObject SpawnedLocalPlayer => _spawnedLocalPlayer;

        private bool _isRegistered = false;

        private void OnEnable()
        {
            RegisterNetworkEvents();
        }

        private void Start()
        {
            RegisterNetworkEvents();
        }

        private void RegisterNetworkEvents()
        {
            if (_isRegistered) return;

            if (NetworkManager.Instance == null)
            {
                return;
            }

            if (NetworkManager.Instance.Auth != null)
            {
                NetworkManager.Instance.Auth.OnUserListSynchronized += HandleUserListSynchronized;
                NetworkManager.Instance.Auth.OnUserJoined += HandleUserJoined;
                NetworkManager.Instance.Auth.OnUserLeft += HandleUserLeft;
                NetworkManager.Instance.Auth.OnAuthCompleted += HandleAuthCompleted;
            }

            NetworkManager.Instance.OnDisconnected += HandleDisconnected;
            _isRegistered = true;
        }

        private void UnregisterNetworkEvents()
        {
            if (!_isRegistered || NetworkManager.Instance == null) return;

            if (NetworkManager.Instance.Auth != null)
            {
                NetworkManager.Instance.Auth.OnUserListSynchronized -= HandleUserListSynchronized;
                NetworkManager.Instance.Auth.OnUserJoined -= HandleUserJoined;
                NetworkManager.Instance.Auth.OnUserLeft -= HandleUserLeft;
                NetworkManager.Instance.Auth.OnAuthCompleted -= HandleAuthCompleted;
            }

            NetworkManager.Instance.OnDisconnected -= HandleDisconnected;
            _isRegistered = false;
        }

        private void OnDisable()
        {
            UnregisterNetworkEvents();
        }

        private void HandleAuthCompleted(AuthResponsePayload response)
        {
            if (!response.success || !_spawnLocalPlayerAutomatically || _localPlayerPrefab == null)
            {
                return;
            }

            if (_spawnedLocalPlayer != null)
            {
                Destroy(_spawnedLocalPlayer);
            }

            Vector3 spawnPosition = response.spawnPosition.ToVector3();
            _spawnedLocalPlayer = Instantiate(_localPlayerPrefab, spawnPosition, Quaternion.identity, _spawnParent);
            _spawnedLocalPlayer.name = $"LocalPlayer_{response.username}";

            var sync = _spawnedLocalPlayer.GetComponent<PlayerNetworkSync>();
            if (sync != null)
            {
                sync.IsLocalPlayer = true;
            }
            else
            {
                Debug.LogWarning("[NetworkPlayerSpawner] El prefab del jugador local no contiene PlayerNetworkSync.");
            }

            Debug.Log($"[NetworkPlayerSpawner] Jugador local instanciado en {spawnPosition}.");
        }

        private void HandleUserListSynchronized(UserListSyncPayload payload)
        {
            if (payload?.users == null) return;

            string localId = NetworkManager.Instance != null ? NetworkManager.Instance.LocalUserId : null;

            foreach (var user in payload.users)
            {
                if (user == null) continue;

                // Evitar instanciar una réplica remota del propio jugador local
                if (!string.IsNullOrEmpty(localId) && user.userId == localId)
                {
                    continue;
                }

                SpawnRemotePlayer(user);
            }
        }

        private void HandleUserJoined(UserSyncPayload user)
        {
            if (user == null) return;

            string localId = NetworkManager.Instance != null ? NetworkManager.Instance.LocalUserId : null;
            if (!string.IsNullOrEmpty(localId) && user.userId == localId)
            {
                return;
            }

            SpawnRemotePlayer(user);
        }

        private void HandleUserLeft(UserSyncPayload user)
        {
            if (user == null || string.IsNullOrEmpty(user.userId)) return;

            DespawnRemotePlayer(user.userId);
        }

        private void HandleDisconnected(string reason)
        {
            ClearAllRemotePlayers();

            if (_spawnLocalPlayerAutomatically && _spawnedLocalPlayer != null)
            {
                Destroy(_spawnedLocalPlayer);
                _spawnedLocalPlayer = null;
            }
        }

        /// <summary>
        /// Instancia un avatar para un usuario remoto con el componente PlayerNetworkSync configurado en modo remoto.
        /// </summary>
        /// <param name="user">Carga útil con los datos del usuario remoto.</param>
        /// <returns>La instancia del GameObject creada.</returns>
        public GameObject SpawnRemotePlayer(UserSyncPayload user)
        {
            if (_remotePlayerPrefab == null)
            {
                Debug.LogError("[NetworkPlayerSpawner] Imposible instanciar jugador remoto: _remotePlayerPrefab no está asignado.");
                return null;
            }

            if (_spawnedRemotePlayers.ContainsKey(user.userId))
            {
                Debug.LogWarning($"[NetworkPlayerSpawner] El jugador remoto {user.username} ({user.userId}) ya está instanciado.");
                return _spawnedRemotePlayers[user.userId];
            }

            Vector3 spawnPos = user.position.ToVector3();
            GameObject remotePlayerInstance = Instantiate(_remotePlayerPrefab, spawnPos, Quaternion.identity, _spawnParent);
            remotePlayerInstance.name = $"RemotePlayer_{user.username}_{user.userId}";

            var sync = remotePlayerInstance.GetComponent<PlayerNetworkSync>();
            if (sync != null)
            {
                sync.IsLocalPlayer = false;
                sync.RemoteUserId = user.userId;
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayerSpawner] El prefab remoto '{_remotePlayerPrefab.name}' no contiene PlayerNetworkSync.");
            }

            _spawnedRemotePlayers.Add(user.userId, remotePlayerInstance);
            Debug.Log($"[NetworkPlayerSpawner] Jugador remoto instanciado: {user.username} ({user.userId}) en {spawnPos}.");

            return remotePlayerInstance;
        }

        /// <summary>
        /// Destruye la instancia del jugador remoto correspondiente al userId dado.
        /// </summary>
        /// <param name="userId">Identificador del usuario remoto.</param>
        /// <returns>True si el avatar fue encontrado y destruido; de lo contrario false.</returns>
        public bool DespawnRemotePlayer(string userId)
        {
            if (_spawnedRemotePlayers.TryGetValue(userId, out var playerObject))
            {
                if (playerObject != null)
                {
                    Destroy(playerObject);
                }

                _spawnedRemotePlayers.Remove(userId);
                Debug.Log($"[NetworkPlayerSpawner] Jugador remoto destruido: {userId}.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Limpia y destruye todos los avatares remotos activos actualmente en la escena.
        /// </summary>
        public void ClearAllRemotePlayers()
        {
            foreach (var kvp in _spawnedRemotePlayers)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }

            _spawnedRemotePlayers.Clear();
            Debug.Log("[NetworkPlayerSpawner] Todos los jugadores remotos han sido destruidos.");
        }

        private void OnDestroy()
        {
            UnregisterNetworkEvents();
            ClearAllRemotePlayers();
        }
    }
}
