
using UnityEngine;
using UnityEditor;
using Game.ScriptableObjects.Card;

[CustomEditor(typeof(CardTypeScriptableObject))]
public class CardTypeScriptableObjectEditor : Editor
{
    SerializedProperty metaData;
    SerializedProperty energyUsage;
    SerializedProperty skillSection;
    private bool showMainCardAction;
    private bool showPostCardAction;
    private bool showNegativeCardAction;

    private void OnEnable()
    {
        metaData = serializedObject.FindProperty("metaData");
        skillSection = serializedObject.FindProperty("skill");
        energyUsage = serializedObject.FindProperty("energyUsage");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(energyUsage);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(skillSection);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(metaData, true);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSkillSection(SerializedProperty actionProperty)
    {
        SerializedProperty targetGroup = actionProperty.FindPropertyRelative("targetGroup");
        SerializedProperty cardAction = actionProperty.FindPropertyRelative("cardAction");
        SerializedProperty doesHavePostCardAction =actionProperty.FindPropertyRelative("doesHavePostCardAction");
        SerializedProperty postCardAction = actionProperty.FindPropertyRelative("postCardAction");
        SerializedProperty doesHaveNegativeEffect = actionProperty.FindPropertyRelative("doesHaveNegativeEffect");
        SerializedProperty negativeEffect = actionProperty.FindPropertyRelative("negativeEffect");
        EditorGUILayout.PropertyField(targetGroup);

        EditorGUILayout.Space();
        
        showMainCardAction = EditorGUILayout.Foldout(
            showMainCardAction,
            "Main Card Action",
            true
        );

        if (showMainCardAction)
        {
            EditorGUI.indentLevel++;
            DrawCardAction(cardAction);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(doesHavePostCardAction);
       
        if (doesHavePostCardAction.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Post Card Action", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawCardAction(postCardAction);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(doesHaveNegativeEffect);
        EditorGUILayout.BeginVertical();

        if (doesHaveNegativeEffect.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Negative Effect", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawCardAction(negativeEffect);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawCardAction(SerializedProperty actionProperty)
    {
        if (actionProperty == null)
            return;

    //     
    // public StatusType statusType;
    // public StatusDurationType durationType;
    // public StatusStartPoint startPoint;
    // public int numberOfTurns;
        
        
        SerializedProperty actionType =
            actionProperty.FindPropertyRelative("actionType");

        SerializedProperty value =
            actionProperty.FindPropertyRelative("value");

        SerializedProperty isAStatusType =
            actionProperty.FindPropertyRelative("isAStatusType");

        SerializedProperty statusType =
            actionProperty.FindPropertyRelative("statusType");

        SerializedProperty durationType =
            actionProperty.FindPropertyRelative("durationType");

        SerializedProperty startPoint =
            actionProperty.FindPropertyRelative("startPoint");

        SerializedProperty numberOfTurns =
            actionProperty.FindPropertyRelative("numberOfTurns");

        EditorGUILayout.PropertyField(actionType);

        EditorGUILayout.PropertyField(value);

        if (actionType.enumValueFlag ==4)
        {
            // EditorGUILayout.PropertyField(isAStatusType);

            // if (isAStatusType.boolValue)
            // {
                EditorGUILayout.PropertyField(statusType);
                EditorGUILayout.PropertyField(durationType);
                EditorGUILayout.PropertyField(startPoint);
                EditorGUILayout.PropertyField(numberOfTurns);
            // }
        }
    }
}
