//from https://forum.unity.com/threads/navmeshsettingsobject.145296/

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityObject = UnityEngine.Object;
public sealed class NavMeshSettings
{
#if UNITY_EDITOR
    private SerializedObject serializedObject;
     
    private SerializedProperty _agentRadius;
     
    private SerializedProperty _agentHeight;
     
    private SerializedProperty _agentSlope;
     
    private SerializedProperty _ledgeDropHeight;
     
    private SerializedProperty _agentClimb;
     
    private SerializedProperty _maxJumpAcrossDistance;
     
    private SerializedProperty _minRegionArea;
     
    private SerializedProperty _manualCellSize;
     
    private SerializedProperty _cellSize;
     
    private SerializedProperty _manualTileSize;
     
    private SerializedProperty _tileSize;
     
    private SerializedProperty _accuratePlacement;
     
    public NavMeshSettings(UnityObject navMeshSettingsObject)
    {
        serializedObject = new SerializedObject(navMeshSettingsObject);      
     
        _agentRadius = serializedObject.FindProperty("m_BuildSettings.agentRadius");
        _agentHeight = serializedObject.FindProperty("m_BuildSettings.agentHeight");
        _agentSlope = serializedObject.FindProperty("m_BuildSettings.agentSlope");
        _ledgeDropHeight = serializedObject.FindProperty("m_BuildSettings.ledgeDropHeight");
        _agentClimb = serializedObject.FindProperty("m_BuildSettings.agentClimb");
        _maxJumpAcrossDistance = serializedObject.FindProperty("m_BuildSettings.maxJumpAcrossDistance");
        _minRegionArea = serializedObject.FindProperty("m_BuildSettings.minRegionArea");
        _manualCellSize = serializedObject.FindProperty("m_BuildSettings.manualCellSize");
        _cellSize = serializedObject.FindProperty("m_BuildSettings.cellSize");
        _manualTileSize = serializedObject.FindProperty("m_BuildSettings.manualTileSize");
        _tileSize = serializedObject.FindProperty("m_BuildSettings.tileSize");
        _accuratePlacement = serializedObject.FindProperty("m_BuildSettings.accuratePlacement");
    }
     
    public float agentRadius
    {
        get => _agentRadius.floatValue;
        set
        {
            _agentRadius.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public float agentHeight
    {
        get => _agentHeight.floatValue;
        set
        {
            _agentHeight.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public float agentSlope
    {
        get => _agentSlope.floatValue;
        set
        {
            _agentSlope.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    // "Drop Height"
    public float ledgeDropHeight
    {
        get => _ledgeDropHeight.floatValue;
        set
        {
            _ledgeDropHeight.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    // "Step Height"
    public float agentClimb
    {
        get => _agentClimb.floatValue;
        set
        {
            _agentClimb.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public float maxJumpAcrossDistance
    {
        get => _maxJumpAcrossDistance.floatValue;
        set
        {
            _maxJumpAcrossDistance.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public float minRegionArea
    {
        get => _minRegionArea.floatValue;
        set
        {
            _minRegionArea.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public bool manualCellSize
    {
        get => _manualCellSize.boolValue;
        set
        {
            _manualCellSize.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public float cellSize
    {
        get => _cellSize.floatValue;
        set
        {
            _cellSize.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public bool manualTileSize
    {
        get => _manualTileSize.boolValue;
        set
        {
            _manualTileSize.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
     
    public int tileSize
    {
        get => _tileSize.intValue;
        set
        {
            _tileSize.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }  
     
    // "Height Mesh"
    public bool accuratePlacement
    {
        get => _accuratePlacement.boolValue;
        set
        {
            _accuratePlacement.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
#endif
}
