﻿using System.Collections.Generic;
using Beatemup.Definitions;
using Beatemup.Ecs;
using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Gameplay;
using Gemserk.Leopotam.Gameplay.Controllers;
using Gemserk.Leopotam.Gameplay.Events;
using UnityEngine;
using LookingDirection = Beatemup.Ecs.LookingDirection;

namespace Beatemup.Controllers
{
    public class NinjaController : ControllerBase, IInit, IStateChanged, IUpdate
    {
        public float dashFrontIntensity = 1.0f;
        public float dashFrontTime = 0.1f;
        public float dashBackTime = 0.1f;
        public float dashHeight = 1.0f;
        
        private Vector3 dashRecoveryDirection;
        
        public float dashRecoverySpeed = 10.0f;
        
        public float dashRecoveryTime = 0.5f;
        
        public AnimationCurve dashRecoverySpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        private bool dashBackRecoveryCanFlip = true;
        
        public float dashFrontCooldown = 5.0f / 15.0f;
        private float dashFrontCooldownCurrent = 0;
        
        public float dashBackCooldown = 5.0f / 15.0f;
        private float dashBackCooldownCurrent = 0;

        public List<string> comboAnimations = new List<string>()
        {
            "Attack2", "Attack3", "AttackFinisher"
        };
        
        public float attackCooldown = 0.1f;
        private float attackCooldownCurrent = 0;
        
        private int comboAttacks => comboAnimations.Count;
        private int currentComboAttack;

        private Vector3 teleportLastHitPosition;
        
        public float knockbackCurveSpeed = 1.0f;
        public float knockbackMaxHeight = 1.0f;

        public float knockbackDownTime = 1.0f;

        public float knockbackRandomAngle = 0;

        public float knockbackHorizontalIntensity = 1;
        private Vector2 knockbackDirection;

        public CameraShakeAsset knockbackGroundHitCameraShake;

        public GameObject deathBodyDefinition;

        public GameObject projectileDefinition;
        
        public float hitStopTime = TmntConstants.HitAnimationPauseTime;

        public bool rangeAttackFired;
        public float rangeAttackTime = 2 / 15f;
        public Vector3 rangeAttackDirection;

        public void OnInit()
        {
            ref var hitComponent = ref world.GetComponent<HitPointsComponent>(entity);
            hitComponent.OnHitEvent += OnHit;
        }
        
        private void OnHit(World world, Entity entity, HitPointsComponent hitPointsComponent)
        {
            ref var states = ref world.GetComponent<StatesComponent>(entity);
            var position = world.GetComponent<PositionComponent>(entity);
            ref var lookingDirection = ref world.GetComponent<LookingDirection>(entity);
            ref var animationComponent = ref world.GetComponent<AnimationComponent>(entity);
            ref var modelShakeComponent = ref world.GetComponent<ModelShakeComponent>(entity);
            
            if (states.HasState("DashFront") || states.HasState("DashBack"))
            {
                return;
            }

            var knockback = false;

            foreach (var hit in hitPointsComponent.hits)
            {
                var hitPosition = hit.position;
                
                lookingDirection.value = (hitPosition - position.value).normalized;
                
                knockback = knockback || hit.knockback;
            }

            if (knockback)
            {
                states.EnterState("Knockback");
            }
            else
            {
                if (states.HasState("HitStun"))
                {
                    // reset anim
                    animationComponent.Play("HitStun", 1);
                    animationComponent.pauseTime = hitStopTime;
                    modelShakeComponent.Shake(hitStopTime);
                }
                else
                {
                    states.EnterState("HitStun");   
                } 
            }
            
        }
        
