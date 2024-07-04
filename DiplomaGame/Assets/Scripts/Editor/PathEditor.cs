using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GameCreatingCore.GamePathing;
using GameCreatingCore;
using GameCreatingCore.GamePathing.GameActions;

[CustomPropertyDrawer(typeof(PatrolCommand))]
public class PathEditor : PropertyDrawer
{
    string typeText = "Type";
    string positionText = "Position";
    string turnWhileMoveText = "Turn while move";
    string turnSideText = "Turning side";
    string waitTimeText = "Wait time";
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float res = 0;

        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(typeText));
        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, new GUIContent(positionText));
        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Boolean, new GUIContent(turnWhileMoveText));
        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(turnSideText));
        if(property.managedReferenceValue != null && property.managedReferenceValue is OnlyWaitCommand)
            res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(waitTimeText));

		return res;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        var value = (PatrolCommand)property.managedReferenceValue;
        if(value == null) {
            value = new OnlyWalkCommand(Vector2.zero, false, GameCreatingCore.GamePathing.GameActions.TurnSideEnum.ShortestPrefereClockwise);
        }
        CommandType type;
        if(value is OnlyWalkCommand)
            type = CommandType.Walk;
        else if(value is OnlyWaitCommand)
            type = CommandType.Wait;
        else {
            throw new System.NotImplementedException();
        }

        EditorGUIUtility.labelWidth = 80;

        float height1 = EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(typeText));
        position = new Rect(position.xMin, position.yMin, position.width, height1);
        type = (CommandType)EditorGUI.EnumPopup(position, typeText, type);
        
        float height2 = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, new GUIContent(positionText));
        position = new Rect(position.xMin, position.yMin + height1, position.width, height2);
        var commandPos = EditorGUI.Vector2Field(position, positionText, value.Position);
        
        float height3 = EditorGUI.GetPropertyHeight(SerializedPropertyType.Boolean, new GUIContent(turnWhileMoveText));
        position = new Rect(position.xMin, position.yMin + height2, position.width, height3);
        var turnWhileMove = EditorGUI.Toggle(position, turnWhileMoveText, value.TurnWhileMoving);
        
        float height4 = EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(turnSideText));
        position = new Rect(position.xMin, position.yMin + height3, position.width, height4);
        var turnSide = (TurnSideEnum)EditorGUI.EnumPopup(position, turnSideText, value.TurningSide);
            
        switch(type) {
            case CommandType.Wait:
                float waitTime;
                if(value is OnlyWaitCommand) {
                    waitTime = (value as OnlyWaitCommand).WaitTime;
                } else {
                    waitTime = 0;
                }
                float height5 = EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(waitTimeText));
                position = new Rect(position.xMin, position.yMin + height4, position.width, height5);
                waitTime = EditorGUI.FloatField(position, waitTimeText, waitTime);
                value = new OnlyWaitCommand(commandPos, false, turnWhileMove, turnSide, waitTime);
                break;
            case CommandType.Walk:
                value = new OnlyWalkCommand(commandPos, turnWhileMove, turnSide);
                break;
            default:
                break;
        }
        property.managedReferenceValue = value;
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
	}
}

public enum CommandType {
    Walk,
    Wait
}