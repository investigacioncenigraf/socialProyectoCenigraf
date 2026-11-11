using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>Direction of a connector leaving a room. Row 0 is the TOP row of the level.</summary>
    public enum MazeDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class MazeDirectionExtensions
    {
        public static readonly MazeDirection[] All =
        {
            MazeDirection.Up,
            MazeDirection.Right,
            MazeDirection.Down,
            MazeDirection.Left
        };

        public static MazeDirection Opposite(this MazeDirection direction) =>
            (MazeDirection)(((int)direction + 2) % 4);

        /// <summary>Step in grid coordinates (column grows right, row grows DOWN).</summary>
        public static Vector2Int GridStep(this MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.Up: return new Vector2Int(0, -1);
                case MazeDirection.Right: return new Vector2Int(1, 0);
                case MazeDirection.Down: return new Vector2Int(0, 1);
                default: return new Vector2Int(-1, 0);
            }
        }

        /// <summary>Unit vector in Unity world space (Y grows UP).</summary>
        public static Vector2 WorldVector(this MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.Up: return Vector2.up;
                case MazeDirection.Right: return Vector2.right;
                case MazeDirection.Down: return Vector2.down;
                default: return Vector2.left;
            }
        }

        /// <summary>Z rotation for an arrow sprite that points UP by default.</summary>
        public static float ArrowAngle(this MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.Up: return 0f;
                case MazeDirection.Left: return 90f;
                case MazeDirection.Down: return 180f;
                default: return 270f;
            }
        }
    }

    /// <summary>A room position in the grid.</summary>
    public readonly struct MazeCell : IEquatable<MazeCell>
    {
        public readonly int Column;
        public readonly int Row;

        public MazeCell(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public MazeCell Neighbor(MazeDirection direction)
        {
            Vector2Int step = direction.GridStep();
            return new MazeCell(Column + step.x, Row + step.y);
        }

        public bool Equals(MazeCell other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is MazeCell other && Equals(other);
        public override int GetHashCode() => Column * 397 ^ Row;
        public override string ToString() => $"({Column},{Row})";
        public static bool operator ==(MazeCell a, MazeCell b) => a.Equals(b);
        public static bool operator !=(MazeCell a, MazeCell b) => !a.Equals(b);
    }

    /// <summary>
    /// A connector between two neighbouring rooms. It is always stored as the connector that
    /// leaves <see cref="From"/> to the Right or Down, so every connector has a single identity.
    /// </summary>
    public readonly struct MazeEdge : IEquatable<MazeEdge>
    {
        public readonly MazeCell From;
        public readonly MazeDirection Direction;

        public MazeEdge(MazeCell from, MazeDirection direction)
        {
            From = from;
            Direction = direction;
        }

        public MazeCell To => From.Neighbor(Direction);
        public bool IsHorizontal => Direction == MazeDirection.Right;

        /// <summary>Canonical edge for the connector that leaves <paramref name="cell"/> in <paramref name="direction"/>.</summary>
        public static MazeEdge Between(MazeCell cell, MazeDirection direction)
        {
            if (direction == MazeDirection.Right || direction == MazeDirection.Down)
            {
                return new MazeEdge(cell, direction);
            }

            return new MazeEdge(cell.Neighbor(direction), direction.Opposite());
        }

        public bool Equals(MazeEdge other) => From.Equals(other.From) && Direction == other.Direction;
        public override bool Equals(object obj) => obj is MazeEdge other && Equals(other);
        public override int GetHashCode() => From.GetHashCode() * 31 + (int)Direction;
        public override string ToString() => $"{From}->{Direction}";
    }

    /// <summary>
    /// Geometry of the level: a grid of square rooms joined by connectors.
    /// Defaults match the design: 13x4 rooms of 6 units, connectors 6 long and 2 wide, plugs 2x2,
    /// room (6,3) centred on (0,0).
    /// </summary>
    [Serializable]
    public sealed class MazeLayout
    {
        public int columns = 13;
        public int rows = 4;
        public float roomSize = 6f;
        public float connectorLength = 6f;
        public float connectorWidth = 2f;
        public float plugSize = 2f;
        [Tooltip("World position of the centre of the TOP-LEFT room (column 0, row 0).")]
        public Vector2 firstRoomCenter = new Vector2(-72f, 36f);

        public float Pitch => roomSize + connectorLength;

        public bool Contains(MazeCell cell) =>
            cell.Column >= 0 && cell.Column < columns && cell.Row >= 0 && cell.Row < rows;

        public Vector2 RoomCenter(MazeCell cell) =>
            new Vector2(
                firstRoomCenter.x + cell.Column * Pitch,
                firstRoomCenter.y - cell.Row * Pitch);

        public Vector2 ConnectorCenter(MazeEdge edge) =>
            (RoomCenter(edge.From) + RoomCenter(edge.To)) * 0.5f;

        public Vector2 ConnectorSize(MazeEdge edge) =>
            edge.IsHorizontal
                ? new Vector2(connectorLength, connectorWidth)
                : new Vector2(connectorWidth, connectorLength);

        /// <summary>Centre of the plug that touches <paramref name="room"/> on its <paramref name="direction"/> side.</summary>
        public Vector2 PlugCenter(MazeCell room, MazeDirection direction) =>
            RoomCenter(room) + direction.WorldVector() * (roomSize * 0.5f + plugSize * 0.5f);

        /// <summary>Rectangle that encloses every room.</summary>
        public Rect LevelRect
        {
            get
            {
                float width = (columns - 1) * Pitch + roomSize;
                float height = (rows - 1) * Pitch + roomSize;
                float left = firstRoomCenter.x - roomSize * 0.5f;
                float bottom = firstRoomCenter.y - (rows - 1) * Pitch - roomSize * 0.5f;
                return new Rect(left, bottom, width, height);
            }
        }

        public IEnumerable<MazeCell> Cells()
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    yield return new MazeCell(column, row);
                }
            }
        }

        public IEnumerable<MazeEdge> Edges()
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    MazeCell cell = new MazeCell(column, row);

                    if (column + 1 < columns)
                    {
                        yield return new MazeEdge(cell, MazeDirection.Right);
                    }

                    if (row + 1 < rows)
                    {
                        yield return new MazeEdge(cell, MazeDirection.Down);
                    }
                }
            }
        }
    }
}
