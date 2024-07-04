using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyKiller : MonoBehaviour
{
    [DoNotSerialize]
    public Camera camera;
    [SerializeField] private SpriteRenderer outline;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer[] accesories;
    [SerializeField] private ViewconeCreator viewCone;
    [SerializeField] private float viewconeDieTime = 0.3f;

    [SerializeField] private Collider2D collider;

    [SerializeField] private Color hoverColor = Color.gray;
    [SerializeField] private Color pressColor = Color.red;
    [SerializeField] private Color deadBodyColor = Color.gray;

    public bool Dead { get; private set; } = false;

    public bool CanBeKilled { get; set; } = true;

    // Update is called once per frame
    void Update()
    {
        if(Dead || !CanBeKilled) {
            outline.enabled = false;
            return;
        }
        var hit = Physics2D.Raycast(camera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if(hit.collider == collider) {
            outline.enabled = true;
            if(Input.GetMouseButton(0)) {
                outline.color = pressColor;
            } else {
                outline.color = hoverColor;
            }
        } else {
            outline.enabled = false;
        }
        
    }

    public void KillHim() {
        Dead = true;
        outline.enabled = false;
        body.color = deadBodyColor;
        foreach(var a in accesories) {
            Destroy(a);
            //a.color = new Color(a.color.r, a.color.g, a.color.b, a.color.a * 0.3f);
        }
        StartCoroutine(ViewconeDieCoroutine());
    }

    IEnumerator ViewconeDieCoroutine() {
        var sub = viewCone.viewLength / viewconeDieTime;
        while(true) {
            viewCone.viewLength -= sub * Time.deltaTime;
            if(viewCone.viewLength < 0 ) {
                viewCone.viewLength = 0;
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        viewCone.displayViewcone = false;
    } 
}
