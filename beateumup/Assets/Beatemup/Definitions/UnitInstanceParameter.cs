using Beatemup.Ecs;
using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Gameplay;
using MyBox;
using UnityEngine;
using LookingDirection = Beatemup.Ecs.LookingDirection;

namespace Beatemup.Definitions
{
    public class UnitInstanceParameter : MonoBehaviour, IEntityInstanceParameter
    {
        public bool controllable = false;
        
        [ConditionalField(nameof(controllable))]
        public int playerInput;

        public float startingLookingDirectionAngle = 0;

        public void Apply(World world, Entity entity)
        {
            ref var position = ref world.GetComponent<PositionComponent>(entity);
            position.value = transform.position;
            
            if (controllable)
            {
                world.AddComponent(entity, new PlayerInputComponent()
                {
                    playerInput = playerInput,
                    disabled = false
                });
            }

            ref var lookingDirection = ref world.GetComponent<LookingDirection>(entity);
            lookingDirection.value = Vector2.right.Rotate(startingLookingDirectionAngle * Mathf.Deg2Rad);
        }
    }
}