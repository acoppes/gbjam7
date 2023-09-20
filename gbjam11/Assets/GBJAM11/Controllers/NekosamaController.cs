﻿using Game.Components;
using Game.Controllers;
using Game.Queries;
using GBJAM11.Components;
using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Components;
using Gemserk.Leopotam.Ecs.Controllers;
using Gemserk.Leopotam.Ecs.Events;
using Gemserk.Triggers.Queries;

namespace GBJAM11.Controllers
{
    public class NekosamaController : ControllerBase, IUpdate, IActiveController
    {
        public bool CanBeInterrupted(Entity entity, IActiveController activeController)
        {
            return true;
        }

        public void OnInterrupt(Entity entity, IActiveController activeController)
        {
            // if attacking, exit attack
            ExitAttack(entity);
            
            // if teleporting, exit teleport
        }
        
        public void OnUpdate(World world, Entity entity, float dt)
        {
            ref var states = ref entity.Get<StatesComponent>();
            ref var input = ref entity.Get<InputComponent>();
            ref var bufferedInput = ref entity.Get<BufferedInputComponent>();
            ref var animations = ref entity.Get<AnimationComponent>();
            ref var weapons = ref entity.Get<WeaponsComponent>();

            var position = entity.Get<PositionComponent>();

            if (input.direction().vector2.SqrMagnitude() > 0)
            {
                weapons.weaponEntity.Get<LookingDirection>().value = input.direction().vector2;
            }
            // if attacking 
            // fire attack
            
            if (states.TryGetState("Teleporting", out var teleportState))
            {
                if (animations.IsPlaying("Teleport") && animations.isCompleted)
                {
                    ExitTeleport(entity);
                }

                return;
            }
            
            if (states.TryGetState("ChargingAttack", out var chargingState))
            {
                var weapon = weapons.weaponEntity.Get<WeaponComponent>();
                
                // weapon.directionIndicatorInstance.Get<PositionComponent>().value = position.value;
                // weapon.directionIndicatorInstance.Get<LookingDirection>().value = weapons.direction;
                
                if (!input.button1().isPressed)
                {
                    // enter attack
                    animations.Play("Attack", 0);
                    states.ExitState("ChargingAttack");
                    states.EnterState("Attacking");

                    // weapons.weapon.directionIndicatorInstance.Get<DestroyableComponent>().destroy = true;
                }
            }
            
            if (states.TryGetState("Attacking", out var attackState))
            {
                if (animations.IsPlaying("Attack") && animations.isCompleted)
                {
                    var weaponEntity = weapons.weaponEntity;
                    
                    ref var attachPoints = ref entity.Get<AttachPointsComponent>();

                    var projectileEntity = world.CreateEntity(weaponEntity.Get<WeaponComponent>().projectileDefinition);
                    projectileEntity.Get<PositionComponent>().value = attachPoints.Get("weapon").position;
                    
                    ref var projectile = ref projectileEntity.Get<ProjectileComponent>();
                    projectile.initialVelocity = weaponEntity.Get<LookingDirection>().value;

                    projectileEntity.Get<PlayerComponent>().player = entity.Get<PlayerComponent>().player;

                   // weapons.lastFiredProjectile = projectileEntity;
                    
                    ExitAttack(entity);
                }

                return;
            }

            if (bufferedInput.HasBufferedAction(input.button1()))
            {
                var teleportKunaiList = world.GetEntities(new EntityQuery(new TypesParameter("teleport_kunai")));
                
                if (teleportKunaiList.Count > 0)
                {
                    bufferedInput.ConsumeBuffer();
                    EnterTeleport(entity, teleportKunaiList[0]);
                    return;
                }
                else
                {
                    bufferedInput.ConsumeBuffer();
                    EnterAttack(entity);
                    return;
                }
                
                // fire attack

            }

            // ref var movement = ref entity.Get<MovementComponent>();
            // movement.movingDirection = input.direction3d();
            
            // if (bufferedInput.HasBufferedAction(input.button2()))
            // {
            //     // teleport to kunai
            //     EnterTeleport(entity);
            //     return;
            // }
        }

        private void EnterAttack(Entity entity)
        {
            // start anim, start state, etc...
            // lock looking direction, movement, etc..
            
            ref var states = ref entity.Get<StatesComponent>();
            ref var animations = ref entity.Get<AnimationComponent>();
            ref var activeController = ref entity.Get<ActiveControllerComponent>();
            ref var movement = ref entity.Get<MovementComponent>();
            // ref var weapons = ref entity.Get<WeaponsComponent>();
            // ref var input = ref entity.Get<InputComponent>();
            
            activeController.TakeControl(entity, this);
            movement.speed = 0;
            animations.Play("Charge");
            states.EnterState("ChargingAttack");

            // weapons.weapon.directionIndicatorInstance = entity.world.CreateEntity(weapons.weapon.directionIndicatorDefinition);
            // weapons.weapon.directionIndicatorInstance.Get<PositionComponent>().value = entity.Get<PositionComponent>().value;
            // weapons.weapon.directionIndicatorInstance.Get<LookingDirection>().value = weapons.direction;
        }

        private void ExitAttack(Entity entity)
        {
            // exit state, stop anim, etc...
            
            ref var states = ref entity.Get<StatesComponent>();
            ref var activeController = ref entity.Get<ActiveControllerComponent>();
            ref var movement = ref entity.Get<MovementComponent>();

            activeController.ReleaseControl(this);
            movement.speed = movement.baseSpeed;
            states.ExitState("Attacking");
        }

        private void EnterTeleport(Entity entity, Entity kunaiEntity)
        {
            ref var states = ref entity.Get<StatesComponent>();
            ref var animations = ref entity.Get<AnimationComponent>();
            ref var activeController = ref entity.Get<ActiveControllerComponent>();
            ref var movement = ref entity.Get<MovementComponent>();
            ref var weapons = ref entity.Get<WeaponsComponent>();
            
            activeController.TakeControl(entity, this);
            movement.speed = 0;
            
            animations.Play("Teleport", 0);
            states.EnterState("Teleporting");
            
            // spawn teleport particle in position

            var kunaiComponent = kunaiEntity.Get<KunaiComponent>();
            if (kunaiComponent.stuckEntity.Exists())
            {
                // swap places!!
                kunaiComponent.stuckEntity.Get<PositionComponent>().value = entity.Get<PositionComponent>().value;
            }
            
            var teleportPosition = kunaiEntity.Get<PositionComponent>().value;
            // teleportPosition.y = 0;
            entity.Get<PositionComponent>().value = teleportPosition;

            kunaiEntity.Get<DestroyableComponent>().destroy = true;
            // weapons.lastFiredProjectile = Entity.NullEntity;
        }

        private void ExitTeleport(Entity entity)
        {
            ref var states = ref entity.Get<StatesComponent>();
            ref var activeController = ref entity.Get<ActiveControllerComponent>();
            ref var movement = ref entity.Get<MovementComponent>();
            
            activeController.ReleaseControl(this);
            movement.speed = movement.baseSpeed;
            states.ExitState("Teleporting");
        }


    }
}