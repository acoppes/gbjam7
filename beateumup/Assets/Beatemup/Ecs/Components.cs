using System;
using System.Collections.Generic;
using Beatemup.Models;
using Gemserk.Leopotam.Ecs;
using UnityEngine;

namespace Beatemup.Ecs
{
    public struct PlayerInputComponent : IEntityComponent
    {
        public bool disabled;
        public int playerInput;
    }

    public struct Button
    {
        public string name;
        
        public bool isPressed;

        public bool wasPressedThisFrame;

        public Button(string name)
        {
            this.name = name;
            isPressed = false;
            wasPressedThisFrame = false;
        }

        public void UpdatePressed(bool pressed)
        {
            wasPressedThisFrame = !isPressed && pressed;
            isPressed = pressed;
        }

        public override string ToString()
        {
            return $"{name}:{isPressed}";
        }
    }
    
    public struct ControlComponent : IEntityComponent
    {
        public const int MaxBufferCount = 15;
        
        public Vector2 direction;
        
        public Button right;
        public Button left;
        public Button up;
        public Button down;
        
        public Button forward;
        public Button backward;
        
        public Button button1;
        public Button button2;

        public List<string> buffer;
        public float bufferTime;

        public static ControlComponent Default()
        {
            return new ControlComponent()
            {
                right = new Button(nameof(right)),
                left = new Button(nameof(left)),
                up = new Button(nameof(up)),
                down = new Button(nameof(down)),
                forward = new Button(nameof(forward)),
                backward = new Button(nameof(backward)),
                button1 = new Button(nameof(button1)),
                button2 = new Button(nameof(button2)),
                buffer = new List<string>()
            };
        }

        public bool HasBufferedAction(Button button)
        {
            return HasBufferedActions(button.name);
        }

        public bool HasBufferedActions(params string[] actions)
        {
            if (actions.Length == 0)
            {
                return false;
            }

            var bufferStart = buffer.Count - actions.Length;
            
            if (bufferStart < 0)
            {
                return false;
            }

            for (var i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                if (!buffer[bufferStart + i].Equals(action, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public void ConsumeBuffer()
        {
            buffer.Clear();
        }
    }
    
    public struct LookingDirection : IEntityComponent
    {
        public Vector2 value;
        public bool locked;
    }

    public struct UnitModelComponent : IEntityComponent
    {
        public enum Visiblity
        {
            Visible = 0,
            Hidden = 1
        }
        
        public GameObject prefab;
        public Model instance;

        public bool rotateToDirection;

        public Visiblity visiblity;

        public bool IsVisible => visiblity == Visiblity.Visible;

        public bool hasShadow;
    }
    
    public struct HorizontalMovementComponent : IEntityComponent
    {
        public bool disabled;
        public float speed;
        
        public Vector2 extraSpeed;
        public Vector2 currentVelocity;
        public Vector2 movingDirection;
    }
    
    public struct VerticalMovementComponent : IEntityComponent
    {
        public bool isOverGround;
        public float speed;
    }

    public struct GravityComponent : IEntityComponent
    {
        public bool disabled;
        public float scale;
    }
    
    public struct JumpComponent : IEntityComponent
    {
        public bool isActive;
        
        public float upSpeed;
        public float upTime;
    }
    
    public struct CurrentAnimationFrameComponent : IEntityComponent
    {
        public int animation;
        public int frame;
        
        public bool hit;
    }

    public struct HitData
    {
        public Vector3 position;
    }

    public struct HitComponent : IEntityComponent
    {
        public List<HitData> hits;
        public event Action<World, Entity, HitComponent> OnHitEvent;

        public void OnHit(World world, Entity entity)
        {
            OnHitEvent?.Invoke(world, entity, this);
        }
    }

    public struct VfxComponent : IEntityComponent
    {
        public float delay;
    }

    public struct DestroyableComponent : IEntityComponent
    {
        public bool destroy;
    }
}