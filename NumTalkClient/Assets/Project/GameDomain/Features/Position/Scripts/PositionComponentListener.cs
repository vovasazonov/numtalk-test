using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Position.Scripts
{
    public class PositionComponentListener : ComponentListener<PositionComponent>
    {
        public override void UpdateView(in PositionComponent component)
        {
            transform.parent.position = component.Position;
        }
    }
}
