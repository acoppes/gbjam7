using Gemserk.Leopotam.Ecs;
using Leopotam.EcsLite;

namespace GBJAM9.Ecs
{
    public class PlayerControlSystem : BaseSystem, IEcsRunSystem
    {
        public void Run(EcsSystems systems)
        {
            var filter = world.GetFilter<UnitControlComponent>()
                .Inc<PlayerInputComponent>().End();
            
            var controlComponents = world.GetComponents<UnitControlComponent>();
            var playerInputComponents = world.GetComponents<PlayerInputComponent>();
            var lookingDirectionComponents = world.GetComponents<LookingDirection>();
            
            foreach (var entity in filter)
            {
                ref var control = ref controlComponents.Get(entity);
                var playerInputComponent = playerInputComponents.Get(entity);

                if (!playerInputComponent.disabled)
                {
                    control.direction = playerInputComponent.keyMap.direction;

                    // if (playerInputComponent.keyMap.direction.SqrMagnitude() > 0)
                    // {
                    //     control.attackDirection = playerInputComponent.keyMap.direction;
                    // }

                    control.mainAction = playerInputComponent.keyMap.button1Pressed;
                    control.secondaryAction = playerInputComponent.keyMap.button2Pressed;
                }
            }
            
            // Update looking direction based on controls
            foreach (var entity in world.GetFilter<UnitControlComponent>().Inc<LookingDirection>().End())
            {
                var control = controlComponents.Get(entity);
                ref var lookingDirection = ref lookingDirectionComponents.Get(entity);

                if (control.direction.sqrMagnitude > 0f)
                {
                    lookingDirection.value = control.direction.normalized;
                }
            }
        }
    }
}