        public void OnEnterState()
        {
            ref var animation = ref world.GetComponent<AnimationComponent>(entity);
            
            var states = world.GetComponent<StatesComponent>(entity);
            ref var gravityComponent = ref world.GetComponent<GravityComponent>(entity);
            ref var physicsComponent = ref world.GetComponent<PhysicsComponent>(entity);
            
            ref var movement = ref world.GetComponent<HorizontalMovementComponent>(entity);

            var lookingDirection = world.GetComponent<LookingDirection>(entity);
            var control = world.GetComponent<ControlComponent>(entity);
            
            if (states.statesEntered.Contains("Moving"))
            {
                animation.Play("Walk");
            }
            
            if (states.statesEntered.Contains("RangeAttack"))
            {
                animation.Play("RangeAttack",1);
                rangeAttackFired = false;
                movement.speed = 0;
                movement.movingDirection = Vector3.zero;
                
                rangeAttackDirection = control.direction3d;
                    
                if (control.direction3d.sqrMagnitude < 0.1f)
                {
                    rangeAttackDirection = lookingDirection.value;
                }
            }
            
            if (states.statesEntered.Contains("DashBackRecovery"))
            {
                dashRecoveryDirection = movement.movingDirection;
                animation.Play("DashRecovery");
            }
            
            if (states.statesEntered.Contains("DashBack"))
            {
                animation.Play("DashBack", 1);
                movement.movingDirection = Vector2.zero;

                var direction = new Vector2(-lookingDirection.value.x, control.direction.y);
                direction.Normalize();
                
                var impulse = new Vector3(direction.x * dashFrontIntensity, dashHeight, direction.y * dashFrontIntensity);
                
                physicsComponent.disableCollideWithObstacles = true;
                physicsComponent.syncType = PhysicsComponent.SyncType.FromPhysics;
                physicsComponent.body.AddForce(impulse, ForceMode.Impulse);
            }

            if (states.statesEntered.Contains("DashFront"))
            {
                animation.Play("DashFront", 1);
                
                var direction = new Vector2(control.direction.x, control.direction.y);
                if (direction.SqrMagnitude() < 0.01f)
                {
                    direction = lookingDirection.value;
                }
                
                direction.Normalize();
                
                // BUG: create a normalized direction 
                
                var impulse = new Vector3(direction.x * dashFrontIntensity, dashHeight, direction.y * dashFrontIntensity);
                
                physicsComponent.disableCollideWithObstacles = true;
                physicsComponent.syncType = PhysicsComponent.SyncType.FromPhysics;
                physicsComponent.body.AddForce(impulse, ForceMode.Impulse);
                
                movement.movingDirection = Vector2.zero;
            }
            
            if (states.statesEntered.Contains("HitStun"))
            {
                states.ExitState("Attack");
                states.ExitState("Combo");
                states.ExitState("Down");
                
                // states.ExitState("DashBack");
            }
            
            if (states.statesEntered.Contains("Knockback"))
            {
                animation.Play("KnockdownAscending");

                var knockbackDirection = new Vector2(-lookingDirection.value.x, 0);
                knockbackDirection = knockbackDirection.Rotate(UnityEngine.Random.Range(-knockbackRandomAngle, knockbackRandomAngle) *
                                                            Mathf.Deg2Rad);
                knockbackDirection *= knockbackHorizontalIntensity;
                
                physicsComponent.disableCollideWithObstacles = true;
                physicsComponent.syncType = PhysicsComponent.SyncType.FromPhysics;
                physicsComponent.body.AddForce(new Vector3(knockbackDirection.x, knockbackMaxHeight, knockbackDirection.y), ForceMode.Impulse);
                
                // knockbackRandomY = UnityEngine.Random.Range(-0.25f, 0.25f);
                
                states.EnterState("Knockback.Up");

                states.ExitState("Attack");
                states.ExitState("Combo");
                states.ExitState("HitStun");
                states.ExitState("Down");
            }
            
            if (states.statesEntered.Contains("GetUp"))
            {
                animation.Play("GetUp", 1);
                movement.speed = 0;
            }
            
            if (states.statesEntered.Contains("Down"))
            {
                animation.Play("Down");
                movement.speed = 0;
            }
            
            if (states.statesEntered.Contains("Death"))
            {
                animation.Play("Death", 1);
                // block movement, etc
                movement.speed = 0;
            }
        }

