using GameCreatingCore.GameActions;
using UnityEngine;

[System.Serializable]
public class UnitySkillRepr {
	public SkillType type;
	public float nonCancelTime = 0.8f;
	public float maxUseDistance = 3;

	public IActiveGameActionProvider GetActionProvider() {
		switch(type) {
			case SkillType.Kill:
				return new KillActionProvider(nonCancelTime, maxUseDistance);
			default:
				throw new System.NotImplementedException($"Value {type} of enum {nameof(SkillType)} is not " +
					$"implemented in {nameof(UnitySkillRepr)}.");
		}
	}

	public static UnitySkillRepr FromProvider(IActiveGameActionProvider provider) {
        if(provider is KillActionProvider) {
            var kill = (KillActionProvider)provider;
            return  new UnitySkillRepr() {
                    maxUseDistance = kill.MaxUseDistance,
                    nonCancelTime = kill.KillingTime,
                    type = SkillType.Kill
            };
        } else {
            throw new System.Exception($"Skill of type {provider.GetType().Name} has no unity saving implemented.");
        }
	}
}

public enum SkillType {
	Kill
}

[System.Serializable]
public class UnitySkillReprGrounded {
	public UnitySkillRepr unitySkillRepr;
	public Vector2 groundedAt;
}