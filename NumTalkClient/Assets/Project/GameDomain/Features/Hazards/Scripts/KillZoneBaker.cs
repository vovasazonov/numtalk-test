using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Hazards.Scripts
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class KillZoneBaker : MonoBehaviour, IComponentConverter
    {
        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("KillZone");
            GetComponent<BoxCollider>().isTrigger = true;
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new KillZoneComponent());
        }

        private void OnDrawGizmos()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.25f, 0.3f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
