using Beatemup.Ecs;
using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Controllers;
using Gemserk.Leopotam.Ecs.Gameplay;
using UnityEngine;
using LookingDirection = Beatemup.Ecs.LookingDirection;

namespace Beatemup.Definitions
{
    public class UnitDefinition : MonoBehaviour, IEntityDefinition
    {
        public float movementSpeed;

        public GameObject modelPrefab;
        public bool hasShadow = true;

        public AnimationsAsset animationsAsset;

        public Vector3 hurtBoxSize;

        public void Apply(World world, Entity entity)
        {
            world.AddComponent(entity, new PlayerComponent());
            world.AddComponent(entity, new PositionComponent());
            world.AddComponent(entity, new LookingDirection
            {
                value = Vector2.right
            });

            world.AddComponent(entity, ControlComponent.Default());

            world.AddComponent(entity, new StatesComponent());

            if (modelPrefab != null)
            {
                world.AddComponent(entity, new UnitModelComponent
                {
                    prefab = modelPrefab,
                    hasShadow = hasShadow
                });
            }
            
            if (hurtBoxSize.sqrMagnitude > 0)
            {
                world.AddComponent(entity, new HurtBoxComponent
                {
                    depth = hurtBoxSize.z,
                    size = new Vector2(hurtBoxSize.x, hurtBoxSize.y)
                });
            }
            
            world.AddComponent(entity, new UnitMovementComponent()
            {
                speed = movementSpeed
            });

            if (animationsAsset != null)
            {
                world.AddComponent(entity, new AnimationComponent
                {
                    fps = AnimationComponent.DefaultFrameRate,
                    animationsAsset = animationsAsset,
                    currentAnimation = 0,
                    currentFrame = 0,
                    currentTime = 0,
                    state = AnimationComponent.State.Completed,
                    loops = 0,
                    paused = false
                });
                world.AddComponent(entity, new CurrentAnimationFrameComponent());
                world.AddComponent(entity, new HitBoxComponent());
            }
            
            world.AddComponent(entity, new HitComponent());
        }
    }
}