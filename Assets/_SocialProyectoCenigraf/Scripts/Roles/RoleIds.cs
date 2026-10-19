namespace SocialProyectoCenigraf.Roles
{
    public static class RoleIds
    {
        public const string Apprentice = "apprentice";
        // Compatibility with scenes, saved state and access rules using the former role ID.
        public static string Normalize(string value)
        {
            string id = value == null ? string.Empty : value.Trim();
            return id == "student" ? Apprentice : id;
        }
    }
}
