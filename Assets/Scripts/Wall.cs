using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Wall : BaseBuilding
{
    
    public override bool Rules(Vector3 center, Mesh prefabMesh, Quaternion rotation)
    {
        Collider[] overlapedColliders = Physics.OverlapBox(center, prefabMesh.bounds.extents * 1.1f, rotation);

        if (overlapedColliders.Length > 0)
        {
            foreach (Collider col in overlapedColliders)
            {
                if (col.TryGetComponent(out Roof roof))
                {
                    if (center.y > col.bounds.max.y)
                        return false;
                }
            }

            return true;
        }

        return false;
    }

    public override (Vector3, Vector3) Snap(List<Vector3> snapGrid, Vector3 mousePos)
    {

       return base.Snap(snapGrid, mousePos);
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        float posX;
        float posY;
        float posZ;
        Vector3 pos;

        int min = -1;
        int max = 1;
        for (int x = min; x <= max; x++)
        {
            posX = col.bounds.center.x + (col.bounds.size.x / 4) * x * 2;

            for (int y = min; y <= max; y++)
            {
                posY = col.bounds.center.y + (col.bounds.size.y / 4) * y * 2;

                for (int z = min; z <= max; z++)
                {
                    if (z == 0 && y == 0 && x == 0)
                        continue;
                    if (z != 0 && (y != 0 || x != 0))
                        continue;

                    posZ = col.bounds.center.z + (col.bounds.size.z / 3.5f) * z * 2;
                    pos = new(posX, posY, posZ);

                    Gizmos.DrawWireSphere(pos, .01f);
                }
            }
        }

        if (closestPos == Vector3.zero && closestGirdPoint == Vector3.zero)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(closestPos, .1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(closestGirdPoint, .1f);
    }
#endif
}
