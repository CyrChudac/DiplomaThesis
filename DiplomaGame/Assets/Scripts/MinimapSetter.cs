using UnityEngine;

public class MinimapSetter : MonoBehaviour
{
    [SerializeField] private LevelInitializor initializer;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RenderTexture minimapTexture;
    [SerializeField] private RectTransform targetImage;

    void Start()
    {
        var level = initializer.currentLevel;
        var aspect = level.OuterObstacle.BoundingBox.width / level.OuterObstacle.BoundingBox.height;
        minimapCamera.aspect = aspect;
        minimapCamera.orthographicSize = level.OuterObstacle.BoundingBox.height / 2;
        minimapCamera.transform.position = new Vector3(
            level.OuterObstacle.BoundingBox.center.x,
            level.OuterObstacle.BoundingBox.center.y,
            minimapCamera.transform.position.z
            );

        minimapTexture.width = (int)(minimapTexture.height * aspect);
        if(aspect < 1)
            targetImage.localScale = new Vector3(targetImage.localScale.y * aspect, targetImage.localScale.y, targetImage.localScale.z);
        else
            targetImage.localScale = new Vector3(targetImage.localScale.x, targetImage.localScale.x / aspect, targetImage.localScale.z);
    }
}
