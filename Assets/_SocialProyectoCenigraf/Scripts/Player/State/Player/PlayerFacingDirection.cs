using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    public enum PlayerFacingDirection
    {
        DownRight,
        DownLeft,
        UpRight,
        UpLeft
    }

    public static class PlayerFacingDirectionResolver
    {
        private const float DirectionThreshold = 0.0001f;

        public static PlayerFacingDirection Resolve(
            PlayerFacingDirection currentDirection,
            Vector2 movementDirection)
        {
            bool facesRight = IsFacingRight(currentDirection);
            bool facesUp = IsFacingUp(currentDirection);

            if (movementDirection.x > DirectionThreshold)
            {
                facesRight = true;
            }
            else if (movementDirection.x < -DirectionThreshold)
            {
                facesRight = false;
            }

            if (movementDirection.y > DirectionThreshold)
            {
                facesUp = true;
            }
            else if (movementDirection.y < -DirectionThreshold)
            {
                facesUp = false;
            }

            if (facesUp)
            {
                return facesRight
                    ? PlayerFacingDirection.UpRight
                    : PlayerFacingDirection.UpLeft;
            }

            return facesRight
                ? PlayerFacingDirection.DownRight
                : PlayerFacingDirection.DownLeft;
        }

        private static bool IsFacingRight(PlayerFacingDirection direction)
        {
            return direction == PlayerFacingDirection.DownRight ||
                   direction == PlayerFacingDirection.UpRight;
        }

        private static bool IsFacingUp(PlayerFacingDirection direction)
        {
            return direction == PlayerFacingDirection.UpRight ||
                   direction == PlayerFacingDirection.UpLeft;
        }
    }
}
