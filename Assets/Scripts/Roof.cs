using UnityEngine;

public class Roof : BaseBuilding
{
    public override bool Rules(Vector3 center, Mesh mesh, Quaternion rotation)
    {
        Collider[] overlapedColliders = Physics.OverlapBox(center, mesh.bounds.extents * 1.1f, rotation);

        if (overlapedColliders.Length > 0)
        {
            foreach (Collider col in overlapedColliders)
            {
                if (col.TryGetComponent(out Wall wall))
                {
                    if (col.bounds.center.y < center.y)
                        return true;
                }
            }

        }

        return false;
    }

    public override Vector3 Snap(Vector3 mousePosition, Mesh prefabMesh, Quaternion rotation)
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3 result = mousePosition;

        Vector3 dir = (transform.position - mousePosition).normalized;
        float dot = Vector3.Dot(-dir, transform.up);

        // calculate bounds while accounting for rotation and scale 
        float scaledHeight = mesh.bounds.max.y * transform.lossyScale.y;
        float scaledWidth = mesh.bounds.extents.x * transform.lossyScale.x;

        // snap Up
        if (dot <= -.7f)
        {
            result = transform.position - transform.up * scaledHeight;
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
