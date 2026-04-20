using UnityEngine;

public class DragObject : MonoBehaviour
{
    private Vector3 offset;
    private float zDepth;

    void OnMouseDown()
    {
        zDepth = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPos();
    }

    void OnMouseDrag()
    {
        transform.position = GetMouseWorldPos() + offset;
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zDepth;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}