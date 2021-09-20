using GBJAM.Commons;
using UnityEngine;

namespace GBJAM9
{
    public class FreeUnitMovement : MonoBehaviour
    {
        [SerializeField]
        protected Transform transform;

        [SerializeField]
        protected GameboyButtonKeyMapAsset gameboyKeyMap;

        [SerializeField]
        protected float speed;

        [SerializeField]
        protected Vector2 perspective = new Vector2(1.0f, 0.75f);
    
        // Update is called once per frame
        private void Update()
        {
            var myPosition = transform.localPosition;
            var velocity = gameboyKeyMap.direction * speed * Time.deltaTime;

            // TODO: vertical movement perspective....
        
            myPosition.x += velocity.x * perspective.x;
            myPosition.y += velocity.y * perspective.y;

            transform.localPosition = myPosition;
        }
    }
}
