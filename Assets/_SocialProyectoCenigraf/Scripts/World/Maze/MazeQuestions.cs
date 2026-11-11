using System;

namespace SocialProyectoCenigraf.World.Maze
{
    public readonly struct MazeQuestion
    {
        public readonly string Prompt;
        public readonly string CorrectAnswer;
        public readonly string[] FalseAnswers;
        /// <summary>First false answer (kept for convenience).</summary>
        public string FalseAnswer => FalseAnswers != null && FalseAnswers.Length > 0 ? FalseAnswers[0] : string.Empty;

        public MazeQuestion(string prompt, string correctAnswer, params string[] falseAnswers)
        {
            Prompt = prompt;
            CorrectAnswer = correctAnswer;
            FalseAnswers = falseAnswers ?? Array.Empty<string>();
        }
    }

    /// <summary>Temporary source of questions until the real question system exists.</summary>
    public interface IMazeQuestionProvider
    {
        /// <param name="questionNumber">1 for the first question room, 2 for the second, ...</param>
        MazeQuestion GetQuestion(int questionNumber, Random random);
    }

    /// <summary>
    /// Multiplication table: question N asks "N x table" (1x1, 2x1, 3x1, ... for table 1).
    /// The false answer is a random number different from the correct one.
    /// </summary>
    public sealed class TimesTableQuestionProvider : IMazeQuestionProvider
    {
        private readonly int table;

        public TimesTableQuestionProvider(int table = 1)
        {
            this.table = table;
        }

        private static int RandomWrong(Random random, int maximum, int correct, int alreadyUsed)
        {
            int value;

            do
            {
                value = random.Next(0, maximum + 1);
            }
            while (value == correct || value == alreadyUsed);

            return value;
        }

        public MazeQuestion GetQuestion(int questionNumber, Random random)
        {
            int correct = questionNumber * table;
            int maximum = Math.Max(12, correct * 2);
            int first = RandomWrong(random, maximum, correct, -1);
            int second = RandomWrong(random, maximum, correct, first);

            return new MazeQuestion(
                $"¿Cuánto es {questionNumber}×{table}?",
                correct.ToString(),
                first.ToString(),
                second.ToString());
        }
    }
}
