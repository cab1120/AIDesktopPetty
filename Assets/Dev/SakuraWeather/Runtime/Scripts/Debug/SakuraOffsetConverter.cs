using UnityEngine;

public class SakuraOffsetConverter : MonoBehaviour
{
    [SerializeField]
    private Transform targetCamera;

    [ContextMenu("Print Current Sakura Offsets")]
    private void PrintOffsets()
    {
        PrintOffset(
            "Background",
            new Vector3(0f, 0, 10f));

        PrintOffset(
            "Midground",
            new Vector3(0f, 2f, 10f));

        PrintOffset(
            "Foreground",
            new Vector3(-4f, -2f, 1.5f));
    }

    private void PrintOffset(
        string layerName,
        Vector3 worldCenter)
    {
        Vector3 localOffset =
            targetCamera.InverseTransformPoint(worldCenter);

        Debug.Log(
            $"{layerName} Local Offset = {localOffset}");
    }
}