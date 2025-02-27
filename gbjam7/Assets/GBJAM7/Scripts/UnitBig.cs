using UnityEngine;

namespace GBJAM7.Scripts
{
    public class UnitBig : MonoBehaviour
    {
        public Animator animator;

        public Transform[] hitPositions;
        public GameObject hitParticlePrefab;

        public bool attackingRanged = false;
        public bool criticalHit = false;
        private int currentHitIndex = 0;

        [SerializeField]
        private AudioSource shootSfx;

        private void Awake()
        {
            currentHitIndex = UnityEngine.Random.Range(0, hitPositions.Length);
        }

        public void StartAttacking()
        {
            animator.SetBool("Attacking", true);
            animator.SetBool("Ranged", attackingRanged);
            animator.SetBool("CriticalHit", criticalHit);
        }

        public void StopAttacking()
        {
            animator.SetBool("Attacking", false);
            attackingRanged = false;
        }

        public void Death()
        {
            animator.SetBool("Dead", true);
        }

        public void ShowHitParticle()
        {
            if (hitPositions.Length == 0 || hitParticlePrefab == null)
                return;
            Instantiate(hitParticlePrefab, hitPositions[currentHitIndex], false);
            currentHitIndex = (currentHitIndex + 1) % hitPositions.Length;
        }

        public void PlayShootSfx()
        {
            if (shootSfx != null)
                shootSfx.Play();
        }
    }
}