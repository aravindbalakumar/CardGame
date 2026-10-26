namespace CrimsonDoom.UI.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>Edits the actual scene Transform; previews only sprite keys, never Transform curves.</summary>
    [CustomEditor(typeof(GameViewUIToolkit))]
    public sealed class GameViewUIToolkitEditor : Editor
    {
        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        private readonly List<bool> enemySlots = new List<bool>();
        private PopupField<string> animation;
        private ObjectField selectedObject, clipField;
        private Vector3Field position, rotation, scale;
        private VisualElement transformFields;
        private Label status;
        private SpriteRenderer selectedRenderer, previewRenderer;
        private Sprite originalSprite;
        private ObjectReferenceKeyframe[] spriteKeys;
        private AnimationClip clip;
        private bool enemy, playing;
        private double startedAt;
        private string choicesSignature;
        private string SelectionKey => "CrimsonDoom.AnimationObject." + target.GetEntityId();

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label("Animation Transform & Preview") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(new HelpBox("Select an animation object, adjust its local Transform, then preview. Changes support Undo and are saved with the scene. Sprite preview does not animate the Transform.", HelpBoxMessageType.Info));
            animation = new PopupField<string>("Animation", new List<string> { "None" }, 0);
            animation.RegisterValueChangedCallback(evt =>
            {
                int index = animation.index;
                if (index >= 0 && index < renderers.Count) SelectRenderer(renderers[index], enemySlots[index]);
            });
            root.Add(animation);
            selectedObject = new ObjectField("Sprite object") { objectType = typeof(SpriteRenderer), allowSceneObjects = true };
            selectedObject.RegisterValueChangedCallback(evt => SelectRenderer(evt.newValue as SpriteRenderer, enemy));
            root.Add(selectedObject);
            transformFields = new VisualElement();
            position = new Vector3Field("Position");
            rotation = new Vector3Field("Rotation");
            scale = new Vector3Field("Scale");
            position.RegisterValueChangedCallback(evt => EditTransform(0, evt.newValue));
            rotation.RegisterValueChangedCallback(evt => EditTransform(1, evt.newValue));
            scale.RegisterValueChangedCallback(evt => EditTransform(2, evt.newValue));
            transformFields.Add(position);
            transformFields.Add(rotation);
            transformFields.Add(scale);
            root.Add(transformFields);
            clipField = new ObjectField("Preview clip") { objectType = typeof(AnimationClip), allowSceneObjects = false };
            clipField.RegisterValueChangedCallback(evt => { StopPreview(); clip = evt.newValue as AnimationClip; });
            root.Add(clipField);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            buttons.Add(new Button(StartPreview) { text = "Play Preview" });
            buttons.Add(new Button(StopPreview) { text = "Stop Preview" });
            buttons.Add(new Button(() =>
            {
                if (selectedRenderer == null) return;
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView == null) sceneView = EditorWindow.GetWindow<SceneView>();
                sceneView.in2DMode = true;
                sceneView.Frame(selectedRenderer.bounds, false);
            }) { text = "Frame in Scene" });
            root.Add(buttons);
            status = new Label("Preview is available outside Play Mode.");
            status.style.whiteSpace = WhiteSpace.Normal;
            root.Add(status);
            var settings = new Foldout { text = "Bindings and UI settings", value = true };
            InspectorElement.FillDefaultInspector(settings, serializedObject, this);
            root.Add(settings);
            RefreshChoices();
            string savedId = SessionState.GetString(SelectionKey, "");
            int savedIndex = renderers.FindIndex(r => r != null && r.GetEntityId().ToString() == savedId);
            if (savedIndex >= 0)
            {
                animation.SetValueWithoutNotify(animation.choices[savedIndex]);
                SelectRenderer(renderers[savedIndex], enemySlots[savedIndex]);
            }
            else if (renderers.Count > 0) SelectRenderer(renderers[0], enemySlots[0]);
            root.schedule.Execute(() => { RefreshChoices(); SyncTransformFields(); }).Every(200);
            return root;
        }

        private void OnEnable()
        {
            EditorApplication.update += TickPreview;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private void OnDisable()
        {
            StopPreview();
            EditorApplication.update -= TickPreview;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
        }

        private void OnPlayModeChanged(PlayModeStateChange state) => StopPreview();
        private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path) => StopPreview();

        private void RefreshChoices()
        {
            if (target == null) return;
            serializedObject.UpdateIfRequiredOrScript();
            var labels = new List<string>();
            renderers.Clear();
            enemySlots.Clear();
            AddChoice(labels, "Voss / Idle", serializedObject.FindProperty("heroRenderer"), false);
            AddChoice(labels, "Wrag / Idle", serializedObject.FindProperty("enemyRenderer"), true);
            var bindings = serializedObject.FindProperty("characterAnimations");
            for (int i = 0; i < bindings.arraySize; i++)
            {
                var entry = bindings.GetArrayElementAtIndex(i);
                AddChoice(labels, entry.FindPropertyRelative("characterName").stringValue + " / " +
                    entry.FindPropertyRelative("animationName").stringValue + " [" + (i + 1) + "]",
                    entry.FindPropertyRelative("renderer"), entry.FindPropertyRelative("characterSlot").enumValueIndex == 1);
            }
            string signature = string.Join("|", labels) + string.Join(",", renderers.Select(r => r.GetEntityId())) +
                string.Join(",", enemySlots);
            if (signature == choicesSignature) return;
            choicesSignature = signature;
            animation.choices = labels.Count > 0 ? labels : new List<string> { "None" };
            int index = renderers.IndexOf(selectedRenderer);
            animation.SetValueWithoutNotify(index >= 0 ? labels[index] : animation.choices[0]);
            if (index >= 0) enemy = enemySlots[index];
            // Never reinitialize a Transform when bindings are inserted, removed, or reordered.
        }

        private void AddChoice(List<string> labels, string label, SerializedProperty property, bool isEnemy)
        {
            var renderer = property.objectReferenceValue as SpriteRenderer;
            if (renderer == null) return;
            labels.Add(label);
            renderers.Add(renderer);
            enemySlots.Add(isEnemy);
        }

        private void SelectRenderer(SpriteRenderer renderer, bool isEnemy)
        {
            StopPreview();
            selectedRenderer = renderer;
            enemy = isEnemy;
            selectedObject.SetValueWithoutNotify(renderer);
            SessionState.SetString(SelectionKey, renderer != null ? renderer.GetEntityId().ToString() : "");
            var animator = renderer != null ? renderer.GetComponentInParent<Animator>(true) : null;
            clip = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips.FirstOrDefault() : null;
            clipField.SetValueWithoutNotify(clip);
            SyncTransformFields();
        }

        private void SyncTransformFields()
        {
            if (transformFields == null) return;
            transformFields.SetEnabled(selectedRenderer != null);
            if (selectedRenderer == null) return;
            var t = selectedRenderer.transform;
            // Preserve Euler input while focused (including negative and >360 degree values).
            var focused = rotation.panel?.focusController.focusedElement as VisualElement;
            if (focused == null || !position.Contains(focused)) position.SetValueWithoutNotify(t.localPosition);
            if (focused == null || !rotation.Contains(focused))
                if (Quaternion.Angle(Quaternion.Euler(rotation.value), t.localRotation) > 0.001f)
                    rotation.SetValueWithoutNotify(t.localEulerAngles);
            if (focused == null || !scale.Contains(focused)) scale.SetValueWithoutNotify(t.localScale);
        }

        private void EditTransform(int field, Vector3 value)
        {
            if (selectedRenderer == null || !float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z)) return;
            var t = selectedRenderer.transform;
            Undo.RecordObject(t, "Edit animation Transform");
            if (field == 0) t.localPosition = value;
            else if (field == 1) t.localRotation = Quaternion.Euler(value);
            else t.localScale = value;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            EditorUtility.SetDirty(t);
            if (!Application.isPlaying && t.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            SceneView.RepaintAll();
        }

        private void StartPreview()
        {
            StopPreview();
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                status.text = "Use the game's animation buttons in Play Mode. Exit Play Mode to save Transform edits.";
                return;
            }
            if (selectedRenderer == null || clip == null)
            {
                status.text = "Assign a sprite object and an AnimationClip containing its sprite sequence.";
                return;
            }
            var animator = selectedRenderer.GetComponentInParent<Animator>(true);
            string path = animator != null ? AnimationUtility.CalculateTransformPath(selectedRenderer.transform, animator.transform) : "";
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).FirstOrDefault(b =>
                b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite" && b.path == path);
            spriteKeys = binding.type != null ? AnimationUtility.GetObjectReferenceCurve(clip, binding) : null;
            if (spriteKeys == null || spriteKeys.Length == 0)
            {
                status.text = "This clip has no sprite sequence for the selected object. Add sprite frames in Unity's Animation window.";
                return;
            }
            previewRenderer = selectedRenderer;
            originalSprite = previewRenderer.sprite;
            startedAt = EditorApplication.timeSinceStartup;
            playing = true;
            status.text = "Previewing " + clip.name + ". Transform fields remain editable.";
            TickPreview();
        }

        private void TickPreview()
        {
            if (!playing) return;
            if (previewRenderer == null || clip == null || target == null) { StopPreview(); return; }
            float time = (float)((EditorApplication.timeSinceStartup - startedAt) % Mathf.Max(clip.length, 1f / Mathf.Max(1, clip.frameRate)));
            int index = 0;
            for (int i = 1; i < spriteKeys.Length && spriteKeys[i].time <= time; i++) index = i;
            previewRenderer.sprite = spriteKeys[index].value as Sprite;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private void StopPreview()
        {
            playing = false;
            if (previewRenderer != null) previewRenderer.sprite = originalSprite;
            previewRenderer = null;
            spriteKeys = null;
            if (status != null) status.text = "Preview stopped. Transform edits are retained; save the scene to keep them.";
            SceneView.RepaintAll();
        }
    }
}
