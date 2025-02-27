using System;
using System.Collections.Generic;
using System.Linq;
using GBJAM.Commons;
using GBJAM9.Components;
using GBJAM9.Controllers;
using UnityEngine;

namespace GBJAM9
{
    public class World : MonoBehaviour
    {
        [NonSerialized]
        public readonly List<Entity> entities = new List<Entity>();

        private readonly int vfxDoneHash = Animator.StringToHash("Done");

        public Action<Entity> onPickup;
        
        private int playerLayer;
        private int enemyLayer;

        private int playerProjectilesLayer;
        private int enemyProjectilesLayer;

        public List<T> GetComponentList<T>() where T : IEntityComponent
        {
            return entities.Where(u => u.GetComponent<T>() != null)
                .Select(u => u.GetComponent<T>()).ToList();
        }
        
        public List<Entity> GetEntitiesWith<T>() where T : IEntityComponent
        {
            return entities.Where(e => e.GetComponent<T>() != null)
                .ToList();
        }
        
        public List<Entity> GetEntitiesWith<T1, T2>() 
            where T1 : IEntityComponent 
            where T2 : IEntityComponent
        {
            return entities
                .Where(e => e.GetComponent<T1>() != null)
                .Where(e => e.GetComponent<T2>() != null)
                .ToList();
        }

        public Entity GetSingleton(string name)
        {
            return entities
                .FirstOrDefault(e => e.singleton != null && e.singleton.uniqueName.Equals(name));
        }

        private void Awake()
        {
            playerLayer = LayerMask.NameToLayer("Player");
            enemyLayer = LayerMask.NameToLayer("Enemy");
            playerProjectilesLayer = LayerMask.NameToLayer("Player_Attack");
            enemyProjectilesLayer = LayerMask.NameToLayer("Enemy_Attack");
        }

