using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>One question of a catalog: what is asked, the right answer and the wrong ones.</summary>
    [Serializable]
    public struct MazeQuestionEntry
    {
        [TextArea(1, 3)] public string prompt;
        public string correctAnswer;
        [Tooltip("Shown when the room has two exits with an answer.")]
        public string falseAnswer1;
        [Tooltip("Shown when the room has three exits with an answer.")]
        public string falseAnswer2;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(prompt) && !string.IsNullOrWhiteSpace(correctAnswer);

        public MazeQuestion ToQuestion()
        {
            List<string> wrong = new List<string>(2);

            if (!string.IsNullOrWhiteSpace(falseAnswer1)) wrong.Add(falseAnswer1.Trim());
            if (!string.IsNullOrWhiteSpace(falseAnswer2)) wrong.Add(falseAnswer2.Trim());

            return new MazeQuestion(prompt.Trim(), correctAnswer.Trim(), wrong.ToArray());
        }
    }

    /// <summary>A set of questions for one level. Edited in the Inspector.</summary>
    [CreateAssetMenu(
        fileName = "MazeQuestionCatalog",
        menuName = "Cenigraf/Maze/Question Catalog",
        order = 0)]
    public sealed class MazeQuestionCatalog : ScriptableObject
    {
        [SerializeField] private string catalogName = "Prueba de la Orientación";
        [SerializeField] private List<MazeQuestionEntry> questions = new List<MazeQuestionEntry>();

        public string CatalogName => catalogName;
        public int Count => questions != null ? questions.Count : 0;
        public IReadOnlyList<MazeQuestionEntry> Questions => questions;
    }

    /// <summary>
    /// Deals questions from a catalog without repeating any until the catalog runs out, so the
    /// rooms of one run never ask the same thing twice.
    /// </summary>
    public sealed class MazeCatalogQuestionProvider : IMazeQuestionProvider
    {
        private readonly List<MazeQuestionEntry> pool = new List<MazeQuestionEntry>();
        private readonly MazeQuestionCatalog catalog;
        private int next;

        public MazeCatalogQuestionProvider(MazeQuestionCatalog source, Random random)
        {
            catalog = source;
            Refill(random);
        }

        public MazeQuestion GetQuestion(int questionNumber, Random random)
        {
            if (pool.Count == 0)
            {
                Refill(random);
            }

            if (pool.Count == 0)
            {
                return new MazeQuestion("¿...?", "1", "2");
            }

            if (next >= pool.Count)
            {
                next = 0;
            }

            return pool[next++].ToQuestion();
        }

        private void Refill(Random random)
        {
            pool.Clear();
            next = 0;

            if (catalog == null)
            {
                return;
            }

            foreach (MazeQuestionEntry entry in catalog.Questions)
            {
                if (entry.IsValid)
                {
                    pool.Add(entry);
                }
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                MazeQuestionEntry temp = pool[i];
                pool[i] = pool[j];
                pool[j] = temp;
            }
        }
    }
}
