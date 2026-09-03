using Project.GameDomain.Features.Universe.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Location.Scripts
{
    public class LocationGizmos : MonoBehaviour
    {
        [SerializeField] private Color _color = Color.green;
        [SerializeField] private Color _runnableColor = Color.yellow;
        [SerializeField] private int _pixelWidth = 250;
        [SerializeField] private int _pixelHeight = 250;
        [SerializeField] private int _runnablePixelHeight = 200;

        private void OnDrawGizmos()
        {
            Gizmos.color = _color;

            float width = UniverseConsts.CalculateUnitsBasePixels(_pixelWidth);
            float height = UniverseConsts.CalculateUnitsBasePixels(_pixelHeight);
            
            Vector3 size = new Vector3(width, height, 0f);
            Vector3 center = transform.position;

            Gizmos.DrawWireCube(center, size);

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            Gizmos.DrawLine(center + Vector3.left * halfWidth, center + Vector3.right * halfWidth);
            Gizmos.DrawLine(center + Vector3.down * halfHeight, center + Vector3.up * halfHeight);

            Gizmos.color = _runnableColor;

            float runnableHeight = UniverseConsts.CalculateUnitsBasePixels(_runnablePixelHeight);
            Vector3 runnableSize = new Vector3(width, runnableHeight, 0f);

            Gizmos.DrawWireCube(center, runnableSize);
        }
    }
}
