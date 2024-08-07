using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Camera cameraToMove;
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
        cameraToMove.transform.position += Vector3.right * Input.GetAxis(horizontalAxis) * movementSpeed * keysMovementSpeedModifier * Time.deltaTime;
        cameraToMove.transform.position += Vector3.up * Input.GetAxis(verticalAxis) * movementSpeed * keysMovementSpeedModifier * Time.deltaTime;


        MouseAffect(
            Input.mousePosition.x,
            doNotMoveInWindowedMode ? 0 : float.MinValue,
            Screen.width * mouseMarginToMove, Vector3.left,
            false);
        MouseAffect(
            Input.mousePosition.x, Screen.width * (1 - mouseMarginToMove),
            doNotMoveInWindowedMode ? Screen.width : float.MaxValue, 
            Vector3.right,
            true);
        
        MouseAffect(Input.mousePosition.y, 
            doNotMoveInWindowedMode ? 0 : float.MinValue,
            Screen.height * mouseMarginToMove, Vector3.down,
            false);
        MouseAffect(Input.mousePosition.y, Screen.height * (1 - mouseMarginToMove), 
            doNotMoveInWindowedMode ? Screen.height : float.MaxValue, 
            Vector3.up,
            true);

        cameraToMove.transform.position = new Vector3(
            Mathf.Clamp(cameraToMove.transform.position.x, bounds.x, bounds.x + bounds.width),
            Mathf.Clamp(cameraToMove.transform.position.y, bounds.y, bounds.y + bounds.height),
            cameraToMove.transform.position.z);
    }

    void MouseAffect(float mousePos, float min, float max, Vector3 direction, bool endOnMax) {
        if(mousePos >= min && mousePos <= max) {
            var val = (mousePos - min) / (max - min);
            if(!endOnMax) {
                val = 1 - val;
            }
            val *= 1.5f;
            if(val > 1)
                val = 1;
            else
                //val = 1 - (1 - val) * (1 - val);
                val = val * val;
            cameraToMove.transform.position += direction * movementSpeed * mouseMovementSpeedModifier * Time.deltaTime * val;
        }
    }
}
