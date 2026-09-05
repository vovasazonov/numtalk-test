using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Goal.Scripts
{
    public sealed class GoalBaker : MonoBehaviour, IComponentConverter
    {
        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Pickup");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new GoalComponent());
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.35f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 1.05f);
        }
    }
}
