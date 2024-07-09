using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewconesManager : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private bool allowMoreDisplayed = false;

    public List<EnemyObject> Enemies { get; } = new List<EnemyObject>();

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1)) {
            var hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if(hit.collider != null){
                var e = hit.collider.GetComponent<EnemyObject>();
                ShowOrHideEnemyViewcone(e);
            }
        }
    }

	public void ShowEnemyViewcone(EnemyObject e) {
        if(e == null && e.killer.Dead)
            return;
        if(!allowMoreDisplayed)
            foreach(var enemy in Enemies) {
                enemy.viewcone.displayViewcone = false;
            }
        e.viewcone.displayViewcone = true;
		
	}
    
	public void ShowOrHideEnemyViewcone(EnemyObject e) {
        if(e == null || e.killer.Dead)
            return;
        bool curr = e.viewcone.displayViewcone;
        if(!allowMoreDisplayed)
            foreach(var enemy in Enemies) {
                enemy.viewcone.displayViewcone = false;
            }
        e.viewcone.displayViewcone = !curr;
	}
}
