using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private float movementSpeed = 10;
    [SerializeField]
    private float keysMovementSpeedModifier = 1;
    [SerializeField]
    private float mouseMovementSpeedModifier = 1;
    [Range(0, 0.4f)]
    [SerializeField]
    private float mouseMarginToMove = 0.05f;
    [SerializeField]
    private string verticalAxis = "CameraVertical";
    [SerializeField]
    private string horizontalAxis = "CameraHorizontal";
    [SerializeField]
    private bool doNotMoveInWindowedMode = true;

    public void SetBounds(Rect bounds) => this.bounds = bounds;
    private Rect bounds;

    // even though object moves during this update, it is supposed to only be the camera.
    // in other words no collider and only important for visualization. - no need for fixed update
    void Update()
    {
        target.position += Vector3.right * Input.GetAxis(horizontalAxis) * movementSpeed * keysMovementSpeedModifier * Time.deltaTime;
        target.position += Vector3.up * Input.GetAxis(verticalAxis) * movementSpeed * keysMovementSpeedModifier * Time.deltaTime;


        MouseAffect(
            Input.mousePosition.x,
            doNotMoveInWindowedMode ? 0 : float.MinValue,
            Screen.width * mouseMarginToMove, Vector3.left);
        MouseAffect(
            Input.mousePosition.x, Screen.width * (1 - mouseMarginToMove),
            doNotMoveInWindowedMode ? Screen.width : float.MaxValue, 
            Vector3.right);
        
        MouseAffect(Input.mousePosition.y, 
            doNotMoveInWindowedMode ? 0 : float.MinValue,
            Screen.height * mouseMarginToMove, Vector3.down);
        MouseAffect(Input.mousePosition.y, Screen.height * (1 - mouseMarginToMove), 
            doNotMoveInWindowedMode ? Screen.height : float.MaxValue, 
            Vector3.up);

        target.position = new Vector3(
            Mathf.Clamp(target.position.x, bounds.x, bounds.x + bounds.width),
            Mathf.Clamp(target.position.y, bounds.y, bounds.y + bounds.height),
            target.position.z);
    }

    void MouseAffect(float mousePos, float min, float max, Vector3 direction) {
        if(mousePos >= min && mousePos <= max) {
            target.position += direction * movementSpeed * mouseMovementSpeedModifier * Time.deltaTime;
        }
    }
}
