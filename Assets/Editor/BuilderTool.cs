using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuilderTool : EditorWindow
{
    public Material PreviewMaterial;
    public float OffsetPower = 1;

    SerializedObject so;
    SerializedProperty previewMaterialProp, offsetPowerProp;

    Vector3 meshPos, snappedPos, closestGridPoint, offset;
    Vector2 scrollPos;

    List<GameObject> walls = new(), floors = new(), roofs = new(), savedHouses = new();
    GameObject prefabGameObj, chosenPrefab, parentGameobject;

    Mesh prefabMesh;
    Quaternion prefabRotation;

    IBuildingModule buildingModuleChosen;

    bool smallerCollision = false, canPlace;

    string[] sheetNames = new string[4] { "Walls", "Floor", "Roof", "Saved Houses" };


    int savedHousesIndex;
    SHEETS sheets;

    Camera cam;

    [MenuItem("Tools/Builder Tool")]
    public static void OpenWindow() => GetWindow<BuilderTool>("Builder Tool");

    private void OnEnable()
    {
        prefabRotation = Quaternion.identity;
        so = new(this);

        previewMaterialProp = so.FindProperty("PreviewMaterial");
        offsetPowerProp = so.FindProperty("OffsetPower");

        walls = GetPrefabs("Walls");
        floors = GetPrefabs("Floors");
        roofs = GetPrefabs("Roofs");
        savedHouses = GetPrefabs("SavedHouses");


        SceneView.duringSceneGui += DurinSceneGui;
        parentGameobject = new GameObject("SavedHouseN°" + savedHousesIndex);
        parentGameobject.transform.position = Vector3.zero;
        parentGameobject.transform.rotation = Quaternion.identity;


        if (EditorPrefs.HasKey("PreviewMaterial"))
            PreviewMaterial = AssetDatabase.LoadAssetAtPath<Material>(EditorPrefs.GetString("PreviewMaterial"));

        if (EditorPrefs.HasKey("savedHousesIndex"))
        {
            savedHousesIndex = EditorPrefs.GetInt("savedHousesIndex");
        }

    }

    private void OnDisable()
    {
        EditorPrefs.SetString("PreviewMaterial", AssetDatabase.GetAssetPath(PreviewMaterial));
        EditorPrefs.SetInt("savedHousesIndex", savedHousesIndex);
        SceneView.duringSceneGui -= DurinSceneGui;
    }

    private void DurinSceneGui(SceneView view)
    {
        cam = view.camera;
        RaycastHit hit;
        int controllerID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controllerID); // this is needed to use Event.Current.Keycode
        if (prefabMesh != null)
        {
            // change mouse position (tho only if you are not rotateing the prefab)
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out hit) && !RotateingPrefab())
                meshPos = hit.point + offset;


            // set canPlace to true and the color to green but then if there's a collision canPlace turns false and the color is set to red
            PreviewMaterial.SetColor("_BaseColor", Color.green);
            canPlace = true;

            smallerCollision = false;

            // calculate the center of the mesh drawn acounting for the rotation
            Vector3 prefabCenter = meshPos + prefabRotation * prefabMesh.bounds.center;
            Handles.matrix = Matrix4x4.TRS(prefabCenter, prefabRotation, Vector3.one);
            MeshPositionOffset();

            smallerCollision = SnapCollisionCheck(prefabCenter);


            CollisionCheck(meshPos + prefabRotation * prefabMesh.bounds.center);

            if (!buildingModuleChosen.Rules(prefabCenter, prefabMesh, prefabRotation))
            {
                canPlace = false;
                PreviewMaterial.SetColor("_BaseColor", Color.red);
            }

            //drawing the mesh
            PreviewMaterial.SetPass(0);
            Graphics.DrawMeshNow(prefabMesh, meshPos, prefabRotation);

            Placement();


        }
    }

    private void MeshPositionOffset()
    {

        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
        {
            offset += prefabRotation * Vector3.up * OffsetPower * Time.deltaTime;
            e.Use();
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
        {
            offset -= prefabRotation * Vector3.up * OffsetPower * Time.deltaTime;
            e.Use();
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
            prefabGameObj.transform.position = meshPos;
            prefabGameObj.transform.rotation = prefabRotation;
            prefabGameObj.transform.parent = parentGameobject.transform;

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
            collisionMult = .90f;

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
                PreviewMaterial.SetColor("_BaseColor", Color.red);
            }

        }
    }

    private bool SnapCollisionCheck(Vector3 prefabCenter)
    {
        if (!Event.current.alt)
            return false;

        List<Vector3> snapGridPoints = CreateGridPoints(prefabCenter);

        Collider[] overlapedBuildings = Physics.OverlapBox(prefabCenter, prefabMesh.bounds.extents * 1.6f, prefabRotation);


        Handles.color = Color.blue;
        Handles.DrawWireCube(Vector3.zero, prefabMesh.bounds.extents * 3.2f);

        float distance = Mathf.Infinity;
        Collider nearestCollider = null;

        if (overlapedBuildings.Length <= 0)
        {

            return false;
        }

        foreach (Collider col in overlapedBuildings)
        {
            float dist = Vector3.Distance(col.transform.position, meshPos);
            if (dist < distance)
            {
                distance = dist;
                nearestCollider = col;
            }

        }

        if (nearestCollider.TryGetComponent(out IBuildingModule buildingModule))
        {
            (snappedPos, closestGridPoint) = buildingModule.Snap(snapGridPoints, meshPos);

            Vector3 dirToClosestPoint = meshPos - closestGridPoint;

            meshPos = snappedPos + dirToClosestPoint;

            return true;
        }

        return false;
    }

    private List<Vector3> CreateGridPoints(Vector3 prefabCenter)
    {
        Vector3 rotatedSize = prefabRotation * prefabMesh.bounds.size;

        List<Vector3> snapPosGrid = new();

        float posX;
        float posY;
        float posZ;
        Vector3 pos;

        int min = -1;
        int max = 1;
        for (int x = min; x <= max; x++)
        {
            posX = prefabCenter.x + (rotatedSize.x / 4) * x * 2;

            for (int y = min; y <= max; y++)
            {
                posY = prefabCenter.y + (rotatedSize.y / 4) * y * 2;

                for (int z = min; z <= max; z++)
                {
                    if (z == 0 && y == 0 && x == 0)
                        continue;
                    if (z != 0 && (y != 0 || x != 0))
                        continue;
                    //if (x != 0 && y != 0)
                    //    continue;

                    posZ = prefabCenter.z + (rotatedSize.z / 3.5f) * z * 2;
                    pos = new(posX, posY, posZ);

                    snapPosGrid.Add(pos);

                    Handles.color = Color.yellow;
                    Handles.DrawWireCube(pos - prefabCenter, Vector3.one * 0.05f);
                }
            }
        }

        return snapPosGrid;
    }

    private bool RotateingPrefab()
    {
        Event e = Event.current;
        bool holdingKey = e.control;

        if (holdingKey)
        {
            prefabRotation = Handles.RotationHandle(prefabRotation, meshPos);
            return true;
        }
        return false;
    }

    private void OnGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(previewMaterialProp);
        EditorGUILayout.PropertyField(offsetPowerProp);
        so.ApplyModifiedProperties();

        if (GUILayout.Button("ResetRotation"))
        {
            prefabRotation = Quaternion.identity;
        }

        if (GUILayout.Button("ResetOffset"))
        {
            offset = Vector3.zero;
        }
        var prevSheet = sheets;
        sheets = (SHEETS)GUILayout.Toolbar((int)sheets, sheetNames);

        if (prevSheet != sheets)
            scrollPos = Vector2.zero;

        switch ((int)sheets)
        {
            case 0:
                SetUpSheet(walls);
                break;

            case 1:
                SetUpSheet(floors);
                break;

            case 2:
                SetUpSheet(roofs);
                break;

            case 3:
                if (GUILayout.Button("SaveHouse"))
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(parentGameobject, AssetDatabase.GenerateUniqueAssetPath("Assets/prefabs/SavedHouses/" + parentGameobject.name + ".prefab"), InteractionMode.UserAction, out var x);

                    if (x)
                    {
                        savedHousesIndex++;
                        Destroy(parentGameobject);
                        parentGameobject = new GameObject("SavedHouseN°" + savedHousesIndex);
                        parentGameobject.transform.position = Vector3.zero;
                        parentGameobject.transform.rotation = Quaternion.identity;
                    }
                }
                
                break;
        }
    }

    private void SetUpSheet(List<GameObject> _Prefabs)
    {
        var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPos);
        scrollPos = scrollViewScope.scrollPosition;
        using (scrollViewScope)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope())
                {
                    ChoosePrefabs(_Prefabs);
                }
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

public enum SHEETS
{
    Walls = 0,
    Floor = 1,
    Roof = 2,
    SavedHouses = 3,
}