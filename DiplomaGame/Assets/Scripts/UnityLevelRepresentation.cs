using GameCreatingCore.LevelRepresentationData;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnityLevelRepresentation : ScriptableObject
{
	[SerializeField]
	public List<UnityObstacle> Obstacles;
	/// <summary>
	/// The Outer walls of the environment.
	/// </summary>
	[SerializeField]
	public UnityObstacle OuterObstacle;
	[SerializeField]
	public List<Enemy> Enemies;
	[SerializeField]
	public List<UnitySkillRepr> AvailableSkills;
	[SerializeField]
	public List<UnitySkillReprGrounded> SkillsToPickup;
	[SerializeField]
	public Vector2 FriendlyStartPos;
	[SerializeField]
	public LevelGoal Goal;

	public LevelRepresentation GetLevelRepresentation() {
		return new LevelRepresentation(
			Obstacles.Select(o => o.ToObstacle()).ToList(),
			OuterObstacle.ToObstacle(),
			Enemies,
			SkillsToPickup?.Select(x => new PickupableActionProvider(x.unitySkillRepr.GetActionProvider(), x.groundedAt)).ToList(),
			AvailableSkills?.Select(x => x.GetActionProvider()).ToList(),
			FriendlyStartPos,
			Goal);
	}
}