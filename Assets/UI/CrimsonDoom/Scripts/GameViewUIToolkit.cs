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
        [SerializeField] private GameHandler gameHandler;
        [SerializeField] private UserPlayer localPlayer;
        [SerializeField] private Player opponent;
        [SerializeField] private Transform legacyCardParent;
        [SerializeField] private SpriteRenderer heroRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [SerializeField] private VisualTreeAsset cardTemplate;
        [SerializeField] private Sprite[] previewCardSprites;
        [SerializeField] private Sprite[] enemyFrames;
        [Tooltip("UV bounds excluding the transparent padding in the existing hero frames.")]
        [SerializeField] private Rect heroArtBounds = new Rect(0.09f, 0.20f, 0.82f, 0.60f);
        [Tooltip("Fixed world rectangle represented by the battlefield background, independent of actor Transforms.")]
        [SerializeField] private Rect battlefieldWorldBounds = new Rect(-9.605f, -4.095f, 19.21f, 8.19f);

        private static readonly string[] PreviewDescriptions =
        {
            "Deal 10 damage.", "Return a fallen\nally to 50% HP.", "Deal 10 damage.",
            "Heal 10 HP.", "Gain 3 Block\nand 1 Strength."
        };

        private readonly List<CardTypeScriptableObject> displayedHand = new List<CardTypeScriptableObject>();
        private readonly List<Button> cardButtons = new List<Button>();
        private readonly List<Card> legacyCards = new List<Card>();
        private VisualElement root, hand, playerFill, enemyFill, options, playerStatus, intentPreview, battlefieldBackground;
        private Label headerHealth, playerHealth, block, enemyHealth, enemyName, hint;
        private Label handCount, previewLabel, energyValue, currencyCount;
        private Button endTurn, selfTarget, enemyTarget, settingsButton, closeOptions, deckButton, drawPile;
        private Image heroArt, enemyArt;
        private Toggle animateArt;
        private IVisualElementScheduledItem refreshTask;
        private Card selectedCard;
        private bool showingPreview, initialized, turnAvailable = true;
        private Sprite lastHeroSprite, lastEnemySprite;
        private int legacyChildCount;

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
            battlefieldBackground = root.Q(className: "background");
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
            heroArt = root.Q<Image>("hero-art");
            enemyArt = root.Q<Image>("enemy-art");
            options = root.Q("options-panel");
            intentPreview = root.Q("intent-preview");
            settingsButton = root.Q<Button>("settings-button");
            closeOptions = root.Q<Button>("close-options");
            deckButton = root.Q<Button>("deck-button");
            drawPile = root.Q<Button>("draw-pile");
            animateArt = root.Q<Toggle>("animate-art");
            endTurn.clicked += EndTurn;
            selfTarget.clicked += TargetSelf;
            enemyTarget.clicked += TargetOpponent;
            settingsButton.clicked += ToggleOptions;
            closeOptions.clicked += CloseOptions;
            deckButton.clicked += FocusHand;
            drawPile.clicked += FocusHand;
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
            RefreshFromGame(true);
            // Existing hand lists and selection have no events; compare at 10 Hz, never rebuild per frame.
            refreshTask = root.schedule.Execute(() => RefreshFromGame(false)).Every(100);
        }

        // UIDocument builds its visual tree during OnEnable; Start handles component enable order.
        private void Start()
        {
            if (!initialized) OnEnable();
        }

        private System.Collections.IEnumerator WaitForDocument()
        {
            yield return null;
            if (isActiveAndEnabled && !initialized) OnEnable();
        }

        private void OnDisable()
        {
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
            deckButton.clicked -= FocusHand;
            drawPile.clicked -= FocusHand;
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            displayedHand.Clear();
            legacyCards.Clear();
            cardButtons.Clear();
            lastHeroSprite = lastEnemySprite = null;
            initialized = false;
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
            if (Application.isPlaying && gameHandler != null && selectedCard != gameHandler.SelectedCard)
            {
                selectedCard = gameHandler.SelectedCard;
                RefreshAvailability();
            }
            if (force || modeChanged) RefreshAvailability();
            if (animateArt.value || force) RefreshActorArt();
            RefreshActorPlacement();
        }

        private bool HandChanged()
        {
            if (showingPreview) return false;
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
            legacyChildCount = legacyCardParent != null ? legacyCardParent.childCount : 0;
            if (cardTemplate == null) return;
            if (!showingPreview && legacyCardParent != null)
                legacyCardParent.GetComponentsInChildren(false, legacyCards);
            int count = showingPreview ? previewCardSprites.Length : localPlayer.currentHand.Count;
            for (int i = 0; i < count; i++)
            {
                var tree = cardTemplate.CloneTree();
                var button = tree.Q<Button>("card");
                button.RemoveFromHierarchy();
                hand.Add(button);
                cardButtons.Add(button);
                button.EnableInClassList("card-first", i == 0);
                button.EnableInClassList("card-last", i == count - 1);
                if (showingPreview)
                {
                    button.AddToClassList("preview-card");
                    button.Q<Image>("card-frame").sprite = previewCardSprites[i];
                    button.Q<Label>("card-description").text = PreviewDescriptions[i % PreviewDescriptions.Length];
                    button.tooltip = "Design preview — activate gameplay to use cards.";
                    // Preview cards may be inspected and focused, but never execute game actions.
                    int index = i;
                    button.clicked += () => InspectPreview(index);
                }
                else
                {
                    var data = localPlayer.currentHand[i];
                    displayedHand.Add(data);
                    if (data == null) { button.SetEnabled(false); continue; }
                    button.Q<Image>("card-frame").sprite = previewCardSprites[0];
                    button.Q<Label>("card-title").text = data.metaData.name;
                    button.Q<Label>("card-cost").text = data.energyUsage.ToString();
                    button.Q<Image>("card-art").image = data.metaData.cardTexture;
                    button.Q<Label>("card-description").text = Describe(data.cardAction) + Describe(data.postCardAction);
                    button.tooltip = data.metaData.name + " — select, then choose a highlighted target.";
                    int index = i;
                    button.clicked += () => SelectCard(index);
                }
            }
            SetText(handCount, count.ToString());
        }

        private void InspectPreview(int index)
        {
            for (int i = 0; i < cardButtons.Count; i++) cardButtons[i].EnableInClassList("selected", i == index);
            SetText(hint, "Design preview · gameplay is inactive");
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
            if (showingPreview || !turnAvailable || gameHandler == null) return;
            var legacyCard = FindLegacyCard(index);
            if (legacyCard == null) return;
            // Uses the original selection/targeting path, including its existing highlights.
            legacyCard.OnCardSelected();
            selectedCard = gameHandler.SelectedCard;
            RefreshAvailability();
        }

        private void RefreshAvailability()
        {
            bool ready = !showingPreview && turnAvailable && gameHandler != null && localPlayer.Health > 0;
            endTurn.SetEnabled(ready);
            playerStatus.EnableInClassList("active-player", showingPreview || ready);
            playerStatus.EnableInClassList("inactive-player", !showingPreview && !ready);
            bool selected = ready && selectedCard != null && selectedCard.cardData != null;
            selfTarget.SetEnabled(selected && selectedCard.cardTargetGroup.HasFlag(TargetGroup.self));
            enemyTarget.SetEnabled(selected && opponent != null && opponent.isActiveAndEnabled && selectedCard.cardTargetGroup.HasFlag(TargetGroup.others));
            for (int i = 0; i < cardButtons.Count; i++)
            {
                if (showingPreview) continue;
                var card = FindLegacyCard(i);
                cardButtons[i].SetEnabled(ready && card != null);
                cardButtons[i].EnableInClassList("selected", card != null && card == selectedCard);
            }
            if (!showingPreview) SetText(hint, selected ? "Choose a target" : "");
        }

        private void TargetSelf() => Target(localPlayer);
        private void TargetOpponent() => Target(opponent);
        private void Target(Player target)
        {
            if (showingPreview || !turnAvailable || target == null || selectedCard == null) return;
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

        private void RefreshActorArt()
        {
            if (heroRenderer != null && heroRenderer.sprite != lastHeroSprite)
            {
                lastHeroSprite = heroRenderer.sprite;
                heroArt.image = lastHeroSprite.texture;
                heroArt.uv = heroArtBounds;
            }
            Sprite enemySprite = enemyRenderer != null ? enemyRenderer.sprite : null;
            if (enemyFrames != null && enemyFrames.Length > 0)
                enemySprite = enemyFrames[Application.isPlaying ? Mathf.FloorToInt(Time.unscaledTime * 12f) % enemyFrames.Length : 0];
            if (enemySprite != null && enemySprite != lastEnemySprite)
            {
                lastEnemySprite = enemySprite;
                enemyArt.sprite = lastEnemySprite;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            root.EnableInClassList("compact", evt.newRect.width < 1500);
            root.EnableInClassList("narrow", evt.newRect.width < 1100);
            root.EnableInClassList("portrait-layout", evt.newRect.height > evt.newRect.width);
        }

        private void RefreshActorPlacement()
        {
            if (battlefieldBackground == null || battlefieldBackground.panel == null ||
                battlefieldWorldBounds.width <= 0 || battlefieldWorldBounds.height <= 0) return;
            var backgroundRect = battlefieldBackground.contentRect;
            // A UIDocument can be attached before its first layout, when dimensions are NaN.
            if (!(backgroundRect.width > 0) || !(backgroundRect.height > 0)) return;

            // Match the background's centered "cover" layout. No actor provides the origin or
            // scale for another actor, so changing either Transform affects only its own image.
            float unitsToPanel = Mathf.Max(backgroundRect.width / battlefieldWorldBounds.width,
                backgroundRect.height / battlefieldWorldBounds.height);
            var panelOrigin = battlefieldBackground.LocalToWorld(backgroundRect.center);
            if (heroRenderer != null && heroRenderer.sprite != null)
            {
                var sprite = heroRenderer.sprite;
                var crop = new Rect(sprite.texture.width * heroArtBounds.x - sprite.rect.x,
                    sprite.texture.height * heroArtBounds.y - sprite.rect.y,
                    sprite.texture.width * heroArtBounds.width, sprite.texture.height * heroArtBounds.height);
                PlaceActorImage(heroArt, selfTarget, heroRenderer, sprite, crop, panelOrigin, unitsToPanel);
            }
            if (enemyRenderer != null && enemyArt != null && enemyArt.sprite != null)
            {
                var sprite = enemyArt.sprite;
                PlaceActorImage(enemyArt, enemyTarget, enemyRenderer, sprite,
                    new Rect(Vector2.zero, sprite.rect.size), panelOrigin, unitsToPanel);
            }
        }

        private void PlaceActorImage(Image art, Button target, SpriteRenderer renderer, Sprite sprite,
            Rect crop, Vector2 panelOrigin, float unitsToPanel)
        {
            if (art == null || art.parent == null) return;
            var actorTransform = renderer.transform;
            var actorScale = actorTransform.lossyScale;
            var center = (crop.center - sprite.pivot) / sprite.pixelsPerUnit;
            if (renderer.flipX) center.x = -center.x;
            if (renderer.flipY) center.y = -center.y;
            var worldCenter = actorTransform.TransformPoint(new Vector3(center.x, center.y, 0));
            var delta = (Vector2)worldCenter - battlefieldWorldBounds.center;
            var panelCenter = panelOrigin + new Vector2(delta.x, -delta.y) * unitsToPanel;
            float width = crop.width / sprite.pixelsPerUnit * Mathf.Abs(actorScale.x) * unitsToPanel;
            float height = crop.height / sprite.pixelsPerUnit * Mathf.Abs(actorScale.y) * unitsToPanel;
            var rotation = new Rotate(-actorTransform.eulerAngles.z);
            var scale = new Scale(new Vector3(
                Mathf.Sign(actorScale.x) * (renderer.flipX ? -1 : 1),
                Mathf.Sign(actorScale.y) * (renderer.flipY ? -1 : 1), 1));
            SetActorRect(art, panelCenter, width, height, rotation, scale);
            if (target != null) SetActorRect(target, panelCenter, width, height, rotation, scale);
        }

        private static void SetActorRect(VisualElement element, Vector2 panelCenter,
            float width, float height, Rotate rotation, Scale scale)
        {
            var localCenter = element.parent.WorldToLocal(panelCenter);
            element.style.right = StyleKeyword.Auto;
            element.style.bottom = StyleKeyword.Auto;
            element.style.left = localCenter.x - width * 0.5f;
            element.style.top = localCenter.y - height * 0.5f;
            element.style.width = width;
            element.style.height = height;
            element.style.rotate = rotation;
            element.style.scale = scale;
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
