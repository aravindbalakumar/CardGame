namespace CrimsonDoom.UI
{
    using System.Collections.Generic;
    using Game;
    using Game.ActorAndPlayers;
    using Game.DataStructures;
    using Game.ScriptableObjects.Card;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>A parallel view of existing gameplay. Preview data never changes the game.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(100)]
    public sealed partial class GameViewUIToolkit : MonoBehaviour
    {
        private enum CharacterSlot
        {
            Hero,
            Enemy
        }

        [System.Serializable]
        private sealed class CharacterAnimationBinding
        {
            public string characterName;
            public string animationName;
            public CharacterSlot characterSlot;
            public SpriteRenderer renderer;
            public Animator animator;
        }

        [SerializeField] private GameHandler gameHandler;
        [SerializeField] private Player2D localPlayer;
        [SerializeField] private AbstractActor opponent;
        [SerializeField] private Transform legacyCardParent;
        [SerializeField] private SpriteRenderer heroRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [Tooltip("Add one entry per animation pose. The assigned scene object's Transform controls its placement.")]
        [SerializeField] private List<CharacterAnimationBinding> characterAnimations = new List<CharacterAnimationBinding>();
        [SerializeField] private VisualTreeAsset cardTemplate;
        [SerializeField] private Sprite[] previewCardSprites;
        [SerializeField, HideInInspector] private Sprite[] folderCardSprites;
        private const int HandCardCount = Defaults.HANDCARDLIMIT;
        private readonly List<Sprite> previewHand = new List<Sprite>();
        private readonly System.Random cardRandom = new System.Random();

#if UNITY_EDITOR
        public static Sprite[] DiscoverCardSprites()
        {
            var sprites = new List<Sprite>();
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Sprites/Cards")) return sprites.ToArray();
            var paths = new List<string>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/Cards" }))
                paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(System.StringComparer.Ordinal);
            foreach (var path in paths)
                foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite) sprites.Add(sprite);
            return sprites.ToArray();
        }

        private void ReloadCardSprites()
        {
            var sprites = DiscoverCardSprites();
            bool changed = folderCardSprites == null || sprites.Length != folderCardSprites.Length;
            for (int i = 0; !changed && i < sprites.Length; i++)
                changed = sprites[i] != folderCardSprites[i];
            if (!changed) return;
            folderCardSprites = sprites;
            previewHand.Clear();
            if (initialized) RefreshHand();
        }
