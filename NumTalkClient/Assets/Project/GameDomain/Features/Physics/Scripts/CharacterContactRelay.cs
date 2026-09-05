using System.Collections.Generic;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>
    /// Lives on the entity root beside the CharacterController, because only that object receives controller
    /// contacts. It buffers them for <see cref="CharacterBodyComponentListener"/> to translate into ECS values;
    /// nothing here reaches a system directly.
    /// </summary>
    /// <remarks>
    /// <c>[ExecuteAlways]</c> so the contact callback also fires when the simulation is stepped from an editor
    /// verification. Unity reuses the <see cref="ControllerColliderHit"/> instance between callbacks, so the values
    /// are copied out immediately rather than the object being buffered.
    /// </remarks>
    [ExecuteAlways]
    public sealed class CharacterContactRelay : MonoBehaviour
    {
        public struct Contact
        {
            public Collider Collider;
            public Vector3 Normal;
            public Vector3 Point;
        }

        private readonly List<Contact> _contacts = new();

        public IReadOnlyList<Contact> Contacts => _contacts;

        public void Clear() => _contacts.Clear();

        private void OnControllerColliderHit(ControllerColliderHit hit) => _contacts.Add(new Contact
        {
            Collider = hit.collider,
            Normal = hit.normal,
            Point = hit.point,
        });
    }
}