        public void OnExitState()
        {
            var states = world.GetComponent<StatesComponent>(entity);
            
            ref var gravityComponent = ref world.GetComponent<GravityComponent>(entity);
            ref var movement = ref world.GetComponent<HorizontalMovementComponent>(entity);
            ref var position = ref world.GetComponent<PositionComponent>(entity);
            
            ref var physicsComponent = ref world.GetComponent<PhysicsComponent>(entity);
            
            if (states.statesExited.Contains("DashBackJump"))
            {
                position.value.z = 0;
                movement.speed = 0;
                gravityComponent.disabled = false;
                
                // exit all sub states, for now manually
                states.ExitState("DashBackJump.Up");
                states.ExitState("DashBackJump.Fall");
            }
            
            if (states.statesExited.Contains("DashBack"))
            {
                position.value.y = 0;
                movement.speed = 0;
                physicsComponent.disableCollideWithObstacles = false;
                physicsComponent.syncType = PhysicsComponent.SyncType.Both;
            }
            
            if (states.statesExited.Contains("DashFront"))
            {
                position.value.y = 0;
                movement.speed = 0;
                physicsComponent.disableCollideWithObstacles = false;
                physicsComponent.syncType = PhysicsComponent.SyncType.Both;
            }
            
            if (states.statesExited.Contains("Knockback"))
            {
                // gravityComponent.disabled = false;
                physicsComponent.disableCollideWithObstacles = false;
                physicsComponent.syncType = PhysicsComponent.SyncType.Both;
                position.value.y = 0;
                
                states.ExitState("Knockback.Up");
                states.ExitState("Knockback.Down");
            }
        }

