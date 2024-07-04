using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class HumanPlayerController : MonoBehaviour
{
    [SerializeField] 
    private Camera cam;

    public UnityEvent<Vector2?> OnDestinationChanged;


    [SerializeField]
    private string EnemyLayer;


    public Collider2D AttackingEnemy { get; private set; } = null;


	private void Start() {
		if (cam == null) {
            Debug.LogWarning($"Player controller camera defaulting to {nameof(Camera)}.{nameof(Camera.main)}");
            cam = Camera.main;
        }
	}

    private float lastTime = -5000;
    private Vector3 lastHit;

	// Update is called once per frame
	void Update()
    {
        var left = Input.GetMouseButton(0);
        var right = Input.GetMouseButton(1);
        if(left || right) {
            var hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if(hit.collider != null){
                if(left) {
                    OnDestinationChanged.Invoke(hit.point);
                    if(hit.collider.gameObject.layer == LayerMask.NameToLayer(EnemyLayer)) {
                        AttackingEnemy = hit.collider;
                    } else {
                        AttackingEnemy = null;
                    }
                } else {
                    if(hit.collider.gameObject.layer != LayerMask.NameToLayer(EnemyLayer)) { 
                        OnDestinationChanged.Invoke(null);
                    }
                }
            }
        }
    }
}
