using UnityEngine;

public class Floor : BaseBuilding
{
    public override bool Rules(Vector3 center, Mesh mesh, Quaternion rotation)
    {
        Collider[] overlapedColliders = Physics.OverlapBox(center, mesh.bounds.extents * 1.1f, rotation);

        if (overlapedColliders.Length > 0)
        {
            foreach (Collider col in overlapedColliders)
            {
                if (col.TryGetComponent(out Roof roof))
                    return false;

                if (col.TryGetComponent(out Floor floor))
                {
                    if (col.bounds.center.y > center.y || col.bounds.center.y < center.y)
                        return false;
                }
            }

            return true;
        }

        return false;
    }

    public override Vector3 Snap(Vector3 mousePosition, Mesh prefabMesh, Quaternion rotation)
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3 result = mousePosition;

        Vector3 dir = (transform.position - mousePosition).normalized;
        float dot = Vector3.Dot(-dir, transform.right);

        // calculate bounds while accounting for rotation and scale 
        float scaledWidth = mesh.bounds.extents.x * transform.lossyScale.x;
        float scaledDepth = mesh.bounds.extents.z * transform.lossyScale.z;

        // snap right
        if (dot >= .7f)
            result = transform.position + transform.right * (scaledWidth * 2f);

        // snap left
        else if (dot <= -.7f)
            result = transform.position - transform.right * (scaledWidth * 2f);

        else
        {
            dot =  Vector3.Dot(-dir, transform.forward);

            // snap front
            if (dot >= .7f)
                result = transform.position + transform.forward * (scaledDepth * 2f);

            // snap back
            else if (dot <= -.7f)
                result = transform.position - transform.forward * (scaledDepth * 2f);

        }
            return result;
    }
}
