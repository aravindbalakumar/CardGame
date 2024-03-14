namespace Game.ActorAndPlayers.AI.Behaviour.Editor
{
    using Game.ActorAndPlayers.AI.Behaviour;
    using UnityEditor;

    [CustomEditor(typeof(BehaviourNode))]
    public class BehaviourNodeEditor : Editor
    {
        SerializedProperty m_conditions, m_shouldIncludeAllCondition, m_botAction, m_nodeType, m_negativeOutcome, m_outcome;
        bool arrangeBool;
        void OnEnable()
        {
            m_conditions = serializedObject.FindProperty("conditions");
            m_shouldIncludeAllCondition = serializedObject.FindProperty("shouldIncludeAllCondition");
            m_botAction = serializedObject.FindProperty("botAction");
            m_nodeType = serializedObject.FindProperty("nodeType");
            m_negativeOutcome = serializedObject.FindProperty("negativeOutcome");
            m_outcome = serializedObject.FindProperty("outcome");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            arrangeBool = m_nodeType.enumValueIndex == 0;
            EditorGUILayout.PropertyField(m_nodeType);
            if (arrangeBool)
            {
                EditorGUILayout.PropertyField(m_conditions);
                EditorGUILayout.PropertyField(m_shouldIncludeAllCondition);
                EditorGUILayout.PropertyField(m_outcome);
                EditorGUILayout.PropertyField(m_negativeOutcome);

            }
            else
            {
                EditorGUILayout.PropertyField(m_botAction);
                EditorGUILayout.PropertyField(m_outcome);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
