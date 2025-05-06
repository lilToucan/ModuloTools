using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuilderTool : EditorWindow
{

    public LayerMask layerMask;
    public Material previewMaterial;

    SerializedObject so;
    SerializedProperty layerMaskProp;
    SerializedProperty previewMaterialProp;

    List<GameObject> walls = new(), floors = new(), roofs = new();

    Vector3 mousePos;
    Vector3 snappedPos;
    Vector2 scrollPos;

    GameObject prefabGameObj, chosenPrefab;

    IBuildingModule buildingModuleChosen;

    Mesh prefabMesh;



    Quaternion prefabRotation;
    bool canPlace;

    [MenuItem("Tools/Builder Tool")]
    public static void OpenWindow() => GetWindow<BuilderTool>("Builder Tool");

    private void OnEnable()
    {
        prefabRotation = Quaternion.identity;
        so = new(this);
        layerMaskProp = so.FindProperty("layerMask");
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
        RaycastHit hit;

        if (prefabMesh != null)
        {
            // change mouse position (tho only if you are not rotateing the prefab)
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out hit) && !RotateingPrefab())
                mousePos = hit.point;

            //drawing the mesh
            previewMaterial.SetPass(0);

            // calculate the center of the mesh drawn 
            Vector3 prefabCenter = mousePos + prefabRotation * prefabMesh.bounds.center;
            Handles.matrix = Matrix4x4.TRS(prefabCenter, prefabRotation, Vector3.one);

            SnapCollisionCheck(prefabCenter);
            Graphics.DrawMeshNow(prefabMesh, mousePos, prefabRotation);

            CollisionCheck(mousePos + prefabRotation * prefabMesh.bounds.center);

            //if (!buildingModuleChosen.Rules())
            //{
            //    canPlace = false;
            //    previewMaterial.SetColor("_BaseColor", Color.red);
            //}

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

            // reset parameters
            chosenPrefab = null;
            prefabMesh = null;
            prefabGameObj = null;
        }
    }

    private void CollisionCheck(Vector3 prefabCenter)
    {
        // check for collison with an overlap box around the mesh
        Collider[] overlapedColliders = Physics.OverlapBox(prefabCenter, prefabMesh.bounds.extents*0.99f, prefabRotation);

        // draw said overlap box
        Handles.DrawWireCube(Vector3.zero, prefabMesh.bounds.extents * 1.99f);

        // set canPlace to true if there are no collision and false if there are
        previewMaterial.SetColor("_BaseColor", Color.green);
        canPlace = true;

        if (overlapedColliders.Length > 1)
        {
            canPlace = false;
            previewMaterial.SetColor("_BaseColor", Color.red);
        }
    }

    private bool SnapCollisionCheck(Vector3 prefabCenter)
    {
        if(!Event.current.alt)
            return false;

        Collider[] overlapedBuildings = Physics.OverlapBox(prefabCenter, prefabMesh.bounds.extents * 1.2f, prefabRotation);
        Handles.color = Color.blue;
        Handles.DrawWireCube(Vector3.zero, prefabMesh.bounds.extents * 2.4f);

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
            mousePos = buildingModule.Snap(mousePos); // idk if i should use prefabCenter or mousePos

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
        EditorGUILayout.PropertyField(layerMaskProp);
        EditorGUILayout.PropertyField(previewMaterialProp);
        so.ApplyModifiedProperties();

        var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPos);
        scrollPos = scrollViewScope.scrollPosition;

        if (GUILayout.Button("ResetRotation"))
        {
            prefabRotation = Quaternion.identity;
        }

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
                    if(!prefab.TryGetComponent(out buildingModuleChosen))
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