using System;
using System.Collections.Generic;
using GBJAM9.Ecs;
using Gemserk.Leopotam.Ecs;
using Gemserk.Leopotam.Ecs.Controllers;
using Gemserk.Leopotam.Ecs.Gameplay;
using UnityEngine;

public class UnitDefinition : MonoBehaviour, IEntityDefinition
{
    public float movementSpeed;
    public float health;

    public bool showLookingDirection = true;

    public bool canBeControlled = true;
    public bool canBeTargeted = true;

    public bool autoDestroyOnDeath = true;

    public float colliderRadius = 0f;
    public bool collidesWithTerrain = true;
    
    public GameObject modelPrefab;

    public void Apply(World world, int entity)
    {
        world.AddComponent(entity, new PlayerComponent());
        world.AddComponent(entity, new PositionComponent());
        
        world.AddComponent(entity, new LookingDirection
        {
            value = Vector2.right, 
            disableIndicator = !showLookingDirection
        });

        if (canBeControlled)
        {
            world.AddComponent(entity, new PlayerInputComponent());
            world.AddComponent(entity, new UnitControlComponent());
        }

        world.AddComponent(entity, new UnitStateComponent());
        world.AddComponent(entity, new AnimatorComponent());
        
        world.AddComponent(entity, new StatesComponent());

        world.AddComponent(entity, new UnitModelComponent
        {
            prefab = modelPrefab
        });

        world.AddComponent(entity, new AbilitiesComponent
        {
            abilities = new List<Ability>(),
            targetings = new List<Targeting>()
        });

        world.AddComponent(entity, new UnitMovementComponent()
        {
            speed = movementSpeed
        });
        
        if (canBeTargeted)
        {
            world.AddComponent(entity, new TargetComponent());
        }
        
        if (health > 0)
        {
            world.AddComponent(entity, new HealthComponent
            {
                current = health,
                total = health,
                autoDestroyOnDeath = autoDestroyOnDeath
            });
        }

        if (colliderRadius > 0)
        {
            world.AddComponent(entity, new ColliderComponent
            {
                radius = colliderRadius,
                collisions = new Collider2D[10]
            });
        }

        if (collidesWithTerrain)
        {
            world.AddComponent(entity, new TerrainCollisionComponent());
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, colliderRadius);
    }
}