using System.Collections.Generic;
using UnityEngine;

public interface IBuildingModule
{
    public bool Rules(Vector3 center, Quaternion rotation);
    public (Vector3,Vector3) Snap(List<Vector3> snapGrid, Vector3 mousePos);
}