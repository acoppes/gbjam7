using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GBJAM.Commons.Prefabs.Sfx;
using GBJAM.Commons.Transitions;
using GBJAM9.Components;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace GBJAM9
{
    public class CurrentRunData
    {
        public RoomComponent currentRoom;
        public List<GameObject> generatedRooms;
        public int secretRooms;
        public int totalRooms;
    }
    
    public class GameController : MonoBehaviour
    {
        public CameraFollow cameraFollow;

        public GameObject mainPlayerUnitPrefab;

        public RoomDataAsset rooms;
        
        public GameObject roomExitUnitPrefab;
        
        public AudioSource backgroundMusicAudioSource;
        
        public GameObject transitionPrefab;

        [FormerlySerializedAs("entityManager")] 
        public World world;

        public float delayBetweenRooms = 0.5f;

        public int roomIncrementPerVictory = 3;
        
        public int minRooms, maxRooms;
        
        public int minEnemiesPerRoom, maxEnemiesPerRoom;
        public int enemiesIncrementPerRun;

        private int extraEnemies;
        
        private int extraRooms;

        private Entity nekoninEntity;

        private CurrentRunData runData = new CurrentRunData();

        private RoomComponent currentRoom
        {
            get => runData.currentRoom;
            set => runData.currentRoom = value;
        }
        
        private List<Entity> roomExitUnits = new List<Entity>();
        private Entity gameEntity;

        private Entity hud;
        
        public int initialHealth = 2;

        public SfxVariant defeatSfx;

        private int currentRun;

        public VictorySequence victorySequence;
        
        public void Start()
        {
            gameEntity = world.GetSingleton("Game");
            hud = world.GetSingleton("GameHud");
            
            // This controller could be an entity too...

            currentRun = 0;

            // Start game sequence as coroutine?
            StartCoroutine(RestartGame(false, false));
        }
        
        
        private void RestartMusic(GameComponent.State nextState)
        {
            var musicClip = currentRoom.completedMusic;
            
            if (gameEntity.game.state == GameComponent.State.Fighting)
            {
                musicClip = currentRoom.fightMusic;
            }
            
            if (backgroundMusicAudioSource != null)
            {
                backgroundMusicAudioSource.loop = true;

                if (backgroundMusicAudioSource.clip == musicClip 
                    && backgroundMusicAudioSource.isPlaying)
                {
                    return;
                }

                backgroundMusicAudioSource.clip = musicClip;
                backgroundMusicAudioSource.Play();
            }
        }

        private IEnumerator RestartGame(bool disableTransition, bool victory)
        {
            gameEntity.game.state = GameComponent.State.Restarting;

            GameObject transitionObject = null;

            hud.hud.visible = false;

            var transitionPosition = Vector2.zero;

            if (nekoninEntity != null)
            {
                transitionPosition = nekoninEntity.transform.position;
            }

            if (!victory)
            {
                if (nekoninEntity != null)
                {
                    GameObject.Destroy(nekoninEntity.gameObject);
                    nekoninEntity = null;
                }
            }

            if (!disableTransition)
            {
                transitionObject = GameObject.Instantiate(transitionPrefab);
                transitionObject.transform.position = transitionPosition;

                var transition = transitionObject.GetComponent<Transition>();
                transition.Open();

                yield return new WaitWhile(delegate
                {
                    return !transition.isOpen;
                });

                yield return new WaitForSeconds(delayBetweenRooms);
            }
            
            if (nekoninEntity == null)
            {
                var unitObject = GameObject.Instantiate(mainPlayerUnitPrefab);
                nekoninEntity = unitObject.GetComponent<Entity>();
                nekoninEntity.health.total = initialHealth;
            }

            if (currentRoom != null)
            {
                GameObject.Destroy(currentRoom.gameObject);
            }

            var roomObject = GameObject.Instantiate(rooms.startingRoomPrefab);
            currentRoom = roomObject.GetComponent<RoomComponent>();
            nekoninEntity.transform.position = currentRoom.roomStart.transform.position;
            
            cameraFollow.followTransform = nekoninEntity.transform;

            if (!disableTransition)
            {
                transitionObject.transform.position = currentRoom.roomStart.transform.position;
                var transition = transitionObject.GetComponent<Transition>();
                
                transition.Close();
            
                yield return new WaitWhile(delegate
                {
                    return !transition.isClosed;
                });
            
                GameObject.Destroy(transition.gameObject);
            }
            
            nekoninEntity.input.enabled = true;
            
            hud.hud.visible = true;
            runData.totalRooms = UnityEngine.Random.Range(minRooms, maxRooms) + extraRooms;
            gameEntity.game.state = GameComponent.State.Fighting;

            RegenerateRoomExits();
            RestartMusic(GameComponent.State.Fighting);
        }


        private IEnumerator StartTransitionToNextRoom(RoomExitComponent roomExit)
        {
            hud.hud.visible = false;
            
            gameEntity.game.state = GameComponent.State.TransitionToRoom;
            
            nekoninEntity.GetComponentInChildren<UnitInput>().enabled = false;

            yield return null;

            var transitionObject = GameObject.Instantiate(transitionPrefab);
            // var transitionPosition = cameraFollow.cameraTransform.position;
            // transitionPosition.z = 0;
            transitionObject.transform.position = roomExit.transform.position;

            var nextRoomRewardType = roomExit.rewardType;

            var transition = transitionObject.GetComponent<Transition>();
            transition.Open();

            yield return new WaitWhile(delegate
            {
                return !transition.isOpen;
            });

            yield return new WaitForSeconds(delayBetweenRooms);

            var nextRoomPrefab = rooms.GetNextRoom(runData);

            GameObject.Destroy(currentRoom.gameObject);

            var roomObject = GameObject.Instantiate(nextRoomPrefab);
            currentRoom = roomObject.GetComponent<RoomComponent>();

            if (currentRoom.isSecretRoom)
            {
                runData.secretRooms++;
                runData.generatedRooms.Add(nextRoomPrefab);
            }

            currentRoom.minEnemies = minEnemiesPerRoom + extraEnemies;
            currentRoom.maxEnemies = maxEnemiesPerRoom + extraEnemies;
            
            nekoninEntity.transform.position = currentRoom.roomStart.transform.position;
            
            runData.totalRooms--;

            currentRoom.rewardType = nextRoomRewardType;
            
            RestartMusic(GameComponent.State.Fighting);

            transitionObject.transform.position = currentRoom.roomStart.transform.position;
            transition.Close();
            
            yield return new WaitWhile(delegate
            {
                return !transition.isClosed;
            });
            
            gameEntity.game.state = GameComponent.State.Fighting;
            
            GameObject.Destroy(transition.gameObject);

            RegenerateRoomExits();
            
            nekoninEntity.GetComponentInChildren<UnitInput>().enabled = true;
            
            hud.hud.visible = true;
        }

        private void RegenerateRoomExits()
        {
            // foreach (var roomExitUnit in roomExitUnits)
            // {
            //     // now is autodestroyed 
            //     if (roomExitUnit != null)
            //     {
            //         GameObject.Destroy(roomExitUnit.gameObject);
            //     }
            // }
            
            roomExitUnits.Clear();

            var roomExits = new List<RoomExitSpawn>(currentRoom.roomExits);

            var newRoomRewardTypes = rooms.rewardTypes.OrderBy(s => Random.value).ToList();

            for (var i = 0; i < roomExits.Count; i++)
            {
                var roomExit = roomExits[i];
                var roomExitObject = GameObject.Instantiate(roomExitUnitPrefab, roomExit.transform.position, 
                    Quaternion.identity, currentRoom.transform);
                // roomExitObject.transform.position = roomExit.transform.position;
                var roomExitUnit = roomExitObject.GetComponentInChildren<Entity>();
                roomExitUnits.Add(roomExitUnit);

                // if no more rooms, avoid generating next room reward
                if (newRoomRewardTypes.Count > 0)
                {
                    if (i >= newRoomRewardTypes.Count)
                    {
                        i = 0;
                    }
                    var rewardType = newRoomRewardTypes[i];
                    roomExitUnit.roomExit.rewardType = rewardType.name;
                }

                if (runData.totalRooms <= 0)
                {
                    roomExitUnit.roomExit.rewardType = "unknown";
                }

                GameObject.Destroy(roomExit.gameObject);
            }
        }
        
        private IEnumerator VictorySequence()
        {
            gameEntity.game.state = GameComponent.State.TransitionToRoom;
            nekoninEntity.input.enabled = false;
            hud.hud.visible = false;

            yield return new WaitForSeconds(2.0f);

            extraRooms += roomIncrementPerVictory;
            currentRun++;
            extraEnemies += enemiesIncrementPerRun;

            victorySequence.transform.position = nekoninEntity.transform.position;
            victorySequence.Restart();

            yield return new WaitUntil(delegate
            {
                return victorySequence.completed;
            });
            
            cameraFollow.followTransform = null;
            var cameraPosition = cameraFollow.transform.position;
            cameraFollow.transform.position = new Vector3(1000, 1000, cameraPosition.z);
            
            victorySequence.Complete();

            var coroutine = StartCoroutine(RestartGame(false, true));
            
            yield return coroutine;
            
            // victorySequence.transform.position = nekoninEntity.transform.position;

            // hide victory sequence...
        }

          private IEnumerator DefeatSequence()
        {
            gameEntity.game.state = GameComponent.State.TransitionToRoom;
            nekoninEntity.input.enabled = false;
            
            // TODO: show custom defeat screen, wait a bit, then go to restart game.
            
            yield return new WaitForSeconds(2.0f);

            extraRooms = 0;
            extraEnemies = 0;
            currentRun++;

            StartCoroutine(RestartGame(false, false));
            
            // yield return ;
        }
          
        public void Update()
        {
            if (gameEntity.game.state == GameComponent.State.TransitionToRoom)
            {
                return;
            }

            if (gameEntity.game.state == GameComponent.State.Restarting)
            {
                return;
            }

            if (gameEntity.game.state == GameComponent.State.Victory)
            {
                // TODO: start another sequence first (transition, stuff), then restart
                StartCoroutine(VictorySequence());
                return;
            }
            
            if (gameEntity.game.state == GameComponent.State.Defeat)
            {
                // TODO: start another sequence first (transition, stuff), then restart
                // StartCoroutine(RestartGame(false));
                StartCoroutine(DefeatSequence());
                return;
            }

            if (gameEntity.game.state == GameComponent.State.Fighting)
            {
                if (!nekoninEntity.health.alive)
                {
                    gameEntity.game.state = GameComponent.State.Defeat;
                    if (defeatSfx != null)
                    {
                        defeatSfx.Play();
                    }
                    // StartCoroutine(DefeatSequence());
                    return;
                }
            }

                // check if one room exit is pressed
            var roomExitList = world.GetComponentList<RoomExitComponent>();
            foreach (var roomExit in roomExitList)
            {
                if (roomExit.playerInExit)
                {
                    // TODO: more room data and logic..
                    StartCoroutine(StartTransitionToNextRoom(roomExit));
                }
            }
            
        }
        
    }
}