        public void Update()
        {
            // perform general logics in order
            var toDestroy = new List<Entity>();

            var mainUnits = entities.Where(u => u.GetComponent<MainUnitComponent>() != null)
                .Select(u => u.GetComponent<MainUnitComponent>()).ToList();

            var iterationList = new List<Entity>(entities);
            
            foreach (var e in iterationList)
            {
                if (e.player != null)
                {
                    e.player.layer = e.player.player == 0 ? playerLayer : enemyLayer;
                    e.player.enemyLayer = e.player.player == 0 ? enemyLayer : playerLayer;
                    e.player.projectileLayer = e.player.player == 0 ? playerProjectilesLayer : enemyProjectilesLayer;

                    e.player.layerMask = LayerMask.GetMask(e.player.player == 0 ? "Player" : "Enemy");
                    e.player.enemyLayerMask = LayerMask.GetMask(e.player.player == 0 ? "Enemy" : "Player");
                }

                if (e.controller != null)
                {
                    if (e.controller.controllerObject is EntityController controllerObject)
                    {
                        if (!e.controller.initialized)
                        {
                            controllerObject.entity = e;
                            controllerObject.OnInit(this);
                            e.controller.initialized = true;
                        }

                        controllerObject.OnWorldUpdate(this);
                    }
                }

                if (e.model != null && e.model.optionalStartLookAt != null)
                {
                    e.model.lookingDirection = e.model.optionalStartLookAt.localPosition.normalized;
                    
                    Destroy(e.model.optionalStartLookAt.gameObject);
                    e.model.optionalStartLookAt = null;
                }

                if (e.state != null)
                {
                    // reset the hit state
                    e.state.hit = false;
                    e.state.dead = false;
                }
                
                var health = e.health;
                if (health != null)
                {
                    health.disableDamageCurrentTime -= Time.deltaTime;
                    
                    if (health.current > 0)
                    {
                        var damageDisabled = health.disableDamageCurrentTime > 0;
                        
                        var receivedDamage = health.damages > 0 && !damageDisabled;

                        if (receivedDamage && !health.immortal)
                        {
                            health.current -= health.damages;
                            health.disableDamageCurrentTime = health.disableDamageAfterHitDuration;
                        }
                        
                        health.alive = health.current > 0;
                        health.damages = 0;

                        if (e.state != null)
                        {
                            e.state.hit = receivedDamage;

                            if (!health.alive)
                            {
                                e.state.dead = true;

                                if (health.vfxPrefab != null)
                                {
                                    var vfxObject = GameObject.Instantiate(e.health.vfxPrefab);
                                    vfxObject.transform.position = e.health.vfxAttachPoint.position;
                                }
                            }

                            e.state.invulnerable = damageDisabled;
                        }

                        if (receivedDamage && health.current == 1)
                        {
                            if (e.sfxContainer != null && e.sfxContainer.lowHealthSfx != null)
                            {
                                e.sfxContainer.lowHealthSfx.Play();
                            }                            
                        }
                    }

                    health.current = Mathf.Clamp(health.current, 0, health.total);

                }
                
                // TODO: blink animation state

                var soundEffect = e.GetComponent<SoundEffectComponent>();
                if (soundEffect != null)
                {
                    if (!soundEffect.started)
                    {
                        soundEffect.sfx.Play();
                        soundEffect.started = true;
                    }
                    else if (!soundEffect.sfx.isPlaying)
                    {
                        toDestroy.Add(e);
                    }
                }

                if (e.input != null && e.gameboyController != null)
                {
                    var gameboyKeyMap = GameboyInput.Instance.current;
                    
                    e.input.movementDirection = gameboyKeyMap.direction;
                    if (gameboyKeyMap.direction.SqrMagnitude() > 0)
                    {
                        e.input.attackDirection = gameboyKeyMap.direction;
                    }
                    e.input.attack = gameboyKeyMap.button1JustPressed;
                    e.input.dash = gameboyKeyMap.button2JustPressed;
                }
                
                if (e.input != null)
                {
                    if (e.health != null && !e.health.alive)
                    {
                        e.input.enabled = false;
                    }
                    
                    if (e.movement != null)
                    {
                        if (e.input.enabled)
                        {
                            e.movement.movingDirection = e.input.movementDirection;
                        }
                        else
                        {
                            e.movement.movingDirection = Vector2.zero;
                        }
                    }
                    
                    if (e.attack != null)
                    {
                        if (e.input.enabled)
                        {
                            e.attack.direction = e.input.attackDirection;
                        }
                    }
                    
                    if (e.state != null)
                    {
                        e.state.walking = e.input.enabled && e.input.movementDirection.SqrMagnitude() > 0;
                    }
                }

                if (e.movement != null)
                {
                    var speed = e.movement.speed;
                    var direction = e.movement.movingDirection;

                    if (e.state != null && e.dash != null && e.state.dashing)
                    {
                        speed = e.dash.speed;
                        // direction = e.movement.lookingDirection;
                        direction = e.dash.direction;
                    }
                    
                    var newPosition = e.transform.localPosition;

                    var velocity = direction * speed;

                    velocity = new Vector2(
                        velocity.x * e.movement.perspective.x, 
                        velocity.y * e.movement.perspective.y);
                    
                    e.collider.rigidbody.velocity = velocity;

                    e.transform.localPosition = newPosition;

                    if (velocity.SqrMagnitude() > 0)
                    {
                        var movingDirection = velocity.normalized;
                        e.movement.lookingDirection = movingDirection;
                        
                        // if (e.attack != null)
                        // {
                        //     e.attack.direction = movingDirection;
                        // }
                        
                        if (e.sfxContainer != null && e.sfxContainer.walkSfx != null)
                        {
                            e.sfxContainer.walkSfx.Play();
                        }    
                    }
                }
                
                if (e.attack != null)
                {
                    e.attack.cooldown -= Time.deltaTime;

                    if (e.state != null)
                    {
                        e.state.swordAttacking = false;
                        e.state.kunaiAttacking = false;
                    }
                    
                    var weaponData = e.attack.weaponData;
                    
                    if (weaponData != null && e.input != null)
                    {
                        var projectilePrefab = weaponData.projectilePrefab;
                        if (e.input.enabled && e.input.attack && projectilePrefab != null && e.attack.cooldown < 0)
                        {
                            var projectileObject = GameObject.Instantiate(projectilePrefab);
                            var projectileEntity = projectileObject.GetComponent<Entity>();
                            var projectileController = projectileObject.GetComponent<ProjectileController>();
                            projectileController.Fire(e.transform.position + e.attack.attackAttachPoint.localPosition,
                                e.attack.direction);
                            projectileController.entity.player.player = e.player.player;
                            
                            projectileEntity.projectile.damage = weaponData.damage + e.attack.extraDamage;
                            
                            if (e.player.player == 0)
                            {
                                projectileController.gameObject.layer = playerProjectilesLayer;
                            }
                            else
                            {
                                projectileController.gameObject.layer = enemyProjectilesLayer;
                            }

                            if (e.state != null)
                            {
                                e.state.kunaiAttacking = weaponData.attackType.Equals("kunai");
                                e.state.swordAttacking = weaponData.attackType.Equals("sword");
                            }
                            
                            e.attack.cooldown = weaponData.cooldown;
                        }
                    }
                }
                
                if (e.model != null)
                {
                    if (e.movement != null)
                    {
                        e.model.lookingDirection = e.movement.lookingDirection;
                    }
                    
                    if (e.attack != null && e.state != null && (e.state.chargeAttack1 || e.state.chargeAttack2 || e.state.kunaiAttacking ||
                        e.state.swordAttacking))
                    {
                        e.model.lookingDirection = e.attack.direction;
                    }
                }

                if (e.roomExit != null)
                {
                    e.roomExit.playerInExit = false;

                    if (e.roomExit.open)
                    {
                        // TODO: change to collider
                        foreach (var mainUnit in mainUnits)
                        {
                            if (Vector2.Distance(mainUnit.transform.position, e.roomExit.transform.position) <
                                e.roomExit.distance)
                            {
                                e.roomExit.playerInExit = true;
                                break;
                            }
                        }
                    }
                    
                    if (e.state != null)
                    {
                        e.state.dead = e.roomExit.open;
                    }
                }

                if (e.collider != null)
                {
                    e.collider.contactsList.Clear();
                    e.collider.collidingEntities.Clear();

                    e.collider.inCollision = false;
                    
                    var updateCollider = true;
                    
                    if (e.health != null && !e.health.alive)
                    {
                        updateCollider = false;
                        
                        if (e.collider.rigidbody != null)
                        {
                            e.collider.rigidbody.bodyType = RigidbodyType2D.Kinematic;
                        }

                        if (e.collider.collider != null)
                        {
                            e.collider.collider.enabled = false;
                        }
                        // turn off collider and rigid body too
                    }
                    
                    if (updateCollider)
                    {
                        e.collider.inCollision =
                            e.collider.collider.GetContacts(e.collider.contactsList) > 0;

                        // filter duplicates?
                        e.collider.collidingEntities = e.collider.contactsList
                            .Where(c => c.collider.GetComponent<Entity>() != null)
                            .Select(c => c.collider.GetComponent<Entity>())
                            .Distinct()
                            .ToList();
                    }
                }

                if (e.pickup != null)
                {
                    if (e.collider != null)
                    {
                        if (e.collider.inCollision)
                        {
                            var contactUnit = e.collider.contactsList[0].collider.GetComponent<Entity>();
                            
                            if (e.pickup.pickupVfxPrefab != null)
                            {
                                var pickupVfx = GameObject.Instantiate(e.pickup.pickupVfxPrefab);
                                pickupVfx.transform.position = e.transform.position;
                            }

                            e.pickup.picked = true;
                            
                            e.SendMessage("OnPickup", contactUnit, SendMessageOptions.DontRequireReceiver);

                            e.destroyed = true;

                            onPickup?.Invoke(e);
                        }
                    }
                }

                if (e.projectile != null && e.collider != null && !e.projectile.damagePerformed)
                {
                    if (e.collider.inCollision)
                    {
                        foreach (var otherEntity in e.collider.collidingEntities)
                        {
                            if (otherEntity != null && e.projectile.totalTargets > 0)
                            {
                                if (otherEntity.player == null)
                                    continue;
                                
                                if (otherEntity.player.player == e.player.player)
                                    continue;
                
                                if (otherEntity.health != null)
                                {
                                    otherEntity.health.damages += e.projectile.damage;
                                    e.projectile.totalTargets--;
                                }
                            }
                        }
                        
                        e.destroyed = true;
                        e.projectile.damagePerformed = true;

                        if (e.projectile.hitSfxPrefab != null)
                        {
                            Instantiate(e.projectile.hitSfxPrefab, transform.position, Quaternion.identity);
                        }
                    }
                }

                if (e.vfx != null && !e.vfx.sfxSpawned)
                {
                    if (e.vfx.sfxVariant != null)
                    {
                        e.vfx.sfxVariant.Play();
                    }
                    e.vfx.sfxSpawned = true;
                }

                if (e.vfx != null && e.model != null)
                {
                    e.destroyed =
                        e.model.animator.GetCurrentAnimatorStateInfo(0).shortNameHash == vfxDoneHash;
                }

                if (e.decoComponent != null)
                {
                    // round positions to pixel perfect...
                    // e.transform.position.
                }

                if (e.destroyed)
                {
                    toDestroy.Add(e);
                }
                
                
            }

            foreach (var unit in toDestroy)
            {
                Destroy(unit.gameObject);
            }
        }

