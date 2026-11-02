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
    public sealed class GameViewUIToolkit : MonoBehaviour
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
        [SerializeField] private UserPlayer localPlayer;
        [SerializeField] private Player opponent;
        [SerializeField] private Transform legacyCardParent;
        [SerializeField] private SpriteRenderer heroRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [Tooltip("Add one entry per animation pose. The assigned scene object's Transform controls its placement.")]
        [SerializeField] private List<CharacterAnimationBinding> characterAnimations = new List<CharacterAnimationBinding>();
        [SerializeField] private VisualTreeAsset cardTemplate;
        private CardCatalog catalog;
        private int catalogRevision = -1;
        private ScrollView handScroll;

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
        private bool showingPreview, browsingCatalog, initialized, turnAvailable = true;
        private bool DisplaysCatalog => showingPreview || browsingCatalog;
        private SpriteRenderer activeHeroRenderer;
        private SpriteRenderer activeEnemyRenderer;
        private int legacyChildCount;
        private bool animationControlsDirty;
        private readonly Dictionary<GameObject, bool> animationVisibility = new Dictionary<GameObject, bool>();
        private Coroutine animationPreview;

        private bool HasLiveGame => Application.isPlaying && localPlayer != null &&
            localPlayer.isActiveAndEnabled && !string.IsNullOrEmpty(localPlayer.playerID) && localPlayer.currentHand != null;

        private void OnEnable()
        {
            if (initialized) return;
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
            handScroll = root.Q<ScrollView>("hand-scroll");
            catalog = CardCatalog.Load();
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
            deckButton.clicked += ToggleCatalog;
            drawPile.clicked += FocusHand;
            BuildAnimationControls();
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (Application.isPlaying)
            {
                if (localPlayer != null) localPlayer.OnPlayerUIUpdate += OnPlayerStatus;
                if (opponent != null) opponent.OnPlayerUIUpdate += OnOpponentStatus;
                if (gameHandler != null) gameHandler.OnGameSystemsInitialized += OnSystemsInitialized;
                if (gameHandler != null && gameHandler.inputHandler != null)
                    gameHandler.inputHandler.IsPointerInputBlocked += BlocksWorldPointerInput;
            }
            showingPreview = !HasLiveGame;
            initialized = true;
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
            RestoreAnimationVisibility();
            refreshTask?.Pause();
            refreshTask = null;
            if (!initialized) return;
            if (localPlayer != null) localPlayer.OnPlayerUIUpdate -= OnPlayerStatus;
            if (opponent != null) opponent.OnPlayerUIUpdate -= OnOpponentStatus;
            if (gameHandler != null) gameHandler.OnGameSystemsInitialized -= OnSystemsInitialized;
            if (gameHandler != null && gameHandler.inputHandler != null)
                gameHandler.inputHandler.IsPointerInputBlocked -= BlocksWorldPointerInput;
            endTurn.clicked -= EndTurn;
            selfTarget.clicked -= TargetSelf;
            enemyTarget.clicked -= TargetOpponent;
            settingsButton.clicked -= ToggleOptions;
            closeOptions.clicked -= CloseOptions;
            stopAnimationPreview.clicked -= StopAnimationPreview;
            deckButton.clicked -= ToggleCatalog;
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
            if (initialized) RefreshActorPlacement();
        }

        private void OnSystemsInitialized() => root.schedule.Execute(() => RefreshFromGame(true));

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

        private void RefreshFromGame(bool force)
        {
            if (!initialized) return;
            if (animationControlsDirty)
            {
                animationControlsDirty = false;
                BuildAnimationControls();
                force = true;
            }
            if (catalog == null) catalog = CardCatalog.Load();
            if (catalog != null && catalogRevision != catalog.Revision)
            {
                catalogRevision = catalog.Revision;
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
                SetText(energyValue, preview ? "3/3" : "\u2014");
                SetText(currencyCount, preview ? "15" : "\u2014");
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
            if (Application.isPlaying && gameHandler != null && selectedCard != gameHandler.SelectedCard)
            {
                selectedCard = gameHandler.SelectedCard;
                RefreshAvailability();
            }
            if (force || modeChanged) RefreshAvailability();
            RefreshActorPlacement();
        }

        private bool HandChanged()
        {
            if (DisplaysCatalog) return false;
            if (legacyCardParent != null && legacyCardParent.childCount != legacyChildCount) return true;
            var current = localPlayer.currentHand;
            if (displayedHand.Count != current.Count) return true;
            for (int i = 0; i < current.Count; i++)
                if (displayedHand[i] != current[i]) return true;
            return false;
        }

        private void RebuildHand()
        {
            hand.Clear();
            cardButtons.Clear();
            displayedHand.Clear();
            legacyCards.Clear();
            if (!DisplaysCatalog && gameHandler != null) gameHandler.UpdateCardDeck();
            legacyChildCount = legacyCardParent != null ? legacyCardParent.childCount : 0;
            if (cardTemplate == null) return;
            if (!DisplaysCatalog && legacyCardParent != null)
                legacyCardParent.GetComponentsInChildren(false, legacyCards);
            int count = DisplaysCatalog ? (catalog != null ? catalog.Entries.Count : 0) : localPlayer.currentHand.Count;
            for (int i = 0; i < count; i++)
            {
                var tree = cardTemplate.CloneTree();
                var button = tree.Q<Button>("card");
                button.RemoveFromHierarchy();
                hand.Add(button);
                cardButtons.Add(button);
                button.RegisterCallback<FocusInEvent>(_ => handScroll.ScrollTo(button));
                if (DisplaysCatalog)
                {
                    var entry = catalog.Entries[i];
                    button.AddToClassList("preview-card");
                    button.Q<Image>("card-frame").image = entry.image;
                    button.tooltip = entry.title + (entry.data != null
                        ? " - " + Describe(entry.data.cardAction) + Describe(entry.data.postCardAction)
                        : " - artwork preview; no gameplay data assigned.");
                    // Preview cards may be inspected and focused, but never execute game actions.
                    int index = i;
                    button.clicked += () => InspectPreview(index);
                }
                else
                {
                    var data = localPlayer.currentHand[i];
                    displayedHand.Add(data);
                    if (data == null) { button.SetEnabled(false); continue; }
                    button.AddToClassList("data-card");
                    var face = catalog != null ? catalog.FindImage(data) : data.fullCardTexture;
                    button.Q<Image>("card-frame").image = face;
                    button.EnableInClassList("missing-face", face == null);
                    button.Q<Label>("card-title").text = data.metaData.name;
                    button.Q<Label>("card-cost").text = data.energyUsage.ToString();
                    button.Q<Image>("card-art").image = data.metaData.cardTexture;
                    button.Q<Label>("card-kind").text = data.cardAction.actionType.ToString();
                    button.Q<Label>("card-description").text = Describe(data.cardAction) + Describe(data.postCardAction);
                    button.tooltip = data.metaData.name + " \u2014 select, then choose a highlighted target.";
                    int index = i;
                    button.clicked += () => SelectCard(index);
                }
            }
            SetText(handCount, count.ToString());
            root.Q<Label>("hand-empty")?.EnableInClassList("hidden", count > 0);
            RefreshAvailability();
            SetText(root.Q<Label>("discard-count"), DisplaysCatalog ? "0" : "\u2014");
        }

        private void InspectPreview(int index)
        {
            for (int i = 0; i < cardButtons.Count; i++)
            {
                bool selected = i == index;
                cardButtons[i].EnableInClassList("selected", selected);
                cardButtons[i].Q<Image>("card-frame").tintColor = selected ? new Color(1f, 0.91f, 0.71f) : Color.white;
            }
            handScroll.ScrollTo(cardButtons[index]);
            SetText(hint, catalog.Entries[index].title + " - artwork preview");
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
            if (DisplaysCatalog || !turnAvailable || gameHandler == null) return;
            var legacyCard = FindLegacyCard(index);
            if (legacyCard == null) return;
            // Uses the original selection/targeting path, including its existing highlights.
            legacyCard.OnCardSelected();
            selectedCard = gameHandler.SelectedCard;
            RefreshAvailability();
            handScroll.ScrollTo(cardButtons[index]);
        }

        private void RefreshAvailability()
        {
            bool ready = !showingPreview && turnAvailable && gameHandler != null && localPlayer.Health > 0;
            endTurn.SetEnabled(ready);
            playerStatus.EnableInClassList("active-player", DisplaysCatalog || ready);
            playerStatus.EnableInClassList("inactive-player", !DisplaysCatalog && !ready);
            bool selected = ready && !browsingCatalog && selectedCard != null && selectedCard.cardData != null;
            selfTarget.SetEnabled(selected && selectedCard.cardTargetGroup.HasFlag(TargetGroup.self));
            enemyTarget.SetEnabled(selected && opponent != null && opponent.isActiveAndEnabled && selectedCard.cardTargetGroup.HasFlag(TargetGroup.others));
            for (int i = 0; i < cardButtons.Count; i++)
            {
                if (DisplaysCatalog) continue;
                var card = FindLegacyCard(i);
                cardButtons[i].SetEnabled(ready && card != null);
                bool isSelected = card != null && card == selectedCard;
                cardButtons[i].EnableInClassList("selected", isSelected);
                cardButtons[i].Q<Image>("card-frame").tintColor = isSelected ? new Color(1f, 0.91f, 0.71f) : Color.white;
            }
            if (!DisplaysCatalog) SetText(hint, selected ? "Choose a target" : "");
        }

        private void TargetSelf() => Target(localPlayer);
        private void TargetOpponent() => Target(opponent);
        private void Target(Player target)
        {
            if (DisplaysCatalog || !turnAvailable || target == null || selectedCard == null) return;
            // The existing handler owns card effects and turn completion.
            gameHandler.inputHandler.OnRaycastHit.Invoke(target.gameObject);
        }

        private void EndTurn()
        {
            if (!HasLiveGame || !turnAvailable) return;
            localPlayer.OnTurnEnd();
            if (gameHandler.SelectedCard != null) gameHandler.UpdateSelectedCard(gameHandler.SelectedCard);
            selectedCard = null;
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
        private void ToggleCatalog()
        {
            browsingCatalog = !browsingCatalog;
            RefreshFromGame(true);
            SetText(hint, browsingCatalog ? "Card collection - select a card to inspect" : "Select a card, then choose a target");
        }

        private void FocusHand()
        {
            if (browsingCatalog)
            {
                browsingCatalog = false;
                RefreshFromGame(true);
            }
            if (cardButtons.Count > 0) cardButtons[0].Focus();
            SetText(hint, showingPreview ? "Design preview \u00b7 gameplay is inactive" : "Select a card, then choose a target");
        }

        private static void SetText(Label label, string value)
        {
            if (label.text != value) label.text = value;
        }

        private static string Describe(CardAction action)
        {
            if (action.value == 0) return "";
            switch (action.actionType)
            {
                case CardActionType.Attack: return "Deal " + action.value + " damage.\n";
                case CardActionType.Heal: return "Heal " + action.value + " HP.\n";
                case CardActionType.Armor: return "Gain " + action.value + " Block.\n";
                case CardActionType.StatusEffect: return action.statusType + " " + action.value + " for " + action.duration + " turns.\n";
                default: return "";
            }
        }
    }
}
