using System;

namespace SocialProyectoCenigraf.Events
{
    [Serializable]
    public sealed class EventPublication
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Link { get; }
        public DateTime PublicationStartsAt { get; }
        public DateTime PublicationEndsAt { get; }
        public string LocalImagePath { get; }
        public string CreatedByRoleId { get; }
        public DateTime CreatedAtUtc { get; }

        public EventPublication(
            string id,
            string title,
            string description,
            string link,
            DateTime publicationStartsAt,
            DateTime publicationEndsAt,
            string localImagePath,
            string createdByRoleId,
            DateTime createdAtUtc)
        {
            Id = id;
            Title = title;
            Description = description;
            Link = link;
            PublicationStartsAt = publicationStartsAt;
            PublicationEndsAt = publicationEndsAt;
            LocalImagePath = localImagePath;
            CreatedByRoleId = createdByRoleId;
            CreatedAtUtc = createdAtUtc;
        }

        public EventPublication WithUpdatedContent(
            string title,
            string description,
            string link,
            DateTime publicationStartsAt,
            DateTime publicationEndsAt,
            string localImagePath)
        {
            return new EventPublication(
                Id,
                title,
                description,
                link,
                publicationStartsAt,
                publicationEndsAt,
                localImagePath,
                CreatedByRoleId,
                CreatedAtUtc);
        }
    }
}
