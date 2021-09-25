using System;
using UnityEngine;

namespace GBJAM9.Components
{
    public class HealthComponent : MonoBehaviour, IGameComponent
    {
        public int total;
        public int current;

        public bool immortal;
        
        [NonSerialized]
        public int damages;
    }
}