using System;
using System.Globalization;

namespace SocialProyectoCenigraf.Events
{
    public static class EventPublicationValidator
    {
        public const string DateFormat = "dd/MM/yyyy";

        public static bool TryCreate(
            string title,
            string description,
            string link,
            string startsAt,
            string endsAt,
            string imagePath,
            string createdByRoleId,
            out EventPublication publication,
            out string error)
        {
            publication = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                error = "El título del evento es obligatorio.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                error = "La descripción del evento es obligatoria.";
                return false;
            }

            if (!TryParseDate(startsAt, out DateTime start))
            {
                error = $"La fecha inicial debe usar el formato {DateFormat}.";
                return false;
            }

            if (!TryParseDate(endsAt, out DateTime end))
            {
                error = $"La fecha final debe usar el formato {DateFormat}.";
                return false;
            }

            if (end < start)
            {
                error = "La fecha final no puede ser anterior a la inicial.";
                return false;
            }

            string normalizedLink = link?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(normalizedLink) &&
                (!Uri.TryCreate(normalizedLink, UriKind.Absolute, out Uri uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            {
                error = "El enlace debe comenzar con http:// o https://.";
                return false;
            }

            publication = new EventPublication(
                Guid.NewGuid().ToString("N"),
                title.Trim(),
                description.Trim(),
                normalizedLink,
                start,
                end,
                imagePath?.Trim() ?? string.Empty,
                createdByRoleId?.Trim() ?? string.Empty,
                DateTime.UtcNow);
            return true;
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value?.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }
    }
}
