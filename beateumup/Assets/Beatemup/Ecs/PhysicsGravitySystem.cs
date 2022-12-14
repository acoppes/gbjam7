﻿using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Gameplay;
using Leopotam.EcsLite;
using UnityEngine;

namespace Beatemup.Ecs
{
    public class PhysicsGravitySystem : BaseSystem, IEcsRunSystem, IEcsInitSystem
    {
        public float distanceToGround = 0.1f;
        public Vector3 gravity = new Vector3(0, -9.81f, 0);
        
        private LayerMask groundContactLayerMask;
        
        public void Init(EcsSystems systems)
        {
            Physics.gravity = Vector3.zero;
            groundContactLayerMask = LayerMask.GetMask("StaticObstacle");
        }

        public void Run(EcsSystems systems)
        {
            var gravityComponents = world.GetComponents<GravityComponent>();
            var positionComponents = world.GetComponents<PositionComponent>();
            var verticalMovements = world.GetComponents<VerticalMovementComponent>();
            var physicsComponents = world.GetComponents<PhysicsComponent>();
            
            foreach (var entity in world.GetFilter<GravityComponent>().Inc<PhysicsComponent>().End())
            {
                ref var gravityComponent = ref gravityComponents.Get(entity);
                ref var physicsComponent = ref physicsComponents.Get(entity);

                if (gravityComponent.disabled)
                {
                    continue;
                }

                if (physicsComponent.isStatic)
                {
                    continue;
                }

                gravityComponent.inContactWithGround = false;

                var ray = new Ray(physicsComponent.body.position + new Vector3(0, 0.1f, 0), Vector3.down);
                
                if (Physics.Raycast(ray, out var hit, 2f, groundContactLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // don't apply gravity if in contact with ground?
                    gravityComponent.inContactWithGround = hit.distance < distanceToGround;
                }

                if (!gravityComponent.inContactWithGround)
                {
                    physicsComponent.body.AddForce(gravity * gravityComponent.scale, ForceMode.Acceleration);
                }
            }

            foreach (var entity in world.GetFilter<VerticalMovementComponent>().Inc<PositionComponent>().End())
            {
                ref var position = ref positionComponents.Get(entity);
                ref var vertical = ref verticalMovements.Get(entity);

                position.value.y += vertical.speed * Time.deltaTime;
                
                if (position.value.y <= 0)
                {
                    position.value.y = 0;
                    vertical.speed = 0;
                }
                
                vertical.isOverGround = position.value.y <= Mathf.Epsilon;
            }
        }


    }
}