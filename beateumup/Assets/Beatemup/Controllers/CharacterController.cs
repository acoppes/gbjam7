using Beatemup.Ecs;
using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Controllers;
using UnityEngine;

namespace Beatemup.Controllers
{
    public class CharacterController : ControllerBase, IInit, IEntityDestroyed
    {
        private static readonly string[] ComboAnimations = 
        {
            "Attack2", "Attack3", "AttackFinisher"
        };

        private const string DashState = "Dash";
        private const string DashStopState = "DashStop";
        
        private const string SprintState = "Sprint";

        public float attackCancelationTime = 0.1f;

        public float dashDuration = 1.0f;
        public float dashExtraSpeed = 10.0f;
        
        public float sprintExtraSpeed = 2.0f;

        public float heavySwingStartTime = 0.5f;
        public float heavySwingChargeTime = 0.25f;

        private float pressedAttackTime = 0;

        private int comboAttacks => ComboAnimations.Length;
        private int currentComboAttack;

        public void OnInit()
        {
            ref var lookingDirection = ref world.GetComponent<LookingDirection>(entity);
            lookingDirection.locked = true;
            
            ref var animationComponent = ref world.GetComponent<AnimationComponent>(entity);
            animationComponent.Play("Idle");
        }
        
        public void OnEntityDestroyed(Entity e)
        {

        }

