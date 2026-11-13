using UnityEngine;

// Attach this script to an empty GameObject named "GameBootstrap" in the scene.
// It will auto-create all required managers and the Engineer on Play.
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        SpawnManager<GameEconomy>("GameEconomy");
        SpawnManager<UIManager>("UIManager");
        SpawnManager<GameStateManager>("GameStateManager");
        SpawnManager<BuildingManager>("BuildingManager");
        SpawnManager<WaveManager>("WaveManager");
        SpawnGridManager();
        SpawnEngineer();
        SetupCamera();
    }

    static void SpawnManager<T>(string goName) where T : MonoBehaviour
    {
        if (FindAnyObjectByType<T>() == null)
        {
            new GameObject(goName).AddComponent<T>();
        }
    }

    static void SpawnGridManager()
    {
        if (FindAnyObjectByType<GridManager>() == null)
        {
            GameObject gm = new GameObject("GridManager");
            gm.AddComponent<GridManager>();
        }
    }

    static void SpawnEngineer()
    {
        if (FindAnyObjectByType<EngineerController>() == null)
        {
            // Capsule placeholder until 3D model is imported
            GameObject eng = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            eng.name = "Engineer";
            eng.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);

            Renderer r = eng.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(r.sharedMaterial);
                r.material.color = new Color(0.95f, 0.76f, 0.19f); // Engineer Yellow
            }

            eng.AddComponent<EngineerController>();
        }
    }

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Isometric-style top-down view covering full 16x10 grid
        cam.transform.position = new Vector3(8f, 18f, -2f);
        cam.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
        cam.backgroundColor = new Color(0.02f, 0.05f, 0.1f);
    }
}
