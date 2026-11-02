namespace CrimsonDoom.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed partial class GameViewUIToolkit
    {
        [Header("Card combat animation")]
        [Tooltip("Existing spell pose, or Voss_Player whose Animator contains HolySpellAnim.")]
        [SerializeField] private SpriteRenderer holySpellVoss;
        [SerializeField] private SpriteRenderer deathWrag;
        [Tooltip("Frame 20 of the existing 36-frame, 12 fps HolySpellAnim is the attack flash.")]
        [SerializeField, Range(0f, 1f)] private float releaseNormalizedTime = 20f / 36f;
        [SerializeField] private Vector2 hitSpritePoint = new Vector2(0.42f, 0.58f);
        [SerializeField] private GameObject impactVfxPrefab;
        [SerializeField, Min(0.01f)] private float impactVfxScale = 0.55f;
        [Header("Enemy counterattack")]
        [SerializeField] private SpriteRenderer wragAttack;
        [Tooltip("BitingWragAnim frame 21: the jaws close after the wind-up.")]
        [SerializeField, Range(0f, 1f)] private float counterHitNormalizedTime = 21f / 36f;
        [SerializeField] private Vector2 heroHitSpritePoint = new Vector2(0.5f, 0.58f);

        public const double ImpactToDeathDelay = 1d;
        private static readonly int HolySpellState = Animator.StringToHash("Base Layer.HolySpellAnim");
        private static readonly int HurtVossState = Animator.StringToHash("Base Layer.HurtVossAnim");
        public bool CombatSequenceRunning { get; private set; }
        public string CombatPhase { get; private set; } = "Idle";
        public double LastImpactTime { get; private set; }
        public double LastDeathTime { get; private set; }
        public Vector3 LastImpactPosition { get; private set; }
        private int previewSelectedIndex = -1;
        private Coroutine combatSequence;
        private GameObject impactEffect;
        private bool combatAnimationInterrupted, combatCompleted;
        private bool playerAttackCompleted, counterattacking;
        private Game.BotPlayer counterattacker;
        private Game.Card combatCard;
        private bool usesLiveCard;
        private int heroReturnState;
        private readonly Dictionary<SpriteRenderer, bool> combatVisibility = new Dictionary<SpriteRenderer, bool>();

        private bool BeginCombatSequence(Action applyCard)
        {
            if (CombatSequenceRunning || !Application.isPlaying) return false;
            RestoreAnimationVisibility();
            if (!CanAnimate(holySpellVoss) || !CanAnimate(deathWrag) ||
                !CanAnimate(heroRenderer) || enemyRenderer == null || !enemyRenderer.gameObject.activeInHierarchy ||
                !HasParticles(impactVfxPrefab) ||
                (holySpellVoss == heroRenderer && !holySpellVoss.GetComponent<Animator>().HasState(0, HolySpellState)))
            {
                Debug.LogWarning("Assign the existing Voss spell animation, Wrag death pose, and an impact particle prefab before attacking.", this);
                return false;
            }
            heroReturnState = heroRenderer.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).fullPathHash;
            CombatSequenceRunning = true;
            combatCompleted = false;
            usesLiveCard = !showingPreview;
            combatCard = usesLiveCard ? localPlayer.SelectedCard : null;
            LastImpactTime = LastDeathTime = 0d;
            if (gameHandler != null) gameHandler.CardActionInProgress = true;
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
                yield return WaitForCombatAnimation(holySpellVoss, Mathf.Clamp(releaseNormalizedTime, 0.01f, 1f));
                if (combatAnimationInterrupted || !CombatReferencesAvailable()) yield break;

                // Spawn only on Wrag's current visible body. This effect never travels between actors.
                LastImpactPosition = SpritePoint(enemyRenderer, hitSpritePoint);
                impactEffect = SpawnImpactVfx(LastImpactPosition);
                LastImpactTime = Time.timeAsDouble;
                CombatPhase = "Impact";
                applyCard?.Invoke();

                // Particles and animations share the gameplay clock; pausing freezes the whole sequence.
                // Run on the first frame at/after the exact one-second deadline.
                double deathDeadline = LastImpactTime + ImpactToDeathDelay;
                // A nanosecond tolerance prevents binary rounding from adding a whole extra rendered frame.
                while (Time.timeAsDouble < deathDeadline - 0.000000001d)
                {
                    if (!CombatReferencesAvailable()) yield break;
                    yield return null;
                }
                if (!CombatReferencesAvailable()) yield break;
                LastDeathTime = Time.timeAsDouble;
                CombatPhase = "Death";
                ShowCombatPose(enemyRenderer, deathWrag, false);
                yield return WaitForCombatAnimation(deathWrag);
                combatCompleted = !combatAnimationInterrupted;
            }
            finally
            {
                combatSequence = null;
                FinishCombatSequence();
            }
        }

        private bool CombatReferencesAvailable() => isActiveAndEnabled && heroRenderer != null && enemyRenderer != null &&
            heroRenderer.gameObject.activeInHierarchy && enemyRenderer.gameObject.activeInHierarchy &&
            HasParticles(impactVfxPrefab) && (counterattacking ?
                counterattacker != null && counterattacker.Health > 0 && gameHandler != null &&
                gameHandler.CurrentTurnActor == counterattacker && CanAnimate(wragAttack) && CanAnimate(heroRenderer) :
                CanAnimate(holySpellVoss) && CanAnimate(deathWrag) &&
                (!usesLiveCard || (localPlayer != null && combatCard != null && localPlayer.SelectedCard == combatCard)));

        private bool TryAnimateBotTurn(Game.BotPlayer bot)
        {
            if (bot != opponent || !HasLiveGame || gameHandler == null) return false;
            if (CombatSequenceRunning) return true;
            RestoreAnimationVisibility();
            var skill = bot.bot_Attack_Skill?.Find(s => s != null && s.cardAction.actionType == Game.DataStructures.ActionType.Damage);
            var animator = heroRenderer != null ? heroRenderer.GetComponent<Animator>() : null;
            if (skill == null || !CanAnimate(wragAttack) || !CanAnimate(heroRenderer) ||
                !HasParticles(impactVfxPrefab) || !animator.HasState(0, HurtVossState))
            {
                Debug.LogWarning("Wrag counterattack needs its existing bite pose, Voss hurt state, impact prefab and attack skill. Ending this turn safely.", this);
                bot.EndTurn();
                return true;
            }
            heroReturnState = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            counterattacking = true;
            counterattacker = bot;
            CombatSequenceRunning = true;
            combatCompleted = false;
            usesLiveCard = false;
            LastImpactTime = LastDeathTime = 0d;
            gameHandler.CardActionInProgress = true;
            RefreshAvailability();
            combatSequence = StartCoroutine(RunCounterattack(skill));
            return true;
        }

        private IEnumerator RunCounterattack(Game.ScriptableObjects.Card.Skill skill)
        {
            try
            {
                CombatPhase = "CounterWindup";
                ShowCombatPose(enemyRenderer, wragAttack, false);
                yield return WaitForCombatAnimation(wragAttack, Mathf.Clamp(counterHitNormalizedTime, 0.01f, 1f));
                if (combatAnimationInterrupted || !CombatReferencesAvailable()) yield break;

                // Enter the hurt frame before locating its body, so the hit follows the visible Voss pose.
                ShowCombatPose(heroRenderer, heroRenderer, true, HurtVossState);
                LastImpactPosition = SpritePoint(heroRenderer, heroHitSpritePoint);
                impactEffect = SpawnImpactVfx(LastImpactPosition);
                LastImpactTime = Time.timeAsDouble;
                CombatPhase = "CounterImpact";
                counterattacker.UseSkill(skill, localPlayer);
                yield return WaitForCombatAnimation(wragAttack);
                if (combatAnimationInterrupted || !CombatReferencesAvailable()) yield break;
                yield return WaitForCombatAnimation(heroRenderer, 1f, true);
                combatCompleted = !combatAnimationInterrupted;
            }
            finally
            {
                combatSequence = null;
                FinishCombatSequence();
            }
        }

        private void RememberCombatVisibility(SpriteRenderer renderer)
        {
            RememberVisibility(renderer);
            if (!combatVisibility.ContainsKey(renderer)) combatVisibility.Add(renderer, renderer.enabled);
        }

        private void ShowCombatPose(SpriteRenderer idle, SpriteRenderer pose, bool hero, int state = 0)
        {
            RememberCombatVisibility(idle);
            RememberCombatVisibility(pose);
            // Keep actor, collider and behavior components alive; replace only their visual pose.
            if (idle != pose) idle.enabled = false;
            pose.gameObject.SetActive(true);
            pose.enabled = true;
            var animator = pose.GetComponent<Animator>();
            animator.Play(state != 0 ? state : hero && pose == heroRenderer ? HolySpellState : 0, 0, 0f);
            animator.Update(0f);
            if (hero) activeHeroRenderer = pose;
            else activeEnemyRenderer = pose;
        }

        private IEnumerator WaitForCombatAnimation(SpriteRenderer pose, float normalizedTime = 1f, bool allowStateExit = false)
        {
            combatAnimationInterrupted = false;
            var animator = pose.GetComponent<Animator>();
            int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            float previous = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            double stalledSince = Time.realtimeSinceStartupAsDouble;
            while (true)
            {
                if (pose == null || animator == null || !animator.isActiveAndEnabled || !pose.enabled || !CombatReferencesAvailable())
                {
                    combatAnimationInterrupted = true;
                    yield break;
                }
                var current = animator.GetCurrentAnimatorStateInfo(0);
                if (current.fullPathHash != state)
                {
                    // Voss's existing hurt state has an authored automatic transition back to idle.
                    combatAnimationInterrupted = !allowStateExit;
                    yield break;
                }
                if (current.normalizedTime >= normalizedTime) yield break;
                if (current.normalizedTime > previous || Time.timeScale == 0f || animator.speed == 0f)
                    stalledSince = Time.realtimeSinceStartupAsDouble;
                if (Time.realtimeSinceStartupAsDouble - stalledSince > 3d) { combatAnimationInterrupted = true; yield break; }
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
            // Draw just ahead of Wrag's sprite depth plane, at the same on-screen body position.
            var camera = Camera.main;
            return camera != null ? position - camera.transform.forward * 0.2f : position;
        }

        private static bool HasParticles(GameObject prefab) => prefab != null &&
            prefab.GetComponentInChildren<ParticleSystem>(true) != null;

        private GameObject SpawnImpactVfx(Vector3 position)
        {
            var effect = Instantiate(impactVfxPrefab, position, impactVfxPrefab.transform.rotation);
            effect.transform.localScale *= Mathf.Max(0.01f, impactVfxScale);
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = false;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.stopAction = ParticleSystemStopAction.None;
            }
            foreach (var renderer in effect.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingLayerID = enemyRenderer.sortingLayerID;
                renderer.sortingOrder = Mathf.Max(heroRenderer.sortingOrder, enemyRenderer.sortingOrder) + 20;
            }
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true)) particles.Play(false);
            return effect;
        }

        private void CancelCombatSequence()
        {
            var routine = combatSequence;
            combatSequence = null;
            if (routine != null) StopCoroutine(routine);
            if (CombatSequenceRunning) FinishCombatSequence();
        }

        private void FinishCombatSequence()
        {
            if (!CombatSequenceRunning) return;
            bool wasCounterattack = counterattacking;
            var bot = counterattacker;
            if (!wasCounterattack && combatCompleted && usesLiveCard) playerAttackCompleted = true;
            if (impactEffect != null) Destroy(impactEffect);
            impactEffect = null;
            foreach (var entry in combatVisibility)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            combatVisibility.Clear();
            RestoreAnimationVisibility();
            if (holySpellVoss == heroRenderer && heroRenderer != null)
            {
                var animator = heroRenderer.GetComponent<Animator>();
                if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null &&
                    (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HolySpellState ||
                     animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HurtVossState))
                    animator.Play(heroReturnState, 0, 0f);
            }
            // A cosmetic death animation must not silently kill a bot that survived the actual card effect.
            if (!wasCounterattack && combatCompleted && opponent != null && opponent.Health <= 0 && enemyRenderer != null && deathWrag != null)
            {
                enemyRenderer.enabled = false;
                deathWrag.gameObject.SetActive(true);
                deathWrag.enabled = true;
                var animator = deathWrag.GetComponent<Animator>();
                animator.Play(0, 0, 1f);
                animator.Update(0f);
            }
            CombatSequenceRunning = false;
            CombatPhase = "Idle";
            previewSelectedIndex = -1;
            if (gameHandler != null)
            {
                gameHandler.CardActionInProgress = false;
                if (gameHandler.player != null) gameHandler.player.UpdateSelectedCard(null);
            }
            selectedCard = null;
            combatCard = null;
            usesLiveCard = false;
            counterattacking = false;
            counterattacker = null;
            if (wasCounterattack && bot != null) bot.EndTurn();
            if (initialized && isActiveAndEnabled) RefreshFromGame(true);
        }
    }
}