#endif

        private readonly List<CardTypeScriptableObject> displayedHand = new List<CardTypeScriptableObject>();
        private readonly List<Button> cardButtons = new List<Button>();
        private readonly List<Card> legacyCards = new List<Card>();
        private VisualElement root, hand, playerFill, enemyFill, options, playerStatus, intentPreview;
        private Label headerHealth, playerHealth, block, enemyHealth, enemyName, hint;
        private Label handCount, previewLabel, energyValue, currencyCount;
        private Button endTurn, selfTarget, enemyTarget, settingsButton, closeOptions, deckButton, drawPile;
        private Button stopAnimationPreview;
        private VisualElement animationButtons;
        private Label animationStatus;
        private IVisualElementScheduledItem refreshTask;
        private Card selectedCard;
        private bool showingPreview, initialized, turnAvailable = true;
        private SpriteRenderer activeHeroRenderer;
        private SpriteRenderer activeEnemyRenderer;
        private int legacyChildCount;
        private bool animationControlsDirty;
        private readonly Dictionary<GameObject, bool> animationVisibility = new Dictionary<GameObject, bool>();
        private Coroutine animationPreview;
        private Canvas legacyCanvas;
        private UnityEngine.UI.GraphicRaycaster legacyGraphicRaycaster;
        private bool legacyCanvasWasEnabled, legacyRaycasterWasEnabled;

        private bool HasLiveGame => Application.isPlaying && localPlayer != null &&
            localPlayer.isActiveAndEnabled && !string.IsNullOrEmpty(localPlayer.playerID) && localPlayer.currentHand != null;

        private void OnEnable()
        {
            if (initialized) return;
#if UNITY_EDITOR
            ReloadCardSprites();
#endif
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            if (documentRoot == null)
            {
                if (Application.isPlaying && GetComponent<UIDocument>().enabled)
                    StartCoroutine(WaitForDocument());
                return;
            }
            root = documentRoot.Q("game-root");
            if (root == null) return;
            hand = root.Q("hand");
            headerHealth = root.Q<Label>("header-health");
            playerHealth = root.Q<Label>("player-health");
            block = root.Q<Label>("player-block");
            enemyHealth = root.Q<Label>("enemy-health");
            enemyName = root.Q<Label>("enemy-name");
            hint = root.Q<Label>("selection-hint");
            playerFill = root.Q("player-health-fill");
            enemyFill = root.Q("enemy-health-fill");
            playerStatus = root.Q("player-status");
            handCount = root.Q<Label>("hand-count");
            previewLabel = root.Q<Label>("preview-label");
            energyValue = root.Q<Label>("energy-value");
            currencyCount = root.Q<Label>("currency-count");
            endTurn = root.Q<Button>("end-turn");
            selfTarget = root.Q<Button>("self-target");
            enemyTarget = root.Q<Button>("enemy-target");
            options = root.Q("options-panel");
            intentPreview = root.Q("intent-preview");
            settingsButton = root.Q<Button>("settings-button");
            closeOptions = root.Q<Button>("close-options");
            animationButtons = root.Q("animation-buttons");
            animationStatus = root.Q<Label>("animation-status");
            stopAnimationPreview = root.Q<Button>("stop-animation-preview");
            deckButton = root.Q<Button>("deck-button");
            drawPile = root.Q<Button>("draw-pile");
            endTurn.clicked += EndTurn;
            selfTarget.clicked += TargetSelf;
            enemyTarget.clicked += TargetOpponent;
            settingsButton.clicked += ToggleOptions;
            closeOptions.clicked += CloseOptions;
            stopAnimationPreview.clicked += StopAnimationPreview;
            deckButton.clicked += FocusHand;
            drawPile.clicked += FocusHand;
            BuildAnimationControls();
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (Application.isPlaying)
            {
                if (localPlayer != null) localPlayer.OnPlayerUIUpdate += OnPlayerStatus;
                if (opponent != null) opponent.OnPlayerUIUpdate += OnOpponentStatus;
                if (gameHandler != null) GameHandler.OnGameSystemsInitialized += OnSystemsInitialized;
                if (gameHandler != null)
                {
                    gameHandler.CardActionAnimation = TryAnimateCardAction;
                    gameHandler.BotTurnAnimation = TryAnimateBotTurn;
                    gameHandler.TurnChanged += OnTurnChanged;
                }
                // if (gameHandler != null && gameHandler.inputHandler != null)
                //     gameHandler.inputHandler.IsPointerInputBlocked += BlocksWorldPointerInput;
            }
            showingPreview = !HasLiveGame;
            initialized = true;
            if (Application.isPlaying && legacyCardParent != null)
            {
                // Keep the original card objects/data active, with one visible and interactive UI layer.
                legacyCanvas = legacyCardParent.GetComponentInParent<Canvas>();
                if (legacyCanvas != null)
                {
                    legacyCanvasWasEnabled = legacyCanvas.enabled;
                    legacyCanvas.enabled = false;
                    legacyGraphicRaycaster = legacyCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    if (legacyGraphicRaycaster != null)
                    {
                        legacyRaycasterWasEnabled = legacyGraphicRaycaster.enabled;
                        legacyGraphicRaycaster.enabled = false;
                    }
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.projectChanged += ReloadCardSprites;
#endif
            activeHeroRenderer = heroRenderer;
            activeEnemyRenderer = enemyRenderer;
            RefreshFromGame(true);
            // Existing hand lists and selection have no events; compare at 10 Hz, never rebuild per frame.
            refreshTask = root.schedule.Execute(() => RefreshFromGame(false)).Every(100);
        }

        // UIDocument builds its visual tree during OnEnable; Start handles component enable order.
        private void Start()
        {
            if (!initialized) OnEnable();
        }

        private void OnValidate() => animationControlsDirty = true;

        private System.Collections.IEnumerator WaitForDocument()
        {
            yield return null;
            if (isActiveAndEnabled && !initialized) OnEnable();
        }

        private void OnDisable()
        {
            CancelCombatSequence();
            if (legacyCanvas != null) legacyCanvas.enabled = legacyCanvasWasEnabled;
            if (legacyGraphicRaycaster != null) legacyGraphicRaycaster.enabled = legacyRaycasterWasEnabled;
            legacyCanvas = null;
            legacyGraphicRaycaster = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.projectChanged -= ReloadCardSprites;
#endif
            previewHand.Clear();
            RestoreAnimationVisibility();
            refreshTask?.Pause();
            refreshTask = null;
            if (!initialized) return;
            if (localPlayer != null) localPlayer.OnPlayerUIUpdate -= OnPlayerStatus;
            if (opponent != null) opponent.OnPlayerUIUpdate -= OnOpponentStatus;
            if (gameHandler != null) GameHandler.OnGameSystemsInitialized -= OnSystemsInitialized;
            if (gameHandler != null && gameHandler.CardActionAnimation == TryAnimateCardAction)
                gameHandler.CardActionAnimation = null;
            if (gameHandler != null && gameHandler.BotTurnAnimation == TryAnimateBotTurn)
                gameHandler.BotTurnAnimation = null;
            if (gameHandler != null) gameHandler.TurnChanged -= OnTurnChanged;
            // if (gameHandler != null && gameHandler.inputHandler != null)
            //     gameHandler.inputHandler.IsPointerInputBlocked -= BlocksWorldPointerInput;
            endTurn.clicked -= EndTurn;
            selfTarget.clicked -= TargetSelf;
            enemyTarget.clicked -= TargetOpponent;
            settingsButton.clicked -= ToggleOptions;
            closeOptions.clicked -= CloseOptions;
            stopAnimationPreview.clicked -= StopAnimationPreview;
            deckButton.clicked -= FocusHand;
            drawPile.clicked -= FocusHand;
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            displayedHand.Clear();
            legacyCards.Clear();
            cardButtons.Clear();
            initialized = false;
        }

        // Read-only projection for targeting. Scene objects and their Animators own rendering.
        private void LateUpdate()
        {
            if (initialized)
            {
                // UpdateCombatRecovery();
                // RefreshActorPlacement();
            }
        }

        private void OnSystemsInitialized()
        {
            previewHand.Clear();
            root.schedule.Execute(() => RefreshFromGame(true));
        }

        // The full-screen document owns targeting while visible; avoid a second world-raycast action.
        private bool BlocksWorldPointerInput() => initialized && root.panel != null;

        /// <summary>Call after external hand mutations for immediate refresh.</summary>
        public void RefreshHand() => RefreshFromGame(true);

        /// <summary>Optional future turn-controller hook; no turn state is simulated here.</summary>
        public void SetTurnAvailable(bool available)
        {
            turnAvailable = available;
            if (initialized) RefreshAvailability();
        }

        private void OnTurnChanged(AbstractActor actor)
        {
            playerAttackCompleted = false;
            selectedCard = null;
            if (initialized) RefreshFromGame(true);
        }

        private void RefreshFromGame(bool force)
        {
            if (!initialized) return;
            if (animationControlsDirty)
            {
                animationControlsDirty = false;
                BuildAnimationControls();
                force = true;
            }
            bool preview = !HasLiveGame;
            bool modeChanged = showingPreview != preview;
            showingPreview = preview;
            if (force || modeChanged || HandChanged())
            {
                RebuildHand();
                previewLabel.EnableInClassList("hidden", !preview);
                intentPreview.EnableInClassList("hidden", !preview);
                SetText(energyValue, preview ? "3/3" : "—");
                SetText(currencyCount, preview ? "15" : "—");
                if (!preview)
                {
                    OnPlayerStatus(true, localPlayer.Health, localPlayer.Armor);
                    if (opponent != null) OnOpponentStatus(true, opponent.Health, opponent.Armor);
                }
                else
                {
                    SetText(headerHealth, "50/50");
                    SetText(playerHealth, "50/50");
                    SetText(block, "10 Block");
                    SetText(enemyHealth, "25/25");
                    SetText(enemyName, "Rider & Warg");
                    playerFill.style.width = Length.Percent(100);
                    enemyFill.style.width = Length.Percent(100);
                }
            }
            if (Application.isPlaying && gameHandler != null && localPlayer != null && selectedCard != localPlayer.SelectedCard)
            {
                selectedCard = localPlayer.SelectedCard;
                RefreshAvailability();
            }
            if (force || modeChanged) RefreshAvailability();
            RefreshActorPlacement();
        }

        private bool HandChanged()
        {
            if (showingPreview) return false;
            if (legacyCardParent != null && legacyCardParent.childCount != legacyChildCount) return true;
            var current = localPlayer.currentHand;
            if (displayedHand.Count != HandCardCount) return true;
            for (int i = 0; i < HandCardCount; i++)
                if (displayedHand[i] != (i < current.Count ? current[i] : null)) return true;
            return false;
        }

        private void EnsureHandSprites()
        {
            if (previewHand.Count == HandCardCount) return;
            previewHand.Clear();
            var pool = new List<Sprite>();
            if (folderCardSprites != null)
                foreach (var sprite in folderCardSprites)
                    if (sprite != null && !pool.Contains(sprite)) pool.Add(sprite);
            var remaining = new List<Sprite>();
            while (previewHand.Count < HandCardCount)
            {
                if (pool.Count == 0) { previewHand.Add(null); continue; }
                // Draw without replacement. Only reuse sprites after exhausting a smaller-than-five pool.
                if (remaining.Count == 0) remaining.AddRange(pool);
                int index = cardRandom.Next(remaining.Count);
                previewHand.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
        }

        private void RebuildHand()
        {
            hand.Clear();
            cardButtons.Clear();
            displayedHand.Clear();
            legacyCards.Clear();
            legacyChildCount = legacyCardParent != null ? legacyCardParent.childCount : 0;
            if (cardTemplate == null) return;
            if (!showingPreview && legacyCardParent != null)
                legacyCardParent.GetComponentsInChildren(false, legacyCards);
            EnsureHandSprites();
            const int count = HandCardCount;
            for (int i = 0; i < count; i++)
            {
                var tree = cardTemplate.CloneTree();
                var button = tree.Q<Button>("card");
                button.RemoveFromHierarchy();
                hand.Add(button);
                cardButtons.Add(button);
                button.EnableInClassList("card-first", i == 0);
                button.EnableInClassList("card-last", i == count - 1);
                button.RegisterCallback<PointerEnterEvent>(evt => button.AddToClassList("card-hovered"));
                button.RegisterCallback<PointerLeaveEvent>(evt => button.RemoveFromClassList("card-hovered"));
                // Folder sprites contain the complete card face; do not layer the old art/text over them.
                button.Q<Image>("card-frame").sprite = previewHand[i];
                button.AddToClassList("folder-card");
                button.EnableInClassList("card-image-missing", previewHand[i] == null);
                button.Q<Label>("card-title").text = "Card image unavailable";
                if (showingPreview)
                {
                    button.tooltip = previewHand[i] != null ? "Select, then click Wrag to preview the spell sequence." :
                        "Add imported card sprites to Assets/Sprites/Cards/.";
                    // Preview combat animates the scene without changing gameplay stats.
                    int index = i;
                    button.clicked += () => InspectPreview(index);
                }
                else
                {
                    var data = i < localPlayer.currentHand.Count ? localPlayer.currentHand[i] : null;
                    displayedHand.Add(data);
                    if (data == null) { button.SetEnabled(false); continue; }
                    button.Q<Label>("card-title").text = data.metaData.name;
                    button.tooltip = data.metaData.name + " — select, then choose a highlighted target.";
                    int index = i;
                    button.clicked += () => SelectCard(index);
                }
            }
            SetText(handCount, count.ToString());
            SetText(root.Q<Label>("discard-count"), showingPreview ? "0" : "—");
        }

        private void InspectPreview(int index)
        {
            if (CombatSequenceRunning) return;
            previewSelectedIndex = index;
            for (int i = 0; i < cardButtons.Count; i++) cardButtons[i].EnableInClassList("selected", i == index);
            RefreshAvailability();
        }

        private Card FindLegacyCard(int index)
        {
            if (index >= displayedHand.Count) return null;
            // A hand can contain repeated assets; map each occurrence to its own Canvas card.
            int occurrence = 0;
            for (int i = 0; i < index; i++) if (displayedHand[i] == displayedHand[index]) occurrence++;
            foreach (var card in legacyCards)
                if (card != null && card.cardData == displayedHand[index] && occurrence-- == 0) return card;
            return null;
        }

        private void SelectCard(int index)
        {
            if (CombatSequenceRunning || showingPreview || !turnAvailable || gameHandler == null ||
                !gameHandler.IsPlayerTurn || gameHandler.CardActionInProgress) return;
            var legacyCard = FindLegacyCard(index);
            if (legacyCard == null) return;
            // Uses the original selection/targeting path, including its existing highlights.
            legacyCard.OnCardSelected();
            selectedCard = localPlayer.SelectedCard;
            RefreshAvailability();
        }

        private void RefreshAvailability()
        {
            bool ready = !CombatSequenceRunning && !showingPreview && turnAvailable && gameHandler != null &&
                gameHandler.IsPlayerTurn && !gameHandler.CardActionInProgress && localPlayer.Health > 0 &&
                (opponent == null || opponent.Health > 0);
            endTurn.SetEnabled(ready && playerAttackCompleted);
            playerStatus.EnableInClassList("active-player", showingPreview || ready);
            playerStatus.EnableInClassList("inactive-player", !showingPreview && !ready);
            bool selected = ready && selectedCard != null && selectedCard.cardData != null;
            selfTarget.SetEnabled(selected && selectedCard.cardTargetGroup.Contains(localPlayer.gameObject.layer));
            enemyTarget.SetEnabled(selected && opponent != null && opponent.isActiveAndEnabled && opponent.Health > 0);
            if (showingPreview)
                enemyTarget.SetEnabled(Application.isPlaying && !CombatSequenceRunning && previewSelectedIndex >= 0);
            for (int i = 0; i < cardButtons.Count; i++)
            {
                if (showingPreview)
                {
                    cardButtons[i].SetEnabled(!CombatSequenceRunning && previewHand[i] != null);
                    cardButtons[i].EnableInClassList("selected", i == previewSelectedIndex);
                    continue;
                }
                var card = FindLegacyCard(i);
                cardButtons[i].SetEnabled(ready && card != null);
                cardButtons[i].EnableInClassList("selected", card != null && card == selectedCard);
            }
            if (!showingPreview) SetText(hint, selected ? "Choose a target" : "");
            else SetText(hint, previewSelectedIndex >= 0 ? "Click Wrag to cast" : "");
            if (CombatSequenceRunning) SetText(hint, counterattacking ? "Wrag attacks…" : "Casting…");
        }

        private void TargetSelf()
        {
            if (!CombatSequenceRunning) Target(localPlayer);
        }
        private void TargetOpponent()
        {
            if (!Application.isPlaying || CombatSequenceRunning) return;
            if (showingPreview)
            {
                if (previewSelectedIndex >= 0) BeginCombatSequence(null);
                return;
            }
            if (!HasLiveGame || !turnAvailable || gameHandler == null || !gameHandler.IsPlayerTurn ||
                opponent == null || !opponent.isActiveAndEnabled || opponent.Health <= 0) return;
            var card = localPlayer.SelectedCard;
            if (card != null) GameHandler.OnAbstractActorClickedWithCard?.Invoke(opponent, card.cardData);
        }

        private bool TryAnimateCardAction(AbstractActor target, CardTypeScriptableObject data)
        {
            if (target != opponent || !HasLiveGame) return false;
            if (CombatSequenceRunning) return true;
            var card = localPlayer.SelectedCard;
            if (!turnAvailable || !gameHandler.IsPlayerTurn || opponent.Health <= 0 || card == null || card.cardData != data || data == null || data.skill == null) return true;
            selectedCard = card;
            // Clicking Wrag confirms any card, but its original targeting rules still own the gameplay effect.
            AbstractActor effectTarget = data.skill.targetGroup.Contains(opponent.gameObject.layer) ? opponent :
                data.skill.targetGroup.Contains(localPlayer.gameObject.layer) ? localPlayer : null;
            if (effectTarget == null) return true;
            BeginCombatSequence(() =>
            {
                if (gameHandler != null && effectTarget != null && localPlayer != null && localPlayer.SelectedCard == card)
                    gameHandler.ProcessCardAction(effectTarget, data);
            });
            return true;
        }

        private void Target(AbstractActor target)
        {
            if (CombatSequenceRunning || !HasLiveGame || !turnAvailable || gameHandler == null || !gameHandler.IsPlayerTurn || target == null ||
                selectedCard == null || !selectedCard.cardTargetGroup.Contains(target.gameObject.layer)) return;
            GameHandler.OnAbstractActorClickedWithCard?.Invoke(target, selectedCard.cardData);
        }

        private void EndTurn()
        {
            if (CombatSequenceRunning || !HasLiveGame || !turnAvailable || !playerAttackCompleted ||
                gameHandler == null || !gameHandler.IsPlayerTurn || opponent == null || opponent.Health <= 0) return;
            playerAttackCompleted = false;
            selectedCard = null;
            gameHandler.TryEndTurn(localPlayer);
            RefreshAvailability();
        }

        private void OnPlayerStatus(bool alive, int health, int armor)
        {
            if (showingPreview || !initialized) return;
            string value = health + "/" + localPlayer.MaxHealth;
            SetText(headerHealth, value);
            SetText(playerHealth, value);
            SetText(block, armor + " Block");
            playerFill.style.width = Length.Percent(Mathf.Clamp01((float)health / Mathf.Max(1, localPlayer.MaxHealth)) * 100);
            RefreshAvailability();
        }

        private void OnOpponentStatus(bool alive, int health, int armor)
        {
            if (showingPreview || !initialized || opponent == null) return;
            SetText(enemyName, string.IsNullOrEmpty(opponent.Name) ? opponent.name : opponent.Name);
            SetText(enemyHealth, health + "/" + opponent.MaxHealth);
            enemyFill.style.width = Length.Percent(Mathf.Clamp01((float)health / Mathf.Max(1, opponent.MaxHealth)) * 100);
        }

        public void PlayHeroAttack() => PlayHeroAnimation("Attack");

        public void PlayHeroHurt() => PlayHeroAnimation("Hurt");

        /// <summary>Play a named hero animation configured in the Inspector.</summary>
        public void PlayHeroAnimation(string animationName)
        {
            if (!Application.isPlaying) return;
            foreach (var binding in characterAnimations)
            {
                if (binding == null || binding.characterSlot != CharacterSlot.Hero ||
                    !string.Equals(binding.animationName, animationName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                PlayCharacterAnimation(binding);
                return;
            }
            Debug.LogWarning("No hero animation is configured with the name '" + animationName + "'.", this);
        }

        /// <summary>Play a configured scene animation by character and animation name.</summary>
        public void PlayCharacterAnimation(string characterName, string animationName)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(characterName) ||
                string.IsNullOrEmpty(animationName)) return;
            foreach (var binding in characterAnimations)
            {
                if (binding == null ||
                    !string.Equals(binding.characterName, characterName, System.StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(binding.animationName, animationName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                PlayCharacterAnimation(binding);
                return;
            }

            Debug.LogWarning("No character animation is configured for '" + characterName + " / " + animationName + "'.", this);
        }

        private void PlayCharacterAnimation(CharacterAnimationBinding binding)
        {
            if (CombatSequenceRunning) return;
            if (!Application.isPlaying || binding.renderer == null) return;
            var animator = binding.animator != null ? binding.animator : binding.renderer.GetComponent<Animator>();
            if (animator == null || !animator.enabled ||
                animator.runtimeAnimatorController == null)
            {
                SetText(animationStatus, "Assign an enabled Animator in the Inspector.");
                return;
            }
            RestoreAnimationVisibility();
            // Pose objects are authored separately in this scene. Show exactly one pose per slot,
            // preserving every authored Transform and restoring visibility after playback.
            var idle = binding.characterSlot == CharacterSlot.Hero ? heroRenderer : enemyRenderer;
            RememberVisibility(idle);
            foreach (var pose in characterAnimations)
                if (pose != null && pose.characterSlot == binding.characterSlot) RememberVisibility(pose.renderer);
            RememberVisibility(binding.renderer);
            foreach (var entry in animationVisibility)
                if (entry.Key != null) entry.Key.SetActive(entry.Key == binding.renderer.gameObject);
            animator.Play(0, 0, 0);
            if (binding.characterSlot == CharacterSlot.Hero) activeHeroRenderer = binding.renderer;
            else activeEnemyRenderer = binding.renderer;
            SetText(animationStatus, binding.characterName + " / " + binding.animationName);
            if (binding.renderer != idle)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                float duration = clips.Length > 0 ? clips[0].length / Mathf.Max(0.01f, Mathf.Abs(animator.speed)) : 1f;
                animationPreview = StartCoroutine(RestoreAfterAnimation(duration));
            }
        }

        private void RememberVisibility(SpriteRenderer renderer)
        {
            if (renderer != null && !animationVisibility.ContainsKey(renderer.gameObject))
                animationVisibility.Add(renderer.gameObject, renderer.gameObject.activeSelf);
        }

        private System.Collections.IEnumerator RestoreAfterAnimation(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            animationPreview = null;
            RestoreAnimationVisibility();
        }

        private void RestoreAnimationVisibility()
        {
            if (animationPreview != null) { StopCoroutine(animationPreview); animationPreview = null; }
            foreach (var entry in animationVisibility)
                if (entry.Key != null) entry.Key.SetActive(entry.Value);
            animationVisibility.Clear();
            activeHeroRenderer = heroRenderer;
            activeEnemyRenderer = enemyRenderer;
        }

        private void BuildAnimationControls()
        {
            animationButtons.Clear();
            AddAnimationControl("Voss", "Idle", CharacterSlot.Hero, heroRenderer,
                heroRenderer != null ? heroRenderer.GetComponent<Animator>() : null);
            AddAnimationControl("Wrag", "Idle", CharacterSlot.Enemy, enemyRenderer,
                enemyRenderer != null ? enemyRenderer.GetComponent<Animator>() : null);
            foreach (var binding in characterAnimations)
            {
                if (binding == null || string.IsNullOrEmpty(binding.characterName) ||
                    string.IsNullOrEmpty(binding.animationName)) continue;
                AddAnimationControl(binding.characterName, binding.animationName, binding.characterSlot,
                    binding.renderer, binding.animator);
            }
        }

        private void AddAnimationControl(string characterName, string animationName, CharacterSlot slot,
            SpriteRenderer renderer, Animator animator)
        {
            if (animator == null && renderer != null) animator = renderer.GetComponent<Animator>();
            var button = new Button { text = characterName + " / " + animationName };
            button.AddToClassList("animation-choice");
            button.SetEnabled(renderer != null && animator != null);
            button.clicked += () =>
            {
                if (renderer == null || animator == null)
                {
                    Debug.LogWarning("Assign a Sprite Renderer and Animator to '" + characterName + " / " +
                        animationName + "' to preview it.", this);
                    return;
                }

                var binding = new CharacterAnimationBinding
                {
                    characterName = characterName,
                    animationName = animationName,
                    characterSlot = slot,
                    renderer = renderer,
                    animator = animator
                };
                PlayCharacterAnimation(binding);
            };
            animationButtons.Add(button);
        }

        private void StopAnimationPreview()
        {
            if (CombatSequenceRunning) return;
            if (!Application.isPlaying) return;
            RestoreAnimationVisibility();
            activeHeroRenderer = heroRenderer;
            activeEnemyRenderer = enemyRenderer;
            foreach (var renderer in new[] { heroRenderer, enemyRenderer })
            {
                var animator = renderer != null ? renderer.GetComponent<Animator>() : null;
                if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                    animator.Play(0, 0, 0);
            }
            SetText(animationStatus, "Idle animations restarted; scene visibility is unchanged");
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            root.EnableInClassList("compact", evt.newRect.width < 1500);
            root.EnableInClassList("narrow", evt.newRect.width < 1100);
            root.EnableInClassList("portrait-layout", evt.newRect.height > evt.newRect.width);
        }

        private void RefreshActorPlacement()
        {
            if (root == null || root.panel == null) return;
            var camera = Camera.main;
            if (camera == null) return;
            PlaceTarget(selfTarget, activeHeroRenderer != null ? activeHeroRenderer : heroRenderer, camera);
            PlaceTarget(enemyTarget, activeEnemyRenderer != null ? activeEnemyRenderer : enemyRenderer, camera);
        }

        private void PlaceTarget(Button target, SpriteRenderer renderer, Camera camera)
        {
            if (target == null || target.parent == null) return;
            bool visible = renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                renderer.sprite != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 &&
                GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), renderer.bounds);
            target.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
            if (!visible) return;
            var bounds = renderer.bounds;
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var screen = camera.WorldToScreenPoint(corner);
                if (screen.z < camera.nearClipPlane) continue;
                var panel = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
                var local = target.parent.WorldToLocal(panel);
                minimum = Vector2.Min(minimum, local);
                maximum = Vector2.Max(maximum, local);
            }
            if (!float.IsFinite(minimum.x)) { target.style.visibility = Visibility.Hidden; return; }
            target.style.right = StyleKeyword.Auto;
            target.style.bottom = StyleKeyword.Auto;
            target.style.left = minimum.x;
            target.style.top = minimum.y;
            target.style.width = maximum.x - minimum.x;
            target.style.height = maximum.y - minimum.y;
        }

        private void ToggleOptions() => options.ToggleInClassList("hidden");
        private void CloseOptions() => options.AddToClassList("hidden");
        private void FocusHand()
        {
            if (cardButtons.Count > 0) cardButtons[0].Focus();
            SetText(hint, showingPreview ? "Design preview · gameplay is inactive" : "Select a card, then choose a target");
        }

        private static void SetText(Label label, string value)
        {
            if (label.text != value) label.text = value;
        }

        private static string Describe(Skill action)
        {
            if (action.cardAction.value == 0) return "";
            switch (action.cardAction.actionType)
            {
                case ActionType.Damage: return "Deal " + action.cardAction.value + " damage.\n";
                case ActionType.Heal: return "Heal " + action.cardAction.value + " HP.\n";
                case ActionType.Armor: return "Gain " + action.cardAction.value + " Block.\n";
                case ActionType.StatusEffect: return action.cardAction.statusType + " " +action.cardAction.value + " for " + action.cardAction.durationType + " turns.\n";
                default: return "";
            }
        }
    }
}
