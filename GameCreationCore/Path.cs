using UnityEngine;

namespace GameCreationCore;

public record Path(
	bool Cyclic,
	List<PatrolCommand> Commands
);