        public override void OnUpdate(float dt)
        {
            var control = world.GetComponent<ControlComponent>(entity);
            ref var movement = ref world.GetComponent<UnitMovementComponent>(entity);
            // ref var modelState = ref world.GetComponent<ModelStateComponent>(entity);
            ref var animation = ref world.GetComponent<AnimationComponent>(entity);
            ref var states = ref world.GetComponent<StatesComponent>(entity);
            
            ref var lookingDirection = ref world.GetComponent<LookingDirection>(entity);

            if (states.HasState("HeavySwing"))
            {
                if (animation.IsPlaying("HeavySwingAttack"))
                {
                    if (animation.state == AnimationComponent.State.Completed)
                    {
                        animation.Play("Idle");
                        states.ExitState("HeavySwing");
                    }
                }
                
                if (animation.IsPlaying("HeavySwingFirstStrike"))
                {
                    if (animation.state == AnimationComponent.State.Completed)
                    {
                        animation.Play("HeavySwingAttack", 1);
                    }
                }
                
                if (animation.IsPlaying("HeavySwingHold"))
                {
                    if (!control.button1.isPressed)
                    {
                        animation.Play("HeavySwingFirstStrike", 1);
                        return;
                    }
                }
                
                if (animation.IsPlaying("HeavySwingCharging"))
                {
                    if (!control.button1.isPressed)
                    {
                        animation.Play("Idle");
                        states.ExitState("HeavySwing");
                        return;
                    }

                    if (animation.playingTime > heavySwingChargeTime)
                    {
                        animation.Play("HeavySwingHold");
                    }
                }
                
                if (animation.IsPlaying("HeavySwingStartup"))
                {
                    if (!control.button1.isPressed)
                    {
                        animation.Play("Idle");
                        states.ExitState("HeavySwing");
                        return;
                    }
                    
                    if (animation.state == AnimationComponent.State.Completed)
                    {
                        animation.Play("HeavySwingCharging");
                    }
                }

                return;
            }

            if (states.HasState("SprintStop"))
            {
                if (animation.state == AnimationComponent.State.Completed || control.HasBufferedAction(control.button1))
                {
                    animation.Play("Idle");
                    states.ExitState("SprintStop");
                }
                return;
            }
            
            if (states.HasState(DashStopState))
            {
                if (animation.state == AnimationComponent.State.Completed || control.HasBufferedAction(control.button1))
                {
                    animation.Play("Idle");
                    states.ExitState(DashStopState);
                }

                return;
            }

            if (states.HasState("Backkick"))
            {
                if (animation.state == AnimationComponent.State.Completed)
                {
                    lookingDirection.value.x = -lookingDirection.value.x;
                    animation.Play("Idle");
                    states.ExitState("Backkick");
                }

                return;
            }
            
            if (states.HasState("Attack"))
            {
                var state = states.GetState("Attack");

                if (state.time >= attackCancelationTime &&
                    control.HasBufferedActions(control.button1.name, control.backward.name))
                {
                    control.ConsumeBuffer();
                    
                    animation.Play("Backkick", 1);
                    states.ExitState("Attack");
                    states.ExitState("Combo");
                    states.EnterState("Backkick");
                    return;
                }

                if (states.HasState("Combo") && state.time >= attackCancelationTime && control.HasBufferedAction(control.button1) 
                    && currentComboAttack < comboAttacks)
                {
                    animation.Play(ComboAnimations[currentComboAttack], 1);
                    
                    state.time = 0;
                    
                    if (control.HasBufferedActions(control.backward.name, control.button1.name))
                    {
                        lookingDirection.value.x = -lookingDirection.value.x;
                        states.ExitState("Combo");
                    }

                    currentComboAttack++;
                    control.ConsumeBuffer();
                    
                    return;
                }
            
                if (animation.state == AnimationComponent.State.Completed)
                {
                    animation.Play("Idle");
                    states.ExitState("Combo");
                    states.ExitState("Attack");
                }

                return;
            }

            if (states.HasState(DashState))
            {
                var state = states.GetState(DashState);
                
                if (state.time > dashDuration)
                {
                    movement.movingDirection = Vector2.zero;
                    // modelState.dashing = false;
                    animation.Play("DashStop", 1);
                    movement.extraSpeed.x = 0;
                    states.ExitState(DashState);
                    states.EnterState(DashStopState);
                }
                
                // ref var position = ref world.GetComponent<PositionComponent>(entity);
                // position.value = new Vector2(-position.value.x, position.value.y);
                
                return;
            }
            
            pressedAttackTime -= dt;

            if (!control.button1.isPressed)
            {
                pressedAttackTime = heavySwingStartTime;
            }

            if (pressedAttackTime <= 0)
            {
                control.ConsumeBuffer();
                    
                states.ExitState("Attack");
                states.ExitState("Combo");
                    
                animation.Play("HeavySwingStartup", 1);
                states.EnterState("HeavySwing");
                    
                return;
            }
            
            if (control.HasBufferedAction(control.button1))
            {
                currentComboAttack = 0;
                
                if (Mathf.Abs(movement.currentVelocity.x) > Mathf.Epsilon)
                {
                    animation.Play("AttackMoving", 1);
                }
                else
                {
                    animation.Play("Attack", 1);
                }
                
                movement.movingDirection = Vector2.zero;
                
                control.ConsumeBuffer();

                states.EnterState("Attack");
                states.EnterState("Combo");
                
                return;
            }
            
            if (control.HasBufferedAction(control.button2))
            {
                control.ConsumeBuffer();
                
                states.ExitState(SprintState);
                movement.extraSpeed.x = 0;
                
                movement.movingDirection = new Vector2(lookingDirection.value.x, 0);
                animation.Play("Dash", 1);
                movement.extraSpeed.x = dashExtraSpeed;
                states.EnterState(DashState);
                
                return;
            }

            if (states.HasState(SprintState))
            {
                if ((!control.right.isPressed && !control.left.isPressed) || 
                    (control.right.isPressed && control.left.isPressed) || control.backward.isPressed)
                {
                    // modelState.sprinting = false;
                    movement.extraSpeed.x = 0;
                    movement.movingDirection = Vector2.zero;
                    
                    animation.Play("SprintStop", 1);
                    states.ExitState(SprintState);
                    states.EnterState("SprintStop");
                    return;
                }
                
                movement.movingDirection = control.direction;

                return;
            }
            else
            {
                if (control.HasBufferedActions(control.forward.name, control.forward.name))
                {
                    control.ConsumeBuffer();
                    
                    animation.Play("Sprint");
                    // modelState.sprinting = true;
                    movement.extraSpeed.x = sprintExtraSpeed;
                    states.EnterState(SprintState);
                    states.ExitState("Moving");
                    return;
                }
                
                // if (control.right.isPressed != control.left.isPressed)
                // {
                //
                // }
            }
            
            movement.movingDirection = control.direction;

            if (states.HasState("Moving"))
            {
                if (control.backward.isPressed)
                {
                    lookingDirection.value.x = control.direction.x;
                }
                
                if (control.direction.sqrMagnitude < Mathf.Epsilon)
                {
                    // lookingDirection.locked = false;
                    animation.Play("Idle");
                    states.ExitState("Moving");
                    return;
                }

                if (control.direction.y > 0 && !animation.IsPlaying("WalkUp"))
                {
                    animation.Play("WalkUp");
                } else if (control.direction.y <= 0 && !animation.IsPlaying("Walk"))
                {
                    animation.Play("Walk");
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
                animation.Play("Walk");
                states.EnterState("Moving");
                return;
            }
        }

    }
}