using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LanyardPlacer : MonoBehaviour
{
    [ContextMenu("Lanyardleri Masaya Yerlestir")]
    public void PlaceLanyards()
    {
        GameObject tableObj = GameObject.Find("Table");
        if (tableObj == null)
        {
            Debug.LogError("Scenario 1 | Sahnede 'Table' bulunamadi!");
            return;
        }

        // Find Scenario1Manager
        Scenario1Manager manager = FindObjectOfType<Scenario1Manager>();

        // Create or find Lanyard_Safe
        GameObject safeLanyard = GameObject.Find("Lanyard_Safe");
        if (safeLanyard == null)
        {
            safeLanyard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            safeLanyard.name = "Lanyard_Safe";
        }
        safeLanyard.transform.SetParent(null); // Unparent to make it root
        
        // Put exactly on the table (relative to Table.001 mesh)
        GameObject tableMesh = GameObject.Find("Table.001");
        if (tableMesh != null)
        {
            Vector3 tablePos = tableMesh.transform.position;
            // Masanın dünya koordinatlarındaki üst yüzeyine yerleştiriyoruz
            safeLanyard.transform.position = tablePos + new Vector3(-0.3f, 0.45f, 0f);
        }
        else
        {
            safeLanyard.transform.position = new Vector3(37.95f, 0.77f, 140.3f);
        }
        
        safeLanyard.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        safeLanyard.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);

        // Components
        if (safeLanyard.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() == null)
        {
            safeLanyard.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }
        LanyardTrigger safeTrigger = safeLanyard.GetComponent<LanyardTrigger>();
        if (safeTrigger == null)
        {
            safeTrigger = safeLanyard.AddComponent<LanyardTrigger>();
        }
        safeTrigger.manager = manager;
        safeTrigger.isSafe = true;

        // Material Color (Green)
        Renderer safeRenderer = safeLanyard.GetComponent<Renderer>();
        if (safeRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            safeRenderer.sharedMaterial = mat;
        }

        // Create or find Lanyard_Unsafe
        GameObject unsafeLanyard = GameObject.Find("Lanyard_Unsafe");
        if (unsafeLanyard == null)
        {
            unsafeLanyard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            unsafeLanyard.name = "Lanyard_Unsafe";
        }
        unsafeLanyard.transform.SetParent(null);
        
        if (tableMesh != null)
        {
            Vector3 tablePos = tableMesh.transform.position;
            unsafeLanyard.transform.position = tablePos + new Vector3(0.3f, 0.45f, 0f);
        }
        else
        {
            unsafeLanyard.transform.position = new Vector3(38.35f, 0.77f, 140.3f);
        }
        
        unsafeLanyard.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        unsafeLanyard.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);

        if (unsafeLanyard.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() == null)
        {
            unsafeLanyard.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }
        LanyardTrigger unsafeTrigger = unsafeLanyard.GetComponent<LanyardTrigger>();
        if (unsafeTrigger == null)
        {
            unsafeTrigger = unsafeLanyard.AddComponent<LanyardTrigger>();
        }
        unsafeTrigger.manager = manager;
        unsafeTrigger.isSafe = false;

        // Material Color (Red)
        Renderer unsafeRenderer = unsafeLanyard.GetComponent<Renderer>();
        if (unsafeRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            unsafeRenderer.sharedMaterial = mat;
        }

        // Fix LineRenderer material
        GameObject lanyardLineObj = GameObject.Find("LanyardLine");
        if (lanyardLineObj != null)
        {
            LineRenderer lineRenderer = lanyardLineObj.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Material ropeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (ropeMat == null) ropeMat = new Material(Shader.Find("Standard"));
                ropeMat.color = new Color(0.8f, 0.6f, 0.4f);
                lineRenderer.sharedMaterial = ropeMat;
            }
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
        Debug.Log("Scenario 1 | Lanyard nesneleri masanin uzerine başarıyla yerleştirildi!");
    }
}
