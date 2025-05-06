using UnityEngine;

public class BaseBuilding : MonoBehaviour, IBuildingModule
{
    protected Mesh mesh;

    private void OnEnable()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    public virtual bool Rules(Vector3 center, Mesh mesh, Quaternion rotation)
    {
        return false;
    }

    public virtual Vector3 Snap(Vector3 mousePosition)
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3 result = mousePosition;

        Vector3 dir = (transform.position - mousePosition).normalized;
        float dot = Vector3.Dot(-dir, transform.up);

        // calculate bounds while accounting for rotation and scale 
        float scaledHeight = mesh.bounds.max.y * transform.lossyScale.y;
        float scaledWidth = mesh.bounds.extents.x * transform.lossyScale.x;

        // snap Up
        if (dot >= .7f)
        {
            result = transform.position + transform.up * scaledHeight;
        }
        else
        {
            dot = Vector3.Dot(-dir, transform.right);

            // snap right
            if (dot >= .7f)
                result = transform.position + transform.right * (scaledWidth * 2f);
            
            // snap left
            else if (dot <= -.7f)
                result = transform.position - transform.right * (scaledWidth * 2f);
        }

        return result;
    }
}