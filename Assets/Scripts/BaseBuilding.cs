using System.Collections.Generic;
using UnityEngine;

public class BaseBuilding : MonoBehaviour, IBuildingModule
{
    [HideInInspector] public Mesh mesh;
    protected Vector3 closestPos;
    protected Vector3 closestGirdPoint;
    private void OnEnable()
    {
        // idk why this doesn't work so i had to move it in the snap function
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    public virtual bool Rules(Vector3 center, Mesh prefabMesh, Quaternion rotation)
    {
        return false;
    }

    public virtual (Vector3, Vector3) Snap(List<Vector3> snapGrid, Vector3 mousePosition)
    {
        Collider col = GetComponent<Collider>();
        float posX;
        float posY;
        float posZ;
        float distance = float.MaxValue;

        closestPos = mousePosition;
        closestGirdPoint = snapGrid[0];
        Vector3 pos;
        List<Vector3> posGird = new();

        int max = 1;
        int min = -1;

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


                    posGird.Add(pos);
                }
            }
        }

        foreach (var gridPos in posGird)
        {
            foreach (var snapPoint in snapGrid)
            {
                float dist = Vector3.Distance(gridPos, snapPoint);
                if (dist < distance)
                {
                    distance = dist;
                    closestPos = gridPos;
                    closestGirdPoint = snapPoint;
                }

            }
        }

        //pos = new(posX, posY, posZ);
        return (closestPos, closestGirdPoint);
    }
}