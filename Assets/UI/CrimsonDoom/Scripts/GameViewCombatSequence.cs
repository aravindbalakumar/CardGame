namespace CrimsonDoom.UI
{
    using System;
    using System.Collections;
    using UnityEngine;

    public sealed partial class GameViewUIToolkit
    {
        [Header("Card combat animation")]
        [SerializeField] private SpriteRenderer holySpellVoss;
        [SerializeField] private SpriteRenderer deathWrag;
        [Tooltip("HolySpellAnim: charge at frame 6, release at frame 20 of 36.")]
        [SerializeField, Range(0f, 1f)] private float chargeNormalizedTime = 6f / 36f;
        [SerializeField, Range(0f, 1f)] private float releaseNormalizedTime = 20f / 36f;
        [SerializeField] private Vector2 chargeSpritePoint = new Vector2(0.65f, 0.59f);
        [SerializeField] private Vector2 releaseSpritePoint = new Vector2(0.675f, 0.63f);
        [SerializeField] private Vector2 hitSpritePoint = new Vector2(0.42f, 0.58f);
        [SerializeField, Min(1f)] private float projectileSpeed = 11f;
        [SerializeField] private GameObject castingVfxPrefab;
        [SerializeField] private GameObject projectileVfxPrefab;
        [SerializeField] private GameObject impactVfxPrefab;
        [SerializeField] private GameObject impactBurstVfxPrefab;
        [SerializeField, Min(0.01f)] private float castingVfxScale = 0.06f;
        [SerializeField, Min(0.01f)] private float projectileVfxScale = 0.18f;
        [SerializeField, Min(0.01f)] private float impactVfxScale = 0.12f;
        [SerializeField, Min(0.01f)] private float impactBurstVfxScale = 0.55f;

        public bool CombatSequenceRunning { get; private set; }
        public string CombatPhase { get; private set; } = "Idle";
        public float LastImpactTime { get; private set; }
        public float LastDeathTime { get; private set; }
        private int previewSelectedIndex = -1;
        private Coroutine combatSequence;
        private GameObject spellEffect;
        private GameObject castingEffect;
        private GameObject impactEffect;
        private GameObject impactBurstEffect;
        private bool combatAnimationInterrupted;
        private bool castReleased, heroRecovered;
        private enum CombatVfxRole { Casting, Projectile, ImpactAccent, ImpactBurst }

        private bool BeginCombatSequence(Action applyCard)
        {
            if (CombatSequenceRunning || !Application.isPlaying) return false;
            if (!CanAnimate(holySpellVoss) || !CanAnimate(deathWrag) ||
                heroRenderer == null || enemyRenderer == null || !enemyRenderer.gameObject.activeInHierarchy ||
                !HasParticles(castingVfxPrefab) || !HasParticles(projectileVfxPrefab) ||
                !HasParticles(impactVfxPrefab) || !HasParticles(impactBurstVfxPrefab))
            {
                Debug.LogWarning("Card combat requires the idle/spell/death characters and casting, projectile and impact particle prefabs.", this);
                return false;
            }
            RestoreAnimationVisibility();
            CombatSequenceRunning = true;
            castReleased = heroRecovered = false;
            RefreshAvailability();
            combatSequence = StartCoroutine(RunCombatSequence(applyCard));
            return true;
        }

        private static bool CanAnimate(SpriteRenderer pose)
        {
            var animator = pose != null ? pose.GetComponent<Animator>() : null;
            return pose != null && pose.enabled && animator != null && animator.enabled &&
                animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0;
        }

        private IEnumerator RunCombatSequence(Action applyCard)
        {
            try
            {
                CombatPhase = "Casting";
                ShowCombatPose(heroRenderer, holySpellVoss, true);
                float release = Mathf.Clamp(releaseNormalizedTime, 0.01f, 1f);
                yield return WaitForCombatAnimation(holySpellVoss, Mathf.Min(chargeNormalizedTime, release));
                if (combatAnimationInterrupted || !CombatReferencesAvailable()) yield break;
                castingEffect = SpawnCombatVfx(castingVfxPrefab, CastPoint(),
                    Quaternion.identity, castingVfxScale, CombatVfxRole.Casting);
                yield return WaitForCombatAnimation(holySpellVoss, release);
                if (combatAnimationInterrupted || !CombatReferencesAvailable()) yield break;
                castReleased = true;
                StopCombatVfx(castingEffect, false);

                Vector3 start = SpritePoint(holySpellVoss, releaseSpritePoint);
                spellEffect = SpawnCombatVfx(projectileVfxPrefab, start,
                    ProjectileRotation(start, SpritePoint(enemyRenderer, hitSpritePoint)), projectileVfxScale, CombatVfxRole.Projectile);
                CombatPhase = "Projectile";
                // Present the seeded core at the hand on the release frame, without advancing the effect clock.
                yield return null;
                Vector3 hit = start;
                while (CombatReferencesAvailable() && spellEffect != null)
                {
                    Vector3 target = SpritePoint(enemyRenderer, hitSpritePoint);
                    Vector3 position = spellEffect.transform.position;
                    if ((target - position).sqrMagnitude > 0.000001f)
                        spellEffect.transform.rotation = ProjectileRotation(position, target);
                    hit = Vector3.MoveTowards(position, target, Mathf.Max(1f, projectileSpeed) * Time.deltaTime);
                    spellEffect.transform.position = hit;
                    // Impact happens on the arrival frame, with no extra yield or repositioning jump.
                    if ((hit - target).sqrMagnitude < 0.000001f) break;
                    yield return null;
                }
                if (!CombatReferencesAvailable() || spellEffect == null) yield break;
                // Keep the authored world-space sparks alive briefly, but remove the projectile core on hit.
                foreach (var particles in spellEffect.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Stop(false, particles.main.simulationSpace == ParticleSystemSimulationSpace.World
                        ? ParticleSystemStopBehavior.StopEmitting : ParticleSystemStopBehavior.StopEmittingAndClear);
                impactEffect = SpawnCombatVfx(impactVfxPrefab, hit,
                    Quaternion.identity, impactVfxScale, CombatVfxRole.ImpactAccent);
                impactBurstEffect = SpawnCombatVfx(impactBurstVfxPrefab, hit,
                    Quaternion.identity, impactBurstVfxScale, CombatVfxRole.ImpactBurst);
                LastImpactTime = Time.realtimeSinceStartup;
                CombatPhase = "Impact";
                // The existing handler owns effects and turn completion; invoke it exactly once on hit.
                applyCard?.Invoke();
                if (!CombatReferencesAvailable()) yield break;

                // The imported impact begins on hit; the death deadline is independent of its particle lifetime.
                while (Time.realtimeSinceStartup - LastImpactTime < 1f)
                {
                    if (!CombatReferencesAvailable()) yield break;
                    yield return null;
                }
                if (!CombatReferencesAvailable()) yield break;
                LastDeathTime = Time.realtimeSinceStartup;
                CombatPhase = "Death";
                ShowCombatPose(enemyRenderer, deathWrag, false);
                yield return WaitForCombatAnimation(deathWrag);
            }
            finally
            {
                combatSequence = null;
                FinishCombatSequence();
            }
        }

        private bool CombatReferencesAvailable() => heroRenderer != null && enemyRenderer != null &&
            holySpellVoss != null && deathWrag != null && castingVfxPrefab != null &&
            projectileVfxPrefab != null && impactVfxPrefab != null && impactBurstVfxPrefab != null &&
            CanAnimate(deathWrag) &&
            (heroRecovered || (holySpellVoss.gameObject.activeInHierarchy && CanAnimate(holySpellVoss)));

        private void ShowCombatPose(SpriteRenderer idle, SpriteRenderer pose, bool hero)
        {
            RememberVisibility(idle);
            RememberVisibility(pose);
            idle.gameObject.SetActive(false);
            pose.gameObject.SetActive(true);
            var animator = pose.GetComponent<Animator>();
            animator.Play(0, 0, 0f);
            animator.Update(0f);
            if (hero) activeHeroRenderer = pose;
            else activeEnemyRenderer = pose;
        }

        private IEnumerator WaitForCombatAnimation(SpriteRenderer pose, float normalizedTime = 1f)
        {
            combatAnimationInterrupted = false;
            var animator = pose.GetComponent<Animator>();
            int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            float previous = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float stalledSince = Time.realtimeSinceStartup;
            while (true)
            {
                if (pose == null || animator == null || !animator.isActiveAndEnabled)
                {
                    combatAnimationInterrupted = true;
                    yield break;
                }
                var current = animator.GetCurrentAnimatorStateInfo(0);
                if (current.fullPathHash != state) { combatAnimationInterrupted = true; yield break; }
                if (current.normalizedTime >= normalizedTime) yield break;
                if (current.normalizedTime > previous || Time.timeScale == 0f || animator.speed == 0f)
                    stalledSince = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - stalledSince > 3f) { combatAnimationInterrupted = true; yield break; }
                previous = current.normalizedTime;
                yield return null;
            }
        }

        private static Vector3 SpritePoint(SpriteRenderer renderer, Vector2 point)
        {
            var sprite = renderer.sprite;
            if (sprite == null) return renderer.transform.position;
            Vector2 local = (Vector2.Scale(sprite.rect.size, point) - sprite.pivot) / sprite.pixelsPerUnit;
            if (renderer.flipX) local.x = -local.x;
            if (renderer.flipY) local.y = -local.y;
            Vector3 position = renderer.transform.TransformPoint(local);
            // Contact is on the visible surface, ahead of the sprite's depth-writing plane.
            // The orthographic scene keeps the exact same on-screen hand/shoulder coordinates.
            var camera = Camera.main;
            return camera != null ? position - camera.transform.forward * 0.2f : position;
        }

        private Vector3 CastPoint()
        {
            float time = holySpellVoss.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;
            return SpritePoint(holySpellVoss, Vector2.Lerp(chargeSpritePoint, releaseSpritePoint,
                Mathf.InverseLerp(chargeNormalizedTime, releaseNormalizedTime, time)));
        }

        private void UpdateCombatRecovery()
        {
            if (!CombatSequenceRunning || holySpellVoss == null || heroRenderer == null) return;
            if (!castReleased)
            {
                if (castingEffect != null) castingEffect.transform.position = CastPoint();
                return;
            }
            if (heroRecovered || !holySpellVoss.gameObject.activeInHierarchy) return;
            var animator = holySpellVoss.GetComponent<Animator>();
            if (animator == null || !animator.isActiveAndEnabled) { CancelCombatSequence(); return; }
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f) return;
            ShowCombatPose(holySpellVoss, heroRenderer, true);
            heroRecovered = true;
        }

        private static bool HasParticles(GameObject prefab) => prefab != null &&
            prefab.GetComponentInChildren<ParticleSystem>(true) != null;

        private static Quaternion ProjectileRotation(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity;
        }

        private GameObject SpawnCombatVfx(GameObject prefab, Vector3 position, Quaternion rotation, float scale, CombatVfxRole role)
        {
            if (!HasParticles(prefab)) return null;
            var effect = Instantiate(prefab, position, rotation);
            effect.transform.localScale *= Mathf.Max(0.01f, scale);
            foreach (var renderer in effect.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingLayerID = enemyRenderer.sortingLayerID;
                renderer.sortingOrder = Mathf.Max(heroRenderer.sortingOrder, enemyRenderer.sortingOrder) + 20;
                var material = renderer.sharedMaterial;
                if (material != null && material.HasProperty("_TintColor"))
                {
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_TintColor", SpellColor(material.GetColor("_TintColor"), false));
                    renderer.SetPropertyBlock(block);
                }
            }
            var systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particles in systems) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var particles in systems)
            {
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.useUnscaledTime = false;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.stopAction = ParticleSystemStopAction.None;
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                // The zap's overhead bolt/scorch are unsuitable for a body hit. Reuse its short contact flash/arcs.
                bool visible = renderer != null && renderer.sharedMaterial != null &&
                    (role != CombatVfxRole.ImpactAccent || particles.name == "Flare" || particles.name == "Zap Add Floor") &&
                    (role != CombatVfxRole.Casting || (particles.transform != effect.transform && particles.name != "Pos"));
                if (!visible)
                {
                    var emission = particles.emission;
                    emission.enabled = false;
                    if (renderer != null) renderer.enabled = false;
                    continue;
                }
                main.startColor = SpellGradient(main.startColor, true);
                var color = particles.colorOverLifetime;
                if (color.enabled) color.color = SpellGradient(color.color, false);
                if (role != CombatVfxRole.Projectile) main.loop = false;
                if (role == CombatVfxRole.ImpactAccent) renderer.alignment = ParticleSystemRenderSpace.View;
                if (role == CombatVfxRole.ImpactBurst) main.startLifetimeMultiplier *= 2f;
                if (role == CombatVfxRole.Projectile)
                {
                    if (main.simulationSpace == ParticleSystemSimulationSpace.World)
                    {
                        main.startLifetime = 0.28f;
                        main.startSpeedMultiplier *= 0.4f;
                    }
                    else if (particles.emission.rateOverTime.constantMax > 0f)
                    {
                        main.startLifetime = 0.22f;
                        var emission = particles.emission;
                        emission.rateOverTimeMultiplier = Mathf.Min(emission.rateOverTimeMultiplier, 30f);
                    }
                }
                particles.Play(false);
                if ((role == CombatVfxRole.Projectile || role == CombatVfxRole.ImpactBurst) &&
                    particles.emission.rateOverTime.constantMax > 0f && particles.emission.burstCount == 0)
                    particles.Emit(1);
            }
            return effect;
        }

        private static Color SpellColor(Color color, bool gold)
        {
            float brightness = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            return new Color(brightness, brightness * (gold ? 0.82f : 1f), brightness * (gold ? 0.38f : 1f), color.a);
        }

        private static Gradient SpellGradient(Gradient gradient, bool gold)
        {
            var keys = gradient.colorKeys;
            for (int i = 0; i < keys.Length; i++) keys[i].color = SpellColor(keys[i].color, gold);
            var result = new Gradient { mode = gradient.mode };
            result.SetKeys(keys, gradient.alphaKeys);
            return result;
        }

        private static ParticleSystem.MinMaxGradient SpellGradient(ParticleSystem.MinMaxGradient color, bool gold)
        {
            switch (color.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(SpellColor(color.color, gold));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(SpellColor(color.colorMin, gold), SpellColor(color.colorMax, gold));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(SpellGradient(color.gradientMin, gold), SpellGradient(color.gradientMax, gold));
                default:
                    var result = new ParticleSystem.MinMaxGradient(SpellGradient(color.gradient, gold));
                    result.mode = color.mode;
                    return result;
            }
        }

        private static void StopCombatVfx(GameObject effect, bool clear)
        {
            if (effect == null) return;
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(false, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }

        private void CancelCombatSequence()
        {
            if (combatSequence != null) StopCoroutine(combatSequence);
            combatSequence = null;
            FinishCombatSequence();
        }

        private void FinishCombatSequence()
        {
            if (spellEffect != null) Destroy(spellEffect);
            if (castingEffect != null) Destroy(castingEffect);
            if (impactEffect != null) Destroy(impactEffect);
            if (impactBurstEffect != null) Destroy(impactBurstEffect);
            spellEffect = null;
            castingEffect = null;
            impactEffect = null;
            impactBurstEffect = null;
            if (!CombatSequenceRunning) return;
            RestoreAnimationVisibility();
            CombatSequenceRunning = false;
            CombatPhase = "Idle";
            previewSelectedIndex = -1;
            if (gameHandler != null && gameHandler.SelectedCard != null)
                gameHandler.UpdateSelectedCard(gameHandler.SelectedCard);
            selectedCard = null;
            if (initialized && isActiveAndEnabled) RefreshFromGame(true);
        }
    }
}
