using System.Collections.Generic;

namespace SocialProyectoCenigraf.Events
{
    public interface IEventPublicationRepository
    {
        IReadOnlyList<EventPublication> Publications { get; }
        void Add(EventPublication publication);
        bool Update(EventPublication publication);
        bool Remove(string publicationId);
    }
}
