using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Networking.Runtime.Core
{
    /// <summary>
    /// Despachador seguro de subprocesos para Unity que encola acciones invocadas desde hilos secundarios
    /// (como la recepción asíncrona de WebSockets) y las ejecuta en el hilo principal de Unity durante el ciclo Update.
    /// </summary>
    public sealed class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }

        /// <summary>
        /// Obtiene la instancia singleton activa del despachador, garantizando existencia en el hilo principal.
        /// </summary>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Procesa todas las acciones encoladas en este fotograma
            while (_executionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Excepción al ejecutar acción en hilo principal: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Encola una acción para ser ejecutada en el hilo principal de Unity en el próximo fotograma disponible.
        /// </summary>
        /// <param name="action">Acción sin retorno que interactúa con la API de Unity.</param>
        public void Enqueue(Action action)
        {
            if (action == null) return;
            _executionQueue.Enqueue(action);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
