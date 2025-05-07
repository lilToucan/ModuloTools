using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuilderTool : EditorWindow
{

    
    public Material previewMaterial;

    SerializedObject so;
    
    SerializedProperty previewMaterialProp;

    List<GameObject> walls = new(), floors = new(), roofs = new();

    Vector3 mousePos;
    Vector3 snappedPos;
    Vector2 scrollPos;

    GameObject prefabGameObj, chosenPrefab;

    IBuildingModule buildingModuleChosen;

    Mesh prefabMesh;

    bool smallerCollision = false;


    Quaternion prefabRotation;
    bool canPlace;

    Camera cam;

    [MenuItem("Tools/Builder Tool")]
    public static void OpenWindow() => GetWindow<BuilderTool>("Builder Tool");

    private void OnEnable()
    {
        prefabRotation = Quaternion.identity;
        so = new(this);
        
        previewMaterialProp = so.FindProperty("previewMaterial");

        walls = GetPrefabs("Walls");
        floors = GetPrefabs("Floors");
        roofs = GetPrefabs("Roofs");

        SceneView.duringSceneGui += DurinSceneGui;

        

        if (EditorPrefs.HasKey("previewMaterial"))
            previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(EditorPrefs.GetString("previewMaterial"));

    }

    private void OnDisable()
    {
        EditorPrefs.SetString("previewMaterial", AssetDatabase.GetAssetPath(previewMaterial));
        SceneView.duringSceneGui -= DurinSceneGui;
    }

    private void DurinSceneGui(SceneView view)
    {
        cam = view.camera;
        RaycastHit hit;

        if (prefabMesh != null)
        {
            // change mouse position (tho only if you are not rotateing the prefab)
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out hit) && !RotateingPrefab())
                mousePos = hit.point;

            //drawing the mesh
            previewMaterial.SetPass(0);

            // set canPlace to true and the color to green but then if there's a collision canPlace turns false and the color is set to red
            previewMaterial.SetColor("_BaseColor", Color.green);
            canPlace = true;

            smallerCollision = false;

            // calculate the center of the mesh drawn acounting for the rotation
            Vector3 prefabCenter = mousePos + prefabRotation * prefabMesh.bounds.center;
            Handles.matrix = Matrix4x4.TRS(prefabCenter, prefabRotation, Vector3.one);

            smallerCollision = SnapCollisionCheck(prefabCenter);
            Graphics.DrawMeshNow(prefabMesh, mousePos, prefabRotation);

            CollisionCheck(mousePos + prefabRotation * prefabMesh.bounds.center);

            if (!buildingModuleChosen.Rules(prefabCenter, prefabMesh, prefabRotation))
            {
                canPlace = false;
                previewMaterial.SetColor("_BaseColor", Color.red);
            }

            Placement();

        }
    }

    private void Placement()
    {
        // checks if you pressed the right button
        if (Event.current.keyCode == KeyCode.Mouse1)
        {
            // checks if you are alowed to place down the object
            if (!canPlace)
            {
                Debug.LogError("you can't place an object while it's red");
                return;
            }

            // instantiate the prefab and gives it the desired location and rotation
            prefabGameObj = PrefabUtility.InstantiatePrefab(chosenPrefab) as GameObject;
            prefabGameObj.transform.position = mousePos;
            prefabGameObj.transform.rotation = prefabRotation;

            // registers it to the undo history
            Undo.RegisterCreatedObjectUndo(prefabGameObj, $"spawned {prefabGameObj.name}");
            ResetPrefab();
        }
    }

    private void ResetPrefab()
    {
        // reset parameters
        chosenPrefab = null;
        prefabMesh = null;
        prefabGameObj = null;
    }

    private void CollisionCheck(Vector3 prefabCenter)
    {
        float collisionMult = 1;
        if (smallerCollision)
            collisionMult = .89f;

        // check for collison with an overlap box around the mesh
        Collider[] overlapedColliders = Physics.OverlapBox(prefabCenter, prefabMesh.bounds.extents * collisionMult, prefabRotation);

        // draw said overlap box
        Handles.DrawWireCube(Vector3.zero, prefabMesh.bounds.extents * (2 * collisionMult));

        if (overlapedColliders.Length > 0)
        {
            int colliders = overlapedColliders.Length;
            // ignore the collider of the pavement
            foreach (Collider col in overlapedColliders)
            {
                if (!col.TryGetComponent(out IBuildingModule i))
                {
                    colliders--;
                }
            }

            if (colliders > 0)
            {
                canPlace = false;
                previewMaterial.SetColor("_BaseColor", Color.red);
            }

        }
    }

    private bool SnapCollisionCheck(Vector3 prefabCenter)
    {
        if (!Event.current.alt)
            return false;

        Collider[] overlapedBuildings = Physics.OverlapBox(prefabCenter, prefabMesh.bounds.extents * 1.6f, prefabRotation);
        Handles.color = Color.blue;
        Handles.DrawWireCube(Vector3.zero, prefabMesh.bounds.extents * 3.2f);

        float distance = Mathf.Infinity;
        Collider nearestCollider = null;

        foreach (Collider col in overlapedBuildings)
        {
            float dist = Vector3.Distance(col.transform.position, mousePos);
            if (dist < distance)
            {
                distance = dist;
                nearestCollider = col;
            }

        }

        if (nearestCollider.TryGetComponent(out IBuildingModule buildingModule))
        {
            snappedPos = buildingModule.Snap(mousePos, prefabMesh, prefabRotation);
            Vector3 dir = (prefabCenter - mousePos).normalized;
            //if (dir != Vector3.zero)
            //{
            //    Vector3 forward = prefabRotation.normalized * Vector3.forward;
            //    Vector3 right = prefabRotation.normalized * Vector3.right;
            //    Vector3 up = prefabRotation.normalized * Vector3.up;

            //    float dotForward = Vector3.Dot(dir, forward);
            //    float dotRight = Vector3.Dot(dir, right);
            //    float dotUp = Vector3.Dot(dir, up);

            //    if (dotForward >= .7f || dotForward <= .7f)
            //        dir *= prefabMesh.bounds.extents.z;

            //}
            mousePos = snappedPos + dir;

            return true;
        }
        return false;
    }

    private bool RotateingPrefab()
    {
        Event e = Event.current;
        bool holdingKey = e.control;

        if (holdingKey)
        {
            prefabRotation = Handles.RotationHandle(prefabRotation, mousePos);
            return true;
        }
        return false;
    }

    private void OnGUI()
    {
        so.Update();
        
        EditorGUILayout.PropertyField(previewMaterialProp);
        so.ApplyModifiedProperties();
        
        if (GUILayout.Button("ResetRotation"))
        {
            prefabRotation = Quaternion.identity;
        }

        var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPos);
        scrollPos = scrollViewScope.scrollPosition;


        using (scrollViewScope)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Walls");
                    ChoosePrefabs(walls);

                    GUILayout.Space(10);

                    GUILayout.Label("Floors");
                    ChoosePrefabs(floors);

                    GUILayout.Space(10);

                    GUILayout.Label("Roof");
                    ChoosePrefabs(roofs);

                }
                GUILayout.FlexibleSpace();
            }

        }

    }

    private void ChoosePrefabs(List<GameObject> _Prefabs)
    {
        foreach (GameObject prefab in _Prefabs)
        {
            Texture sprite;
            sprite = AssetPreview.GetAssetPreview(prefab);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Box(sprite, GUILayout.Height(80), GUILayout.Width(100));

                if (GUILayout.Button("Spawn", GUILayout.Height(80), GUILayout.Width(300)))
                {
                    prefabMesh = prefab.GetComponent<MeshFilter>().sharedMesh;
                    chosenPrefab = prefab;
                    if (!prefab.TryGetComponent(out buildingModuleChosen))
                    {
                        Debug.LogError("this prefab is not a building block plese add one of the scripts to this prefab or remove it from the folder", prefab);
                        chosenPrefab = null;
                        prefabMesh = null;
                    }
                }

            }

        }

        Repaint();
    }

    private List<GameObject> GetPrefabs(string _searchQuery = "t:Prefab", string _filesPath = "Assets/prefabs")
    {
        string[] prefabs = AssetDatabase.FindAssets(_searchQuery, new string[] { _filesPath });
        List<GameObject> list = new();
        foreach (string p in prefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(p);
            list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }


        return list;
    }

    private List<GameObject> GetPrefabs(string pathAfterAssetPrefab)
    {
        return GetPrefabs("t:Prefab", "Assets/prefabs" + "/" + pathAfterAssetPrefab);
    }
}