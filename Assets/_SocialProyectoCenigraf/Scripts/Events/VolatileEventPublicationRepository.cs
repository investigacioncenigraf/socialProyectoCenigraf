using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.Events
{
    [DefaultExecutionOrder(-950)]
    [DisallowMultipleComponent]
    public sealed class VolatileEventPublicationRepository :
        MonoBehaviour,
        IEventPublicationRepository
    {
        private static VolatileEventPublicationRepository instance;
        private readonly List<EventPublication> publications =
            new List<EventPublication>();

        public static VolatileEventPublicationRepository Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject repositoryObject = new GameObject(
                        nameof(VolatileEventPublicationRepository));
                    instance = repositoryObject.AddComponent<
                        VolatileEventPublicationRepository>();
                }

                return instance;
            }
        }

        public IReadOnlyList<EventPublication> Publications => publications;
        public event Action PublicationsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            AddDefaultPublications();
        }

        private void AddDefaultPublications()
        {
            if (publications.Count > 0)
            {
                return;
            }

            IReadOnlyList<EventPublication> defaults =
                DefaultEventPublications.Create();
            for (int index = 0; index < defaults.Count; index++)
            {
                publications.Add(defaults[index]);
            }
        }

        public void Add(EventPublication publication)
        {
            if (publication == null)
            {
                throw new ArgumentNullException(nameof(publication));
            }

            publications.Add(publication);
            PublicationsChanged?.Invoke();
        }

        public bool UpdatePublication(EventPublication publication)
        {
            if (publication == null)
            {
                throw new ArgumentNullException(nameof(publication));
            }

            int index = publications.FindIndex(item => item.Id == publication.Id);
            if (index < 0)
            {
                return false;
            }

            publications[index] = publication;
            PublicationsChanged?.Invoke();
            return true;
        }

        public bool Remove(string publicationId)
        {
            int index = publications.FindIndex(item => item.Id == publicationId);
            if (index < 0)
            {
                return false;
            }

            publications.RemoveAt(index);
            PublicationsChanged?.Invoke();
            return true;
        }
    }
}
