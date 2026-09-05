using System;
using System.Collections.Generic;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class CharacterBodyComponentListener : ComponentListener<CharacterBodyComponent>
    {
        private static readonly Type[] RootComponents = { typeof(CharacterController) };

        public override IReadOnlyList<Type> RequiredRootComponents => RootComponents;

        public override void UpdateView(in CharacterBodyComponent component)
        {
            CharacterController controller = transform.parent.GetComponent<CharacterController>();
            if (controller == null)
            {
                return;
            }

            controller.height = component.Height;
            controller.radius = component.Radius;
            controller.center = component.Center;
            controller.slopeLimit = component.SlopeLimit;
            controller.stepOffset = component.StepOffset;
            controller.skinWidth = component.SkinWidth;
            controller.minMoveDistance = 0f;
        }
    }
}
