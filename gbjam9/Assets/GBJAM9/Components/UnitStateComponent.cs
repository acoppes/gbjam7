using System;
using UnityEngine;

namespace GBJAM9.Components
{
    public class UnitStateComponent : MonoBehaviour, IGameComponent
    {
        [NonSerialized]
        public bool walking;

        [NonSerialized]
        public bool kunaiAttacking;
        
        [NonSerialized]
        public bool swordAttacking;

        [NonSerialized]
        public bool dashing;
        
        [NonSerialized]
        public bool hit;
        
        public static readonly int walkingStateHash = Animator.StringToHash("walking");
        public static readonly int dashingStateHash = Animator.StringToHash("dashing");
        public static readonly int kunaiAttackStateHash = Animator.StringToHash("kunai_attack");
        public static readonly int swordAttackStateHash = Animator.StringToHash("sword_attack");
        public static readonly int hitStateHash = Animator.StringToHash("hit");
        
        public static readonly int hittedStateHash = Animator.StringToHash("hitted");
    }
}