using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    [Header("Components")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private ParticleSystem particleSystem;

    [Header("Animation Settings")]
    public float defaultAnimationSpeed = 1f;
    public float attackAnimationSpeed = 1.5f;
    public float hitAnimationDuration = 0.2f;
    public float deathAnimationDuration = 1f;

    [Header("Visual Effects")]
    public Color hitFlashColor = Color.white;
    public float flashDuration = 0.1f;
    public int flashCount = 3;
    
    [Header("Particle Effects")]
    public GameObject summonEffectPrefab;
    public GameObject attackEffectPrefab;
    public GameObject deathEffectPrefab;
    public GameObject healEffectPrefab;
    public GameObject buffEffectPrefab;
    public GameObject debuffEffectPrefab;

    // Animation state hashes for better performance
    private readonly int IdleHash = Animator.StringToHash("Idle");
    private readonly int WalkHash = Animator.StringToHash("Walk");
    private readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly int HitHash = Animator.StringToHash("Hit");
    private readonly int DeathHash = Animator.StringToHash("Death");
    private readonly int SkillHash = Animator.StringToHash("Skill");
    private readonly int VictoryHash = Animator.StringToHash("Victory");

    // Animation state tracking
    private bool isDead = false;
    private bool isAttacking = false;
    private Dictionary<string, ParticleSystem> activeEffects = new Dictionary<string, ParticleSystem>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        particleSystem = GetComponent<ParticleSystem>();

        // Set default animation speed
        animator.speed = defaultAnimationSpeed;
    }

    #region Animation Controls

    public void PlayIdle()
    {
        if (isDead) return;
        animator.SetTrigger(IdleHash);
    }

    public void PlayWalk(Vector2 direction)
    {
        if (isDead || isAttacking) return;

        // Update facing direction
        if (direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        animator.SetBool(WalkHash, direction.magnitude > 0.1f);
    }

    public void PlayAttack()
    {
        if (isDead || isAttacking) return;

        StartCoroutine(PlayAttackSequence());
    }

    private IEnumerator PlayAttackSequence()
    {
        isAttacking = true;
        animator.speed = attackAnimationSpeed;
        animator.SetTrigger(AttackHash);

        // Spawn attack effect
        if (attackEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position + transform.right * 0.5f;
            GameObject effect = Instantiate(attackEffectPrefab, effectPosition, Quaternion.identity);
            Destroy(effect, 1f);
        }

        // Wait for attack animation to complete
        yield return new WaitForSeconds(GetAnimationLength("Attack") / attackAnimationSpeed);

        animator.speed = defaultAnimationSpeed;
        isAttacking = false;
    }

    public void PlayHit()
    {
        if (isDead) return;

        animator.SetTrigger(HitHash);
        StartCoroutine(FlashEffect());
    }

    public void PlayDeath()
    {
        if (isDead) return;

        isDead = true;
        animator.SetTrigger(DeathHash);
        
        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, deathAnimationDuration);
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        
        // Fade out the sprite
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, endColor, elapsed);
            yield return null;
        }
    }

    public void PlaySkill(string skillName)
    {
        if (isDead) return;

        animator.SetTrigger(SkillHash);
        // Additional skill-specific effects can be added here
    }

    public void PlayVictory()
    {
        if (isDead) return;

        animator.SetTrigger(VictoryHash);
    }

    #endregion

    #region Visual Effects

    private IEnumerator FlashEffect()
    {
        Color originalColor = spriteRenderer.color;
        
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(flashDuration * 0.5f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration * 0.5f);
        }
    }

    public void PlaySummonEffect()
    {
        if (summonEffectPrefab != null)
        {
            GameObject effect = Instantiate(summonEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    public void PlayHealEffect()
    {
        PlayParticleEffect("Heal", healEffectPrefab);
    }

    public void PlayBuffEffect()
    {
        PlayParticleEffect("Buff", buffEffectPrefab);
    }

    public void PlayDebuffEffect()
    {
        PlayParticleEffect("Debuff", debuffEffectPrefab);
    }

    private void PlayParticleEffect(string effectName, GameObject effectPrefab)
    {
        if (effectPrefab == null) return;

        // Stop existing effect if it exists
        if (activeEffects.ContainsKey(effectName))
        {
            if (activeEffects[effectName] != null)
            {
                activeEffects[effectName].Stop();
                Destroy(activeEffects[effectName].gameObject);
            }
            activeEffects.Remove(effectName);
        }

        // Create new effect
        GameObject newEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);
        ParticleSystem particleSystem = newEffect.GetComponent<ParticleSystem>();
        
        if (particleSystem != null)
        {
            activeEffects[effectName] = particleSystem;
            particleSystem.Play();
        }
    }

    public void StopEffect(string effectName)
    {
        if (activeEffects.ContainsKey(effectName))
        {
            if (activeEffects[effectName] != null)
            {
                activeEffects[effectName].Stop();
                Destroy(activeEffects[effectName].gameObject);
            }
            activeEffects.Remove(effectName);
        }
    }

    #endregion

    #region Utility Methods

    private float GetAnimationLength(string animationName)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name.Contains(animationName))
            {
                return clip.length;
            }
        }
        return 1f; // Default duration if animation not found
    }

    private void OnDestroy()
    {
        // Clean up any remaining effects
        foreach (var effect in activeEffects.Values)
        {
            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }
        activeEffects.Clear();
    }

    #endregion
}
