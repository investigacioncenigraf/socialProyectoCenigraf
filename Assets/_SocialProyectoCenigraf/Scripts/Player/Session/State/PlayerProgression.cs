using UnityEngine;

namespace SocialProyectoCenigraf.Session.State
{
    public static class PlayerProgression
    {
        public const int MaximumLevel = 100;
        public const int BaseExperienceStep = 100;
        public const int MaximumExperience =
            BaseExperienceStep * MaximumLevel * (MaximumLevel - 1) / 2;

        public static int NormalizeExperience(int experience)
        {
            return Mathf.Clamp(
                experience,
                GameSessionStateData.MinimumExperience,
                MaximumExperience);
        }

        public static int AddExperience(int currentExperience, int amount)
        {
            long result = (long)currentExperience + amount;
            if (result <= GameSessionStateData.MinimumExperience)
            {
                return GameSessionStateData.MinimumExperience;
            }

            if (result >= MaximumExperience)
            {
                return MaximumExperience;
            }

            return (int)result;
        }

        public static int CalculateLevel(int totalExperience)
        {
            int validExperience = NormalizeExperience(totalExperience);
            int level = GameSessionStateData.DefaultLevel;

            while (level < MaximumLevel &&
                validExperience >= ExperienceRequiredForLevel(level + 1))
            {
                level++;
            }

            return level;
        }

        public static int ExperienceRequiredForLevel(int level)
        {
            int validLevel = Mathf.Clamp(
                level,
                GameSessionStateData.DefaultLevel,
                MaximumLevel);
            return BaseExperienceStep * validLevel * (validLevel - 1) / 2;
        }

        public static int ExperienceRequiredToAdvanceFromLevel(int level)
        {
            int validLevel = Mathf.Clamp(
                level,
                GameSessionStateData.DefaultLevel,
                MaximumLevel);
            return validLevel >= MaximumLevel
                ? 0
                : BaseExperienceStep * validLevel;
        }

        public static float GetLevelProgress01(int totalExperience)
        {
            int validExperience = NormalizeExperience(totalExperience);
            int currentLevel = CalculateLevel(validExperience);
            if (currentLevel >= MaximumLevel)
            {
                return 1f;
            }

            int currentLevelStart = ExperienceRequiredForLevel(currentLevel);
            int nextLevelStart = ExperienceRequiredForLevel(currentLevel + 1);
            return Mathf.InverseLerp(
                currentLevelStart,
                nextLevelStart,
                validExperience);
        }
    }
}
