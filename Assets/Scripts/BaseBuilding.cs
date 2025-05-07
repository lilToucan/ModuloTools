using UnityEngine;

public class BaseBuilding : MonoBehaviour, IBuildingModule
{
    [HideInInspector] public Mesh mesh;

    private void OnEnable()
    {
        // idk why this doesn't work so i had to move it in the snap function
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    public virtual bool Rules(Vector3 center, Mesh mesh, Quaternion rotation)
    {
        return false;
    }

    public virtual Vector3 Snap(Vector3 mousePosition, Mesh prefabMesh, Quaternion rotation)
    {
        return mousePosition;
    }
}