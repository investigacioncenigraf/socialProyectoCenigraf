using System.Collections.Generic;

namespace SocialProyectoCenigraf.World.Maze
{
    public enum MazePlugState
    {
        /// <summary>No collider and no visual (nothing can reach this plug).</summary>
        Inactive,
        /// <summary>Solid collider, visible. Closes a path.</summary>
        Blocking,
        /// <summary>Solid like Blocking, but it disappears when the player activates the room's terminal.</summary>
        Locked,
        /// <summary>No collider: the player walks through it. Crossing it reveals the room it touches.</summary>
        Passage
    }

    /// <summary>What a single room has to show and how it starts.</summary>
    public sealed class MazeRoomPlan
    {
        public MazeCell Cell;
        public bool IsStart;
        public bool IsExit;
        /// <summary>Covered by fog at the start. Only the start room begins uncovered.</summary>
        public bool Fogged;
        public bool HasQuestion;
        public MazeQuestion Question;
        public MazeDirection CorrectDirection;
        /// <summary>Exits that show a wrong answer (0, 1 or 2).</summary>
        public readonly List<MazeDirection> FalseDirections = new List<MazeDirection>();
        public bool HasFalseAnswer => FalseDirections.Count > 0;
    }

    public sealed class MazeConnectorPlan
    {
        public MazeEdge Edge;
        /// <summary>State of the plug that touches Edge.From.</summary>
        public MazePlugState PlugFrom;
        /// <summary>State of the plug that touches Edge.To.</summary>
        public MazePlugState PlugTo;
        /// <summary>Covered by fog: the player is never meant to see it.</summary>
        public bool Fogged;
    }

    /// <summary>Everything the scene needs to apply for one generated route.</summary>
    public sealed class MazeLevelPlan
    {
        public MazeRoute Route;
        public readonly Dictionary<MazeCell, MazeRoomPlan> Rooms = new Dictionary<MazeCell, MazeRoomPlan>();
        public readonly Dictionary<MazeEdge, MazeConnectorPlan> Connectors = new Dictionary<MazeEdge, MazeConnectorPlan>();
    }

    public static class MazeLevelPlanner
    {
        public static MazeLevelPlan Build(
            MazeLayout layout,
            MazeRoute route,
            IMazeQuestionProvider questions,
            System.Random random,
            float threeAnswerChance = 0.75f)
        {
            MazeLevelPlan plan = new MazeLevelPlan { Route = route };

            // Rooms: only the start room begins uncovered.
            foreach (MazeCell cell in layout.Cells())
            {
                int index = route.IndexOf(cell);
                plan.Rooms[cell] = new MazeRoomPlan
                {
                    Cell = cell,
                    IsStart = index == 0,
                    IsExit = index == route.Count - 1 && route.Count > 1,
                    Fogged = index != 0
                };
            }

            // Question rooms: one correct exit (towards the next room) and one false exit.
            HashSet<MazeCell> onRoute = new HashSet<MazeCell>();

            for (int i = 0; i < route.Count; i++)
            {
                onRoute.Add(route[i]);
            }

            for (int i = 1; i <= route.Count - 2; i++)
            {
                MazeRoomPlan room = plan.Rooms[route[i]];
                room.CorrectDirection = DirectionBetween(route[i], route[i + 1]);

                List<MazeDirection> falseCandidates = new List<MazeDirection>();

                foreach (MazeDirection direction in MazeDirectionExtensions.All)
                {
                    MazeCell neighbor = route[i].Neighbor(direction);

                    if (layout.Contains(neighbor) && !onRoute.Contains(neighbor))
                    {
                        falseCandidates.Add(direction);
                    }
                }

                MazeQuestion question = questions.GetQuestion(i, random);
                room.HasQuestion = true;
                room.Question = question;

                // Two wrong exits (3 answers in total) whenever the room has room for them and the
                // dice say so; otherwise a single wrong exit (2 answers).
                int falseCount = falseCandidates.Count >= 2 && random.NextDouble() < threeAnswerChance ? 2 : 1;
                falseCount = System.Math.Min(falseCount, falseCandidates.Count);

                for (int f = 0; f < falseCount; f++)
                {
                    int pick = random.Next(falseCandidates.Count);
                    room.FalseDirections.Add(falseCandidates[pick]);
                    falseCandidates.RemoveAt(pick);
                }
            }

            // Connectors.
            foreach (MazeEdge edge in layout.Edges())
            {
                plan.Connectors[edge] = PlanConnector(plan, route, edge);
            }

            return plan;
        }

        private static MazeConnectorPlan PlanConnector(MazeLevelPlan plan, MazeRoute route, MazeEdge edge)
        {
            MazeCell from = edge.From;
            MazeCell to = edge.To;
            int fromIndex = route.IndexOf(from);
            int toIndex = route.IndexOf(to);

            MazeConnectorPlan result = new MazeConnectorPlan
            {
                Edge = edge,
                PlugFrom = MazePlugState.Inactive,
                PlugTo = MazePlugState.Inactive,
                Fogged = true
            };

            if (fromIndex >= 0 && toIndex >= 0)
            {
                if (System.Math.Abs(fromIndex - toIndex) == 1)
                {
                    // Consecutive route rooms: the way forward. The end next to a question room stays
                    // locked until the player uses that room's terminal; the other end is open.
                    int lowerIndex = System.Math.Min(fromIndex, toIndex);
                    bool lowerIsFrom = fromIndex < toIndex;
                    bool locked = lowerIndex >= 1 && plan.Rooms[route[lowerIndex]].HasQuestion;
                    MazePlugState lowerState = locked ? MazePlugState.Locked : MazePlugState.Passage;

                    result.PlugFrom = lowerIsFrom ? lowerState : MazePlugState.Passage;
                    result.PlugTo = lowerIsFrom ? MazePlugState.Passage : lowerState;
                    result.Fogged = locked;
                }
                else
                {
                    // Two route rooms that are neighbours but not consecutive: sealed shortcut.
                    result.PlugFrom = MazePlugState.Blocking;
                    result.PlugTo = MazePlugState.Blocking;
                }

                return result;
            }

            if (fromIndex < 0 && toIndex < 0)
            {
                // Far from the route: unreachable, only hidden by fog.
                return result;
            }

            // One side is a route room, the other is not.
            bool routeIsFrom = fromIndex >= 0;
            MazeCell routeCell = routeIsFrom ? from : to;
            MazeDirection towardsOther = routeIsFrom ? edge.Direction : edge.Direction.Opposite();
            MazeRoomPlan roomPlan = plan.Rooms[routeCell];

            bool isFalseBranch = roomPlan.HasQuestion &&
                                 roomPlan.FalseDirections.Contains(towardsOther);

            if (isFalseBranch)
            {
                // Wrong answer: the player can walk into the connector, but the far end is plugged.
                if (routeIsFrom)
                {
                    result.PlugFrom = MazePlugState.Locked;
                    result.PlugTo = MazePlugState.Blocking;
                }
                else
                {
                    result.PlugFrom = MazePlugState.Blocking;
                    result.PlugTo = MazePlugState.Locked;
                }

                result.Fogged = true;
                return result;
            }

            // Unused exit: closed right at the room's door and hidden.
            result.PlugFrom = MazePlugState.Blocking;
            result.PlugTo = MazePlugState.Blocking;
            return result;
        }

        public static MazeDirection DirectionBetween(MazeCell from, MazeCell to)
        {
            foreach (MazeDirection direction in MazeDirectionExtensions.All)
            {
                if (from.Neighbor(direction) == to)
                {
                    return direction;
                }
            }

            return MazeDirection.Up;
        }
    }
}
