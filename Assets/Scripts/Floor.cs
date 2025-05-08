using System.Collections.Generic;
using UnityEngine;

public class Floor : BaseBuilding
{
    public override bool Rules(Vector3 center, Quaternion rotation)
    {
        Collider[] overlapedColliders = Physics.OverlapBox(center, mesh.bounds.extents * 1.1f, rotation);

        if (overlapedColliders.Length > 0)
        {
            foreach (Collider col in overlapedColliders)
            {
                if (col.TryGetComponent(out Roof roof))
                    return false;
            }

            
            return true;
        }

        return false;
    }

    public override (Vector3, Vector3) Snap(List<Vector3> snapGrid, Vector3 mousePosition)
    {
        return base.Snap(snapGrid, mousePosition);
    }
}