        public void OnUpdate(float dt)
        {
            var control = world.GetComponent<ControlComponent>(entity);

            ref var movement = ref world.GetComponent<HorizontalMovementComponent>(entity);
            ref var gravityComponent = ref world.GetComponent<GravityComponent>(entity);

            ref var animationComponent = ref world.GetComponent<AnimationComponent>(entity);
            ref var modelShakeComponent = ref world.GetComponent<ModelShakeComponent>(entity);
            
            var currentAnimationFrame = world.GetComponent<CurrentAnimationAttackComponent>(entity);
            ref var states = ref world.GetComponent<StatesComponent>(entity);
            
            ref var position = ref world.GetComponent<PositionComponent>(entity);
            ref var hitPoints = ref world.GetComponent<HitPointsComponent>(entity);
            ref var physicsComponent = ref world.GetComponent<PhysicsComponent>(entity);
            
            ref var player = ref world.GetComponent<PlayerComponent>(entity);

            ref var lookingDirection = ref world.GetComponent<LookingDirection>(entity);
            
            ref var cameraShakeProvider = ref world.GetComponent<CameraShakeProvider>(entity);

            State state;
            
            if (states.TryGetState("Death", out state))
            {
                movement.movingDirection = Vector2.zero;

                if (animationComponent.state == AnimationComponent.State.Completed)
                {
                    ref var destroyable = ref world.GetComponent<DestroyableComponent>(entity);
                    destroyable.destroy = true;

                    if (deathBodyDefinition != null)
                    {
                        var deathBodyEntity = world.CreateEntity(deathBodyDefinition);
                        ref var deathBodyPosition = ref world.GetComponent<PositionComponent>(deathBodyEntity);
                        deathBodyPosition.value = position.value;
                        
                        ref var deathBodyAnimationComponent = ref world.GetComponent<AnimationComponent>(deathBodyEntity);
                        deathBodyAnimationComponent.currentAnimation = animationComponent.currentAnimation;
                        deathBodyAnimationComponent.currentFrame = animationComponent.currentFrame;
                        deathBodyAnimationComponent.animationsAsset = animationComponent.animationsAsset;
                        
                        ref var deathBodyLookingDirection = ref world.GetComponent<LookingDirection>(deathBodyEntity);
                        deathBodyLookingDirection.value = lookingDirection.value;
                    }
                }
                
                return;
            }
            
            if (states.TryGetState("Down", out state))
            {
                if (state.time > knockbackDownTime)
                {
                    states.ExitState("Down");
                    states.EnterState("GetUp");
                }
                
                return;
                
            }
            if (states.TryGetState("GetUp", out state))
            {
                if (animationComponent.state == AnimationComponent.State.Completed)
                {
                    states.ExitState("GetUp");
                }
                return;
            }
            
            if (states.TryGetState("Knockback", out state))
            {
                // movement.baseSpeed = new Vector2(knockbackBaseSpeed, 0);
                movement.movingDirection = Vector2.zero;

                var moving = Mathf.Abs(physicsComponent.velocity.x) > 0;

                if (moving)
                {
                    lookingDirection.value = new Vector2(-physicsComponent.velocity.x, 0).normalized;
                }

                if (states.HasState("Knockback.Up"))
                {
                    if (physicsComponent.velocity.y < 0 && state.time > 2f/15f)
                    {
                        states.ExitState("Knockback.Up");
                        states.EnterState("Knockback.Down");
                    }
                    
                    return;
                }
                
                if (states.HasState("Knockback.Down"))
                {
                    if (gravityComponent.inContactWithGround)
                    {
                        states.ExitState("Knockback.Down");
                        states.ExitState("Knockback");

                        if (knockbackGroundHitCameraShake != null)
                        {
                            cameraShakeProvider.AddShake(knockbackGroundHitCameraShake);
                        }
                    
                        if (hitPoints.current <= 0)
                        {
                            states.EnterState("Death");
                        }
                        else
                        {
                            states.EnterState("Down");
                        }
                    }
                }
                
             
                
                return;
            }
            
            if (states.TryGetState("HitStun", out state))
            {
                movement.movingDirection = Vector2.zero;
                
                if (!animationComponent.IsPlaying("HitStun"))
                {
                    animationComponent.Play("HitStun", 1);
                    animationComponent.pauseTime = hitStopTime;
                    modelShakeComponent.Shake(hitStopTime);
                }

                if (animationComponent.state == AnimationComponent.State.Completed)
                {
                    states.ExitState("HitStun");
                    
                    if (hitPoints.current <= 0)
                    {
                        states.EnterState("Death");
                    }
                }
                
                return;
            }
            
            if (states.TryGetState("RangeAttack", out state))
            {
                if (!rangeAttackFired && animationComponent.playingTime > rangeAttackTime)
                {
                    var projectileEntity = world.CreateEntity(projectileDefinition, new IEntityInstanceParameter[]
                    {
                        new LookingDirectionParameter()
                        {
                            value = rangeAttackDirection
                        }
                    });
                    
                    // ref var projectileLookingDirection = ref world.GetComponent<LookingDirection>(projectileEntity);
                    // projectileLookingDirection.value = control.direction3d;
                    //
                    // if (control.direction3d.sqrMagnitude < 0.1f)
                    // {
                    //     projectileLookingDirection.value = lookingDirection.value;
                    // }
                    
                    ref var projectilePosition = ref world.GetComponent<PositionComponent>(projectileEntity);
                    projectilePosition.value = position.value + new Vector3(0, 1f, 0);
                    
                    ref var projectilePlayer = ref world.GetComponent<PlayerComponent>(projectileEntity);
                    projectilePlayer.player = player.player;
                    
                    rangeAttackFired = true;
                }
                
                if (animationComponent.state == AnimationComponent.State.Completed)
                {
                    // fire projectile in with direction lookingdirection
                    states.ExitState("RangeAttack");
                }
                return;
            }

            if (states.TryGetState("DashBackRecovery", out state))
            {
                // movement.movingDirection = Vector2.zero;
                
                // TODO: set direction from caller 

                movement.movingDirection = dashRecoveryDirection;
                movement.speed = dashRecoverySpeedCurve.Evaluate(state.time / dashRecoveryTime) * dashRecoverySpeed;
                
                if (dashBackRecoveryCanFlip && control.backward.isPressed)
                {
                    lookingDirection.value.x = -lookingDirection.value.x;
                }
                
                if (state.time > dashRecoveryTime)
                {
                    physicsComponent.body.velocity = Vector3.zero;
                    states.ExitState("DashBackRecovery");
                    dashBackRecoveryCanFlip = true;
                }

                if (control.HasBufferedAction(control.button1) && attackCooldownCurrent <= 0)
                {
                    physicsComponent.body.velocity = Vector3.zero;
                    states.ExitState("DashBackRecovery");
                    dashBackRecoveryCanFlip = true;

                    // if (control.backward.isPressed)
                    // {
                    //     lookingDirection.value.x = control.direction.x;
                    // }
                    
                    currentComboAttack = 0;
                    animationComponent.Play("Attack1", 1);
                    movement.movingDirection = Vector2.zero;
                    control.ConsumeBuffer();
                    states.EnterState("Attack");
                
                    return;
                }
                
                dashBackCooldownCurrent -= Time.deltaTime;
                dashFrontCooldownCurrent -= Time.deltaTime;
                
                return;
            }

            if (states.TryGetState("DashBack", out state))
            {
                dashBackCooldownCurrent = dashBackCooldown;

                if (state.time > dashBackTime && gravityComponent.inContactWithGround)
                {
                    states.ExitState(state.name);
                    states.EnterState("DashBackRecovery");
                }
                
                return;
            }
            
            if (states.TryGetState("DashFront", out state))
            {
                dashFrontCooldownCurrent = dashFrontCooldown;

                if (state.time > dashFrontTime && gravityComponent.inContactWithGround)
                {
                    states.ExitState(state.name);
                    states.EnterState("DashBackRecovery");
                }
                
                return;
            }

            dashBackCooldownCurrent -= Time.deltaTime;
            dashFrontCooldownCurrent -= Time.deltaTime;

            if (states.TryGetState("HiddenAttack", out state))
            {
                if (animationComponent.IsPlaying("TeleportOut") && animationComponent.state == AnimationComponent.State.Completed)
                {
                    position.value = teleportLastHitPosition;
                    position.value.x += lookingDirection.value.x * 3;
                    
                    animationComponent.Play("TeleportIn", 1);
                    return;
                }
                
                if (animationComponent.IsPlaying("TeleportIn") && animationComponent.state == AnimationComponent.State.Completed)
                {
                    lookingDirection.value.x = -lookingDirection.value.x;
                    
                    states.ExitState("HiddenAttack");
                    
                    // states.EnterState("Attack");
                    // currentComboAttack = comboAttacks;
                    // animation.Play("TeleportFinisher", 1);
                    
                    // dont allow flip! 
                    dashBackRecoveryCanFlip = false;
                    states.EnterState("DashBackRecovery");

                    // reset dash cooldown
                    dashFrontCooldownCurrent = dashFrontCooldown;
                    
                    // if (states.HasState("Combo"))
                    // {
                    //
                    //     
                    //     // animation.Play(ComboAnimations[currentComboAttack], 1);
                    //     // currentComboAttack++;
                    // }
                    
                    return;
                }

                return;
            }

            if (states.TryGetState("Attack", out state))
            {
                if (currentAnimationFrame.currentFrameHit)
                {
                    var hitTargets = world.GetTargets(entity);

                    foreach (var hitTarget in hitTargets)
                    {
                        ref var hitComponent = ref world.GetComponent<HitPointsComponent>(hitTarget.entity);
                        hitComponent.hits.Add(new HitData
                        {
                            position = position.value,
                            knockback = currentComboAttack >= comboAttacks,
                            hitPoints = 1,
                            source = entity
                        });
                        
                        var targetPosition = world.GetComponent<PositionComponent>(hitTarget.entity);

                        // position.value = new Vector3(position.value.x, targetPosition.value.y - 0.1f, position.value.z);
                        
                        states.EnterState("Combo");

                        animationComponent.pauseTime = hitStopTime;
                        modelShakeComponent.Shake(hitStopTime, 0.25f);

                        teleportLastHitPosition = targetPosition.value;
                    }
                }
                
                // if (animation.playingTime >= currentAnimationFrame.cancellationTime 
                //     && currentComboAttack < comboAttacks
                //     && dashBackCooldownCurrent <= 0 
                //     && (control.HasBufferedActions(control.up.name, control.button2.name) ||
                //         control.HasBufferedActions(control.button2.name, control.up.name)))
                // {
                //     control.ConsumeBuffer();
                //     states.ExitState("Attack");
                //     states.ExitState("Combo");
                //     states.EnterState("DashBackJump");
                //     return;
                // }
                
                if (animationComponent.playingTime >= currentAnimationFrame.cancellationTime 
                    && currentComboAttack < comboAttacks
                    && dashBackCooldownCurrent <= 0 
                    && (control.HasBufferedActions(control.backward.name, control.button2.name) ||
                    control.HasBufferedActions(control.button2.name, control.backward.name)))
                {
                    control.ConsumeBuffer();
                    states.ExitState("Attack");
                    states.ExitState("Combo");
                    states.EnterState("DashBack");
                    return;
                }

                /*if (states.HasState("Combo") && animation.playingTime >= attackCancellationTime && 
                    (control.HasBufferedActions(control.forward.name, control.button2.name) ||
                    control.HasBufferedActions(control.button2.name, control.forward.name))*/
                
                if (animationComponent.HasAnimation("TeleportOut") && states.HasState("Combo") && animationComponent.playingTime >= currentAnimationFrame.cancellationTime && 
                    control.HasBufferedActions(control.button2.name)
                     && dashFrontCooldownCurrent <= 0
                    && currentComboAttack < comboAttacks)
                {
                    control.ConsumeBuffer();
                    
                    animationComponent.Play("TeleportOut", 1);
                    states.ExitState("Attack");
                    states.ExitState("Combo");
                    
                    states.EnterState("HiddenAttack");
                    return;
                }
                
                if (animationComponent.playingTime >= currentAnimationFrame.cancellationTime 
                    && currentComboAttack < comboAttacks
                    && dashFrontCooldown <= 0 
                    && control.HasBufferedActions(control.button2.name))
                {
                    control.ConsumeBuffer();
                    states.ExitState("Attack");
                    states.ExitState("Combo");
                    states.EnterState("DashFront");
                    return;
                }

                if (states.HasState("Combo") && animationComponent.playingTime >= currentAnimationFrame.cancellationTime && control.HasBufferedAction(control.button1) 
                    && currentComboAttack < comboAttacks)
                {
                    animationComponent.Play(comboAnimations[currentComboAttack], 1);

                    // if (control.HasBufferedActions(control.backward.name, control.button1.name))
                    // {
                    //     lookingDirection.value.x = -lookingDirection.value.x;
                    //     states.ExitState("Combo");
                    // }

                    currentComboAttack++;
                    control.ConsumeBuffer();
                    
                    return;
                }
            
                if (animationComponent.state == AnimationComponent.State.Completed)
                {
                    states.ExitState("Combo");
                    states.ExitState("Attack");

                    // if combo completed, then reset attack cooldown
                    if (currentComboAttack >= comboAttacks)
                    {
                        attackCooldownCurrent = attackCooldown;
                    }
                }

                return;
            }

            attackCooldownCurrent -= Time.deltaTime;
            
            if (control.HasBufferedAction(control.button1) && attackCooldownCurrent <= 0)
            {
                if (control.backward.isPressed)
                {
                    lookingDirection.value.x = -lookingDirection.value.x;
                }
                
                currentComboAttack = 0;
                
                animationComponent.Play("Attack1", 1);
                movement.movingDirection = Vector2.zero;
                control.ConsumeBuffer();
                states.EnterState("Attack");
                
                // states.EnterState("Combo");
                
                return;
            }

            if (dashFrontCooldownCurrent <= 0)
            {
                
                if (control.HasBufferedAction(control.button2))
                {
                    control.ConsumeBuffer();
                    states.EnterState("DashFront");
                    return;
                }
            }

            if (control.HasBufferedAction(control.button3))
            {
                if (control.backward.isPressed)
                {
                    lookingDirection.value.x = control.direction.x;
                }
                
                control.ConsumeBuffer();
                states.EnterState("RangeAttack");
                return;
            }
            
            movement.speed = movement.baseSpeed;
            movement.movingDirection = control.direction3d;

            if (states.HasState("Moving"))
            {
                if (!animationComponent.IsPlaying("Walk"))
                {
                    animationComponent.Play("Walk");
                }
                
                if (control.backward.isPressed)
                {
                    lookingDirection.value.x = control.direction.x;
                }
                
                if (control.direction.sqrMagnitude < Mathf.Epsilon)
                {
                    states.ExitState("Moving");
                    return;
                }
                return;
            }

            if (control.direction.sqrMagnitude > Mathf.Epsilon)
            {
                if (control.backward.isPressed)
                {
                    lookingDirection.value.x = control.direction.x;
                }
                
                // lookingDirection.locked = true;
                animationComponent.Play("Walk");
                states.EnterState("Moving");
                return;
            }

            if (!animationComponent.IsPlaying("Idle"))
            {
                animationComponent.Play("Idle");
            }
        }


    }
}