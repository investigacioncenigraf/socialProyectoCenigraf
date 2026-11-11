using SocialProyectoCenigraf.World.Rendering;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>One connector (alley) between two rooms: its two plugs and its fog.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Maze/Maze Connector")]
    public sealed class MazeConnector : MonoBehaviour
    {
        [SerializeField] private Vector2Int fromCell;
        [SerializeField] private MazeDirection direction = MazeDirection.Right;
        [SerializeField] private MazePlug plugFrom;
        [SerializeField] private MazePlug plugTo;
        [SerializeField] private ShadowArea fog;

        public MazeEdge Edge => new MazeEdge(new MazeCell(fromCell.x, fromCell.y), direction);
        public MazePlug PlugFrom => plugFrom;
        public MazePlug PlugTo => plugTo;

        /// <summary>Used by the editor builder to wire the connector.</summary>
        public void Bind(Vector2Int from, MazeDirection edgeDirection, MazePlug first, MazePlug second, ShadowArea shadow)
        {
            fromCell = from;
            direction = edgeDirection;
            plugFrom = first;
            plugTo = second;
            fog = shadow;
        }

        /// <summary>True while the connector is still covered by fog.</summary>
        public bool FogCovered => fog != null && fog.gameObject.activeSelf && !fog.IsRevealed;

        public void RevealFog()
        {
            if (!FogCovered)
            {
                return;
            }

            fog.RevealNow(false);

            // The path is visible now, so its plugs may show themselves again.
            if (plugFrom != null) plugFrom.SetHidden(false);
            if (plugTo != null) plugTo.SetHidden(false);
        }

        public void ApplyFogLayer(int sortingLayerId, Color color, int orderBase)
        {
            if (fog == null)
            {
                return;
            }

            fog.SetShadowColor(color);

            SpriteRenderer fogRenderer = fog.GetComponent<SpriteRenderer>();

            if (fogRenderer != null)
            {
                fogRenderer.sortingLayerID = sortingLayerId;
            }

            // The fog is sorted by its own lower edge, like any world object, so the player is
            // never cut by it when he walks up to a path that is still covered.
            WorldYSort fogSorting = fog.GetComponent<WorldYSort>();

            if (fogSorting != null)
            {
                fogSorting.SetOrderOffset(orderBase);
                fogSorting.RefreshSortingOrder();
            }
        }

        public void ApplyStyle(UnityEngine.Color color, Sprite sprite, float sortPointDrop, int sortingLayerId, int orderBase)
        {
            if (plugFrom != null) plugFrom.SetStyle(color, sprite, sortPointDrop, sortingLayerId, orderBase);
            if (plugTo != null) plugTo.SetStyle(color, sprite, sortPointDrop, sortingLayerId, orderBase);
        }

        public void Apply(MazeConnectorPlan plan)
        {
            if (plugFrom != null)
            {
                plugFrom.SetState(plan.PlugFrom);
                plugFrom.SetHidden(plan.Fogged);
            }

            if (plugTo != null)
            {
                plugTo.SetState(plan.PlugTo);
                plugTo.SetHidden(plan.Fogged);
            }

            if (fog != null)
            {
                fog.gameObject.SetActive(plan.Fogged);

                if (plan.Fogged)
                {
                    fog.CoverAgain();
                }
            }
        }
    }
}
