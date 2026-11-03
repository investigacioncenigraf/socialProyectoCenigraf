using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.Controls
{
    public static class GameplayInputLock
    {
        private static readonly HashSet<int> Owners = new HashSet<int>();

        public static bool IsLocked => Owners.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Owners.Clear();
        }

        public static void Acquire(Object owner)
        {
            if (owner != null)
            {
                Owners.Add(owner.GetInstanceID());
            }
        }

        public static void Release(Object owner)
        {
            if (owner != null)
            {
                Owners.Remove(owner.GetInstanceID());
            }
        }
    }
}
