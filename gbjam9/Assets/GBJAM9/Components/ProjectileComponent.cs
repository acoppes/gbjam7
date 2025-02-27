using System;
using UnityEngine;

namespace GBJAM9.Components
{
    public class ProjectileComponent : MonoBehaviour, IEntityComponent
    {
        public int totalTargets = 1;
        
        [NonSerialized]
        public int damage;

        [NonSerialized]
        public bool damagePerformed;
        
        public GameObject hitSfxPrefab;
    }
}