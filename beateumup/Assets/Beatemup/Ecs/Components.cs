using System.Collections.Generic;
using Gemserk.Leopotam.Ecs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beatemup.Ecs
{
    public struct PlayerInputComponent : IEntityComponent
    {
        public bool disabled;
        public int playerInput;
    }

    public struct Button
    {
        public const int DoubleTapFrames = 15;

        public bool isPressed;

        private int lastPressedFrame;
        
        public bool wasPressedThisFrame;

        public bool doubleTap;

        public Button(int buffer)
        {
            // current = 0;
            isPressed = false;

            lastPressedFrame = 0;
            doubleTap = false;

            wasPressedThisFrame = false;
        }

        public void UpdatePressed(bool pressed)
        {
            wasPressedThisFrame = !isPressed && pressed;
            // wasReleased = pressedBuffer[current] && !pressed;
            
            lastPressedFrame--;

            isPressed = pressed;

            if (wasPressedThisFrame)
            {
                doubleTap = lastPressedFrame > 0;
                lastPressedFrame = DoubleTapFrames;
            }
        }

        public void ClearBuffer()
        {
            doubleTap = false;
            lastPressedFrame = 0;
        }

        public static Button Default()
        {
            return new Button(8);
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
                right = Button.Default(),
                left = Button.Default(),
                up = Button.Default(),
                down = Button.Default(),
                forward = Button.Default(),
                backward = Button.Default(),
                button1 = Button.Default(),
                button2 = Button.Default(),
                buffer = new List<string>()
            };
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
        public GameObject instance;

        public Transform subModel;

        public bool rotateToDirection;

        public Visiblity visiblity;

        public bool IsVisible => visiblity == Visiblity.Visible;
    }
    
    public struct UnitMovementComponent : IEntityComponent
    {
        public bool disabled;
        
        public float speed;

        public Vector2 extraSpeed;

        public Vector2 currentVelocity;

        public Vector2 movingDirection;
    }

    public struct KeepInsideCameraComponent : IEntityComponent
    {
        
    }
    
    public struct StateTriggers
    {
        public bool hit;
    }
    
    public struct ModelStateComponent : IEntityComponent
    {
        public bool walking;
        public bool up;
        public bool dashing;
        public bool sprinting;
        
        public bool attack;
        public bool attackMoving;

        public StateTriggers stateTriggers;

        public bool disableAutoUpdate;
    }

    public struct AnimatorComponent : IEntityComponent
    {
        public Animator animator;
    }
    
    public struct TerrainCollisionComponent : IEntityComponent
    {
        public Vector2 lastValidPosition;
    }

    public struct UnitTypeComponent : IEntityComponent
    {
        public int type;
    }
}