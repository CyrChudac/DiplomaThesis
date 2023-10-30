using UnityEngine;

namespace GameCreationCore; 

public abstract record PatrolCommand(
	Vector2 Position,
	bool ExecuteDuringMoving
) {
	public abstract void StartExecution();
	public abstract bool ExecutionFinished { get; }
};
