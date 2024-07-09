using UnityEngine;
using UnityEditor;
using GameCreatingCore.Commands;
using GameCreatingCore.GameActions;

[CustomPropertyDrawer(typeof(PatrolCommand))]
public class PathEditor : PropertyDrawer
{
    string typeText = "Type";
    string positionText = "Position";
    string turnWhileMoveText = "Turn while move";
    string turnSideText = "Turning side";
    string waitTimeText = "Wait time";
    string rotationText = "Rotation";
    string turnSideOnSpotText = "Turn side on spot";
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float res = 0;

        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(typeText));
        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, new GUIContent(positionText));
        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Boolean, new GUIContent(turnWhileMoveText));
        res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(turnSideText));
        if(property.managedReferenceValue != null && property.managedReferenceValue is OnlyWaitCommand)
            res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(waitTimeText));
        if(property.managedReferenceValue != null && property.managedReferenceValue is TurnAndWaitCommand) {
            res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(rotationText));
            res += EditorGUI.GetPropertyHeight(SerializedPropertyType.Enum, new GUIContent(turnSideOnSpotText));
        }

		return res;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        var value = (PatrolCommand)property.managedReferenceValue;
        if(value == null) {
            value = new OnlyWalkCommand(Vector2.zero, false, TurnSideEnum.ShortestPrefereClockwise);
        }
        CommandType type;
        if(value is TurnAndWaitCommand)
            type = CommandType.TurnAndWait;
        else if(value is OnlyWaitCommand)
            type = CommandType.Wait;
        else if(value is OnlyWalkCommand)
            type = CommandType.Walk;
        else{
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
                var waitTime = GetWaitTime(value, position, height4, out _, out _);
                value = new OnlyWaitCommand(commandPos, false, turnWhileMove, turnSide, waitTime);
                break;
            case CommandType.TurnAndWait:
                var rotation = GetRotation(value, position, height4, out position, out var height5);
                var turnSideOnSpot = GetTurnSideOnSpot(value, position, height5, out position, out var height6);
                var waitTime2 = GetWaitTime(value, position, height6, out _, out _);
                value = new TurnAndWaitCommand(commandPos, false, turnWhileMove, turnSide, waitTime2, rotation, turnSideOnSpot);
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
    
    private float GetWaitTime(PatrolCommand value, Rect position, float preHeight, out Rect changedPosition, out float height) {
        float waitTime;
        if(value is OnlyWaitCommand) {
            waitTime = (value as OnlyWaitCommand).WaitTime;
        } else {
            waitTime = 0;
        }
        height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(waitTimeText));
        changedPosition = new Rect(position.xMin, position.yMin + preHeight, position.width, height);
        return EditorGUI.FloatField(changedPosition, waitTimeText, waitTime);
    }

    private float GetRotation(PatrolCommand value, Rect position, float preHeight, out Rect changedPosition, out float height) {
        float rotation;
        if(value is TurnAndWaitCommand) {
            rotation = (value as TurnAndWaitCommand).Rotation;
        } else {
            rotation = 0;
        }
        height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(rotationText));
        changedPosition = new Rect(position.xMin, position.yMin + preHeight, position.width, height);
        return EditorGUI.FloatField(changedPosition, rotationText, rotation);
    }
    
    private TurnSideEnum GetTurnSideOnSpot(PatrolCommand value, Rect position, float preHeight, out Rect changedPosition, out float height) {
        TurnSideEnum turnSide;
        if(value is TurnAndWaitCommand) {
            turnSide = (value as TurnAndWaitCommand).TurnSideOnSpot;
        } else {
            turnSide = TurnSideEnum.ShortestPrefereAntiClockwise;
        }
        height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, new GUIContent(turnSideOnSpotText));
        changedPosition = new Rect(position.xMin, position.yMin + preHeight, position.width, height);
        return (TurnSideEnum)EditorGUI.EnumPopup(changedPosition, turnSideOnSpotText, turnSide);
    }
}

public enum CommandType {
    Walk,
    Wait,
    TurnAndWait
}