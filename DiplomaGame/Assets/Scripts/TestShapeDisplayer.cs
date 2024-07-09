using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestShapeDisplayer : MonoBehaviour
{
    [SerializeField]
    List<Vector2> shape;
    [SerializeField]
    Color firstPointColor = Color.black;
    [SerializeField]
    Color pointsColor = Color.black;
    [SerializeField]
    Color linesColor = Color.white;

    [TextArea(1, 20)]
    [SerializeField]
    string inputString;

    [ContextMenu("Set From Input String")]
    public void FromString() {
        string value = inputString;
        if(value.StartsWith('\"')){
            value = value[1..];
        }
        if(value.StartsWith('(')){
            value = value[1..];
        }
        if(value.EndsWith('\"')){
            value = value[..^1];
        }
        if(value.EndsWith(')')){
            value = value[..^1];
        }
        var lines = value.Split("), (");
        var list = new List<Vector2>();
        foreach(var x in lines) {
            var both = x.Split(", ");
            list.Add(new Vector2(float.Parse(both[0].Replace('.', ',')), 
                float.Parse(both[1].Replace('.', ','))));
        }
        shape = list;
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = linesColor;
        var pre = shape.Last();
        foreach(var v in shape) {
            Gizmos.DrawLine(v, pre);
            pre = v;
        }

        Gizmos.color = firstPointColor;
        Gizmos.DrawSphere(shape.First(), 0.2f);
        Gizmos.color = pointsColor;
        foreach(var v in shape.Skip(1)) {
            Gizmos.DrawSphere(v, 0.2f);
        }
    }
}
