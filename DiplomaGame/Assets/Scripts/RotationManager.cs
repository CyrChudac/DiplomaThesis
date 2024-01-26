using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
//using Unity.PlasticSCM.Editor.WebApi;

public class RotationManager : MonoBehaviour
{
	[SerializeField]
	private GameObject agent;

	[SerializeField]
	private float angleCorrection = -90;

	[Min(0)]
	public float angleDeviation = 10;
	
	[Min(0.05f)]
	public float deviationTime = 1;

	[Tooltip("How many seconds to wait after rotating before starting deviation")]
	[Min(0)]
	public float beforeDeviationSleep = 0;

	[Tooltip("How many seconds to rotate full 360.")]
	[Min(0.05f)]
	public float rotationSpeed = 0.6f;

	private Sequence rotationTween = null;

	public virtual void LookAt(Vector3 point) {
		var relative = point - agent.transform.position;
		float angle = Mathf.Atan2(relative.x, relative.y) * Mathf.Rad2Deg + angleCorrection;
		angle = Mod(angle, 360);
		var curr = agent.transform.rotation.eulerAngles.z;
		curr = Mod(curr, 360);
		agent.transform.rotation = Quaternion.Euler(0, 0, curr);
		var change = Mathf.Abs(angle - curr);

		Vector3 finalRot;
		float time;
		if(change < 360 - change || (change == 180 && angle < curr)) {
			finalRot = new Vector3(0, 0, angle);
			time = rotationSpeed * change / 360;
		}else {
			time = rotationSpeed * (360 - change) / 360;
			if(angle > curr) {
				finalRot = new Vector3(0, 0, angle - 360);
			} else {
				finalRot = new Vector3(0, 0, angle + 360);
			}
		}

		rotationTween?.Kill();
		rotationTween = DOTween.Sequence()
			.Append(agent.transform.DORotate(finalRot, time))
			.AppendInterval(beforeDeviationSleep)
			.OnComplete(() => Deviation());

	}

	void Deviation() {
		var curr = agent.transform.rotation.eulerAngles.z; 
		void Deviation1() {
			rotationTween = DOTween.Sequence()
				.Append(agent.transform.DORotate(new Vector3(0, 0, curr + angleDeviation), deviationTime / 2))
				.Append(agent.transform.DORotate(new Vector3(0, 0, curr), deviationTime / 2))
				.OnComplete(() => Deviation2());
		}
		void Deviation2() {
			rotationTween = DOTween.Sequence()
				.Append(agent.transform.DORotate(new Vector3(0, 0, curr - angleDeviation), deviationTime / 2))
				.Append(agent.transform.DORotate(new Vector3(0, 0, curr), deviationTime / 2))
				.OnComplete(() => Deviation1());
		}
		Deviation1();
	}

	float Mod(float x, int m) {
		return (x%m + m)%m;
	}

	public virtual void Stop() {

	}

}