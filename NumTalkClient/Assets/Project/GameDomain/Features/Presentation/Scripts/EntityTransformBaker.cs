using Arch.Unity.Conversion;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>
    /// Captures the authored pose and layer, and marks the entity as needing a runtime view root. Every course
    /// object carries this; the authoring GameObject itself is destroyed by the bake.
    /// </summary>
    public sealed class EntityTransformBaker : MonoBehaviour, IComponentConverter
    {
        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new ViewComponent());
            converter.AddComponent(new EntityTransformComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Layer = gameObject.layer,
            });
        }
    }
}
