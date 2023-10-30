using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreationCore;

public abstract class LevelCreatorProvider : MonoBehaviour
{
    public abstract ILevelCreator GetLevelCreator();
}