        private void LateUpdate()
        {
            var iterationList = new List<Entity>(entities);

            foreach (var e in iterationList)
            {
                if (e.model != null)
                {
                    var animator = e.model.animator;
                    var state = e.state;
                    
                    if (animator != null && state != null)
                    {
                        animator.SetBool(UnitStateComponent.walkingStateHash, state.walking);
                        animator.SetBool(UnitStateComponent.kunaiAttackStateHash, state.kunaiAttacking);
                        animator.SetBool(UnitStateComponent.swordAttackStateHash, state.swordAttacking);
                        animator.SetBool(UnitStateComponent.dashingStateHash, state.dashing);
                        animator.SetBool(UnitStateComponent.chargeAttack1StateHash, state.chargeAttack1);
                        animator.SetBool(UnitStateComponent.chargeAttack2StateHash, state.chargeAttack2);
                        animator.SetBool(UnitStateComponent.invulnerableStateHash, state.invulnerable);
                        
                        // animator.SetBool(UnitStateComponent.deadStateHash, state.dead);
                        
                        if (state.hit)
                        {
                            animator.SetTrigger(UnitStateComponent.hittedStateHash);
                        }

                        if (state.dead)
                        {
                            animator.SetTrigger(UnitStateComponent.deadStateHash);
                            if (e.sfxContainer != null && e.sfxContainer.deathSfx != null)
                            {
                                e.sfxContainer.deathSfx.Play();
                            }
                        }
                    }

                    if (!e.model.rotateToDirection)
                    {
                        var scale = e.model.transform.localScale;

                        if (Mathf.Abs(e.model.lookingDirection.x) > 0)
                        {
                            // e.model.model.flipX = e.model.lookingDirection.x < 0;
                            scale.x = e.model.lookingDirection.x < 0 ? -1 : 1;
                        }

                        if (e.model.verticalFlip && Mathf.Abs(e.model.lookingDirection.y) > 0)
                        {
                            // e.model.model.flipY = e.model.lookingDirection.y > 0;
                            scale.y = e.model.lookingDirection.y > 0 ? -1 : 1;
                        }

                        e.model.transform.localScale = scale;
                    }
                    else
                    {
                        var angle = Mathf.Atan2(e.model.lookingDirection.y, e.model.lookingDirection.x) * Mathf.Rad2Deg;
                        // if angle > 180 < 270, flip model?
                        e.model.model.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    }
                }
            }
        }
    }
}