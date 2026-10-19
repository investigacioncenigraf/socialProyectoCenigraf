using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SocialProyectoCenigraf.Events
{
    public static class DefaultEventPublications
    {
        private const string AdministratorRoleId = "administrator";

        public static IReadOnlyList<EventPublication> Create()
        {
            DateTime startsAt = new DateTime(2026, 1, 1);
            DateTime endsAt = new DateTime(2026, 12, 31);
            DateTime createdAtUtc = new DateTime(
                2026,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            return new[]
            {
                new EventPublication(
                    "default-free-training",
                    "Aprovecha Formaciones Totalmente Gratis",
                    "Conoce el catálogo de cursos cortos 100% virtuales.",
                    "https://ejecucionformacion.sena.edu.co/cursos-cortos",
                    startsAt,
                    endsAt,
                    GetImagePath("banner 7 de mayo.webp"),
                    AdministratorRoleId,
                    createdAtUtc),
                new EventPublication(
                    "default-technical-support",
                    "ACOMPAÑAMIENTO TÉCNICO",
                    "Descubre todas las herramientas y guías diseñadas para potenciar tu experiencia en el entorno virtual ZAJUNA.",
                    "https://zajuna.sena.edu.co/soporte.php",
                    startsAt,
                    endsAt,
                    GetImagePath("banner soporte .webp"),
                    AdministratorRoleId,
                    createdAtUtc),
                new EventPublication(
                    "default-sena-free",
                    "El SENA es GRATIS !!!",
                    "Implementación del Instrumento de Planificación Predial para la Transición Agroecológica (IPPTA).",
                    "https://betowa.sena.edu.co/oferta/implementacion-del-instrumento-de-planificacion-predial-para-la-transicion-agroecologica-ippta-estacion-ambiental-modulo-1?enrollCourse=3523182&programId=231065&modality=V",
                    startsAt,
                    endsAt,
                    GetImagePath("28.webp"),
                    AdministratorRoleId,
                    createdAtUtc)
            };
        }

        private static string GetImagePath(string fileName)
        {
            return Path.Combine(
                Application.dataPath,
                "_SocialProyectoCenigraf",
                "Asset",
                "imgPublicaciones",
                fileName);
        }
    }
}
