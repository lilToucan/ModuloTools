using UnityEditor;
using UnityEngine;

public class Wall : BaseBuilding
{
    public override bool Rules(Vector3 center, Mesh mesh, Quaternion rotation)
    {
        Collider[] overlapedColliders = Physics.OverlapBox(center, mesh.bounds.extents * 1.1f, rotation);

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

    public override Vector3 Snap(Vector3 centerPos, Mesh prefabMesh, Quaternion rotation)
    {
        Collider col = GetComponent<Collider>();
        float posX;
        float posY;
        float posZ;
        float distance = float.MaxValue;
        Vector3 closestPos = centerPos;
        Vector3 pos;

        int max = 2;
        int min = -2;


        for (int x = min; x <= max; x++)
        {
            posX = col.bounds.center.x + (col.bounds.size.x / 3) * x;

            for (int y = min; y <= max; y++)
            {
                posY = col.bounds.center.y + (col.bounds.size.y / 3) * y;

                for (int z = min; z <= max; z++)
                {
                    if (x != min && x != max && y != min && y != max && z != max && z != min)
                        continue;

                    posZ = col.bounds.center.z + (col.bounds.size.z / 3) * z;
                    pos = new(posX, posY, posZ);

                    float dist = Vector3.Distance(pos, centerPos);

                    if (dist < distance)
                    {
                        distance = dist;
                        closestPos = pos;
                    }

                }
            }
        }

        //pos = new(posX, posY, posZ);
        return closestPos;
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

                    
                    

                    posZ = col.bounds.center.z + (col.bounds.size.z / 3.5f) * z * 2;
                    pos = new(posX, posY, posZ);

                    Gizmos.DrawWireSphere(pos, .01f);
                }
            }
        }
    }
#endif
}
