using UnityEngine;

public interface IBuildingModule
{
    public bool Rules(Vector3 center, Mesh mesh, Quaternion rotation);
    public Vector3 Snap(Vector3 mousePos, Mesh prefabMesh, Quaternion rotation);
}