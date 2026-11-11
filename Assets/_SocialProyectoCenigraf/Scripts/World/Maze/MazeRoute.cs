using System;
using System.Collections.Generic;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>The single correct path: cells[0] is the start room, the last cell is the exit room.</summary>
    public sealed class MazeRoute
    {
        private readonly MazeCell[] cells;
        private readonly Dictionary<MazeCell, int> indexByCell = new Dictionary<MazeCell, int>();

        public MazeRoute(MazeCell[] cells, int seed)
        {
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Seed = seed;

            for (int i = 0; i < cells.Length; i++)
            {
                indexByCell[cells[i]] = i;
            }
        }

        public int Seed { get; }
        public int Count => cells.Length;
        public MazeCell this[int index] => cells[index];
        public MazeCell Start => cells[0];
        public MazeCell End => cells[cells.Length - 1];

        public bool Contains(MazeCell cell) => indexByCell.ContainsKey(cell);

        /// <summary>Position of the cell in the route, or -1 when it is not part of it.</summary>
        public int IndexOf(MazeCell cell) => indexByCell.TryGetValue(cell, out int index) ? index : -1;

        /// <summary>Rooms that ask a question: every room except the start room and the exit room.</summary>
        public bool IsQuestionIndex(int index) => index >= 1 && index <= cells.Length - 2;

        public override string ToString()
        {
            string[] parts = new string[cells.Length];

            for (int i = 0; i < cells.Length; i++)
            {
                parts[i] = cells[i].ToString();
            }

            return string.Join(" > ", parts);
        }
    }

    /// <summary>
    /// Builds a random self-avoiding route from a random start room. Everything is driven by the
    /// seed, so the same seed always produces the same route.
    /// Rule: each question room needs at least one FALSE exit (a neighbour that is not on the
    /// route), otherwise the room would only have one way in and one way out and no decision.
    /// That rule rejects corner rooms (only 2 neighbours) as question rooms.
    /// </summary>
    public static class MazeRouteGenerator
    {
        public static bool TryGenerate(
            MazeLayout layout,
            int steps,
            int seed,
            out MazeRoute route,
            int maxAttempts = 500,
            int preferredThreeAnswerRooms = 0)
        {
            route = null;

            if (layout == null || steps < 1 || layout.columns < 1 || layout.rows < 1)
            {
                return false;
            }

            System.Random random = new System.Random(seed);

            // Ask for routes with many question rooms that can show 3 answers; if that is too
            // demanding for the dice, relax the requirement one room at a time.
            int maxRich = Math.Min(preferredThreeAnswerRooms, Math.Max(0, steps - 1));

            for (int required = maxRich; required >= 0; required--)
            {
                int attemptsForLevel = required == 0 ? maxAttempts : Math.Max(20, maxAttempts / 5);

                for (int attempt = 0; attempt < attemptsForLevel; attempt++)
                {
                    MazeCell start = new MazeCell(
                        random.Next(layout.columns),
                        random.Next(layout.rows));
                    List<MazeCell> path = new List<MazeCell> { start };
                    HashSet<MazeCell> visited = new HashSet<MazeCell> { start };

                    if (Extend(layout, random, path, visited, steps, required))
                    {
                        route = new MazeRoute(path.ToArray(), seed);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Extend(
            MazeLayout layout,
            System.Random random,
            List<MazeCell> path,
            HashSet<MazeCell> visited,
            int steps,
            int requiredRichRooms)
        {
            if (path.Count == steps + 1)
            {
                return HasFalseExitInEveryQuestionRoom(layout, path, visited, requiredRichRooms);
            }

            MazeCell last = path[path.Count - 1];
            MazeDirection[] order = (MazeDirection[])MazeDirectionExtensions.All.Clone();
            Shuffle(order, random);

            foreach (MazeDirection direction in order)
            {
                MazeCell next = last.Neighbor(direction);

                if (!layout.Contains(next) || visited.Contains(next))
                {
                    continue;
                }

                path.Add(next);
                visited.Add(next);

                if (Extend(layout, random, path, visited, steps, requiredRichRooms))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
                visited.Remove(next);
            }

            return false;
        }

        private static bool HasFalseExitInEveryQuestionRoom(
            MazeLayout layout,
            List<MazeCell> path,
            HashSet<MazeCell> onRoute,
            int requiredRichRooms)
        {
            int rich = 0;

            for (int i = 1; i <= path.Count - 2; i++)
            {
                int falseExits = CountFalseExits(layout, path[i], onRoute);

                if (falseExits < 1)
                {
                    return false;
                }

                if (falseExits >= 2)
                {
                    rich++;
                }
            }

            return rich >= requiredRichRooms;
        }

        /// <summary>Neighbours inside the grid that are NOT on the route.</summary>
        public static int CountFalseExits(MazeLayout layout, MazeCell cell, HashSet<MazeCell> onRoute)
        {
            int count = 0;

            foreach (MazeDirection direction in MazeDirectionExtensions.All)
            {
                MazeCell neighbor = cell.Neighbor(direction);

                if (layout.Contains(neighbor) && !onRoute.Contains(neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        private static void Shuffle<T>(T[] items, System.Random random)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
}
