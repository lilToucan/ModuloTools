using UnityEngine;

public class Wall : BaseBuilding
{
    public override bool Rules(Vector3 center, Mesh mesh, Quaternion rotation)
    {
        Collider[] overlapedColliders = Physics.OverlapBox(center, mesh.bounds.extents * 2.1f, rotation);

        if (overlapedColliders.Length > 0)
            return true;

        return false;

    }

    public override Vector3 Snap(Vector3 position)
    {
        return base.Snap(position);
        
        
    }
}
