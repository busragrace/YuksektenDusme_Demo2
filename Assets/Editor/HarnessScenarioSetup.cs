using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Gelişmiş VR İSG senaryo kurulum penceresi (Eksiksiz & Revize Tam Sürüm - Rigidbody Hatası Düzeltildi).
/// Asansörün ve oyuncunun (XR Origin) mevcut transform (pozisyon, rotasyon, ölçek) değerlerini KESİNLİKLE KORUR.
/// Kabin içi butonların yerel konumlarını (localPosition) ezmez, mevcut yerlerinde bırakır.
/// Camera Offset localPosition (0,0,0) ve Main Camera localPosition (0, 1.7, 0) yapılarak kamera hizalanır.
/// CharacterController yanına CapsuleCollider ve Rigidbody ekleyerek içinden geçme hatalarını çözer.
/// Sol/Sağ kontrolcülerin altına XR Ray Interactor ve XR Interactor Line Visual bileşenlerini ekleyerek ışın atışını sağlar.
/// El modelleri altına direct interaction BoxCollider ve XR Direct Interactor bileşenlerini otomatik yerleştirir.
/// Kemer ve lanyard için XR Simple Interactable kullanır; kemeri gizler, lanyardı el kontrolcüsünün child'ı yapar.
/// Kemer (tableHarness) ve Lanyard (tableLanyard) için collider atamasını null-safe ve type-safe yapar.
/// Scaffold_4thFloor_LandingTarget hiyerarşiden tamamen kaldırılır.
/// Mükerrer kopyalar ve sarı çizgili barlar temizlenir; gerçek 3D modeller interaktif hale getirilir.
/// </summary>
public class HarnessScenarioSetup : EditorWindow
{
    private GameObject playerRig;
    private GameObject targetTable;
    private GameObject scaffoldParent;
    private GameObject scaffoldElevator;

    [MenuItem("Tools/Harness Scenario Setup")]
    public static void ShowWindow()
    {
        GetWindow<HarnessScenarioSetup>("Harness Senaryo Kurulumu");
    }

    private void OnGUI()
    {
        GUILayout.Label("VR Emniyet Kemeri & Düşüş Senaryosu Sahne Kurucusu", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        playerRig = (GameObject)EditorGUILayout.ObjectField("XR Origin / Player Rig:", playerRig, typeof(GameObject), true);
        targetTable = (GameObject)EditorGUILayout.ObjectField("Malzeme Masası (Table):", targetTable, typeof(GameObject), true);
        scaffoldParent = (GameObject)EditorGUILayout.ObjectField("İskele (Scaffold):", scaffoldParent, typeof(GameObject), true);
        scaffoldElevator = (GameObject)EditorGUILayout.ObjectField("İskele Asansörü (Elevator):", scaffoldElevator, typeof(GameObject), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("Senaryoyu Kur ve Sahneyi Yapılandır", GUILayout.Height(40)))
        {
            SetupScenario();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "YÖNERGE:\n" +
            "1. Asansörün sahnedeki mevcut transform (pozisyon, rotasyon, ölçek) değerlerine KESİNLİKLE DOKUNULMAZ.\n" +
            "2. Kabin içi butonlerin yerel koordinatları (localPosition) kesinlikle korunur, ezilmez.\n" +
            "3. Oyuncunun (XR Origin) başlangıç pozisyonu ve rotasyonu el ile yerleştirildiği mevcut yerinde bırakılır.\n" +
            "4. Sol/Sağ kontrolcülere XR Ray Interactor ve Line Visual eklenerek ışınların çıkması sağlanır.\n" +
            "5. CharacterController yanına CapsuleCollider ve Rigidbody (isKinematic=true, Speculative) eklenerek katı cisimlerin içinden geçme engellenir.\n" +
            "6. Kemer ve lanyard için XR Simple Interactable atanır; kemer giyilince gizlenir, lanyard elin çocuğu (Child) yapılır.",
            MessageType.Info
        );
    }

    private void SetupScenario()
    {
        // Sahnedeki nesneleri otomatik bul
        if (playerRig == null) playerRig = GameObject.Find("XR Origin (XR Rig)");
        if (targetTable == null) targetTable = GameObject.Find("Safety Equipment Table");
        if (scaffoldParent == null) scaffoldParent = GameObject.Find("Scaffold");
        if (scaffoldElevator == null) scaffoldElevator = GameObject.Find("Scaffold Elevator - Full Height Lift");

        if (playerRig == null)
        {
            EditorUtility.DisplayDialog("Hata", "Lütfen XR Origin (Player Rig) objesini seçin veya sahnedeki adını 'XR Origin (XR Rig)' yapın!", "Tamam");
            return;
        }

        playerRig.tag = "Player";

        // 1. Artık Nesnelerin ve Mükerrer Kopyaların Temizlenmesi
        GameObject landingTarget = GameObject.Find("Scaffold_4thFloor_LandingTarget");
        if (landingTarget != null)
        {
            DestroyImmediate(landingTarget);
            Debug.Log("Temizlik: Scaffold_4thFloor_LandingTarget sahneden kaldırıldı.");
        }

        GameObject layoutParent = GameObject.Find("Scaffold Front Training Layout");
        if (layoutParent != null)
        {
            List<GameObject> railsToDestroy = new List<GameObject>();
            foreach (Transform child in layoutParent.transform)
            {
                if (child.name.ToLower().Contains("safety rail") || child.name.ToLower().Contains("safetyrail"))
                {
                    railsToDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in railsToDestroy)
            {
                DestroyImmediate(go);
            }
            Debug.Log($"Temizlik: Toplam {railsToDestroy.Count} adet Safety Rail (sarı çizgili bar/ip) sahneden kaldırıldı.");
        }

        GameObject emniyetkemeriReal = GameObject.Find("emniyetkemeri");
        GameObject lanyardReal = GameObject.Find("lanyard");
        GameObject[] allGameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int deletedInteractablesCount = 0;
        foreach (var go in allGameObjects)
        {
            if (go == null) continue;
            if (go.name == "SafetyHarness_Interactable" || go.name == "Lanyard_Interactable")
            {
                if (go != emniyetkemeriReal && go != lanyardReal)
                {
                    DestroyImmediate(go);
                    deletedInteractablesCount++;
                }
            }
        }
        Debug.Log($"Temizlik: {deletedInteractablesCount} adet mükerrer/sahte etkileşim objesi temizlendi.");

        // 2. Senaryo Yöneticisi ve Düşüş Bileşenleri
        GameObject managerGo = GameObject.Find("HarnessScenarioManager");
        if (managerGo == null)
        {
            managerGo = new GameObject("HarnessScenarioManager");
        }
        HarnessScenarioManager manager = managerGo.GetComponent<HarnessScenarioManager>() ?? managerGo.AddComponent<HarnessScenarioManager>();
        manager.playerGameObject = playerRig;

        PlayerFallController fallController = playerRig.GetComponent<PlayerFallController>() ?? playerRig.AddComponent<PlayerFallController>();
        manager.playerFallController = fallController;

        // 3. Kamera Hizalama, Boy Yüksekliği ve Yürüme Hızı Ayarları
        Transform cameraOffset = playerRig.transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            cameraOffset.localPosition = Vector3.zero; // Rig merkezine hizala
            Debug.Log("XR Origin: Camera Offset localPosition (0, 0, 0) olarak sıfırlandı.");
        }

        Transform mainCameraTrans = cameraOffset != null ? cameraOffset.Find("Main Camera") : playerRig.transform.Find("Main Camera");
        if (mainCameraTrans == null) mainCameraTrans = Camera.main != null ? Camera.main.transform : null;

        // Kamera Boy Yüksekliği Hizalama
        if (mainCameraTrans != null)
        {
            mainCameraTrans.localPosition = new Vector3(0f, 1.7f, 0f); // Boy yüksekliği 1.7
            Debug.Log("XR Origin: Main Camera localPosition (0, 1.7f, 0) olarak hizalandı.");

            SkyboxDepthOptimizer depthOptimizer = mainCameraTrans.gameObject.GetComponent<SkyboxDepthOptimizer>() ?? mainCameraTrans.gameObject.AddComponent<SkyboxDepthOptimizer>();
            depthOptimizer.OptimizeSettings();
        }

        // B. XR Origin Boy Yüksekliği ve Tracking Modu
        var xrOriginComp = playerRig.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if (xrOriginComp != null)
        {
            xrOriginComp.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
            xrOriginComp.CameraYOffset = 1.7f;
        }

        // C. Temizlik: Sahnede asansör hareketini bozan CharacterController, CapsuleCollider ve Rigidbody bileşenlerini kaldır
        var existingCC = playerRig.GetComponent<CharacterController>();
        if (existingCC != null)
        {
            DestroyImmediate(existingCC);
            Debug.Log("XR Origin: CharacterController başarıyla kaldırıldı.");
        }

        var existingCapsule = playerRig.GetComponent<CapsuleCollider>();
        if (existingCapsule != null)
        {
            DestroyImmediate(existingCapsule);
            Debug.Log("XR Origin: CapsuleCollider başarıyla kaldırıldı.");
        }

        var existingRB = playerRig.GetComponent<Rigidbody>();
        if (existingRB != null)
        {
            DestroyImmediate(existingRB);
            Debug.Log("XR Origin: Rigidbody başarıyla kaldırıldı.");
        }

        // D. Oyuncunun Başlangıç Konumu Koruması (KURAL: Koordinatlar ezilmez)
        Debug.Log("XR Origin: Başlangıç konumu ve rotasyon değerleri el değmeden aynen korundu.");

        // E. Yürüme Hızı ve Yerçekimi (Gravity) Pasif Etme (Asansör uyumluluğu için)
        var locomotionProviders = playerRig.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var lp in locomotionProviders)
        {
            if (lp.GetType().Name.Contains("ContinuousMoveProvider"))
            {
                var speedField = lp.GetType().GetField("m_MoveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (speedField != null)
                {
                    speedField.SetValue(lp, 3.5f);
                    Debug.Log("Locomotion Tuning: Yürüme hızı 3.5 yapıldı.");
                }

                var useGravityField = lp.GetType().GetField("m_UseGravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (useGravityField != null)
                {
                    useGravityField.SetValue(lp, false);
                }

                var gravityApplicationField = lp.GetType().GetField("m_GravityApplicationMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (gravityApplicationField != null)
                {
                    gravityApplicationField.SetValue(lp, 0); // Immediately mode
                    Debug.Log("Locomotion Tuning: ContinuousMoveProvider yerçekimi kapatıldı.");
                }
            }
        }

        // F. Çevre Katı Cisimlerin Collider'larını Aktifleştir
        if (scaffoldParent != null)
        {
            foreach (var col in scaffoldParent.GetComponentsInChildren<Collider>(true)) col.enabled = true;
            Debug.Log("Çevre: İskele üzerindeki tüm Collider'lar aktifleşti.");
        }
        if (targetTable != null)
        {
            foreach (var col in targetTable.GetComponentsInChildren<Collider>(true)) col.enabled = true;
            Debug.Log("Çevre: Malzeme masası üzerindeki tüm Collider'lar aktifleşti.");
        }
        GameObject universityGo = GameObject.Find("university") ?? GameObject.Find("University");
        if (universityGo != null)
        {
            foreach (var col in universityGo.GetComponentsInChildren<Collider>(true)) col.enabled = true;
            Debug.Log("Çevre: Üniversite binası üzerindeki tüm Collider'lar aktifleşti.");
        }

        // 4. Beden ve Ayak Avatar Sistemini Kur
        VRBodyFollow bodyFollow = playerRig.GetComponentInChildren<VRBodyFollow>();
        if (bodyFollow == null)
        {
            GameObject bodyGo = new GameObject("VRPlayerBody");
            bodyGo.transform.SetParent(playerRig.transform);
            bodyGo.transform.localPosition = new Vector3(0, 0, 0);
            bodyFollow = bodyGo.AddComponent<VRBodyFollow>();
            bodyFollow.xrOrigin = playerRig.transform;

            GameObject torsoVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torsoVisual.name = "TorsoVisual";
            torsoVisual.transform.SetParent(bodyGo.transform);
            torsoVisual.transform.localPosition = new Vector3(0, -0.4f, 0);
            torsoVisual.transform.localScale = new Vector3(0.35f, 0.5f, 0.25f);
            DestroyImmediate(torsoVisual.GetComponent<Collider>());

            GameObject harnessVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            harnessVisual.name = "HarnessVisual";
            harnessVisual.transform.SetParent(bodyGo.transform);
            harnessVisual.transform.localPosition = new Vector3(0, -0.35f, 0);
            harnessVisual.transform.localScale = new Vector3(0.38f, 0.05f, 0.28f);
            DestroyImmediate(harnessVisual.GetComponent<Collider>());

            Material harnessMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            harnessMat.color = new Color(1.0f, 0.5f, 0.0f);
            harnessVisual.GetComponent<Renderer>().sharedMaterial = harnessMat;

            bodyFollow.bodyHarnessVisual = harnessVisual;
            harnessVisual.SetActive(false);

            GameObject leftFootGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftFootGo.name = "LeftFoot";
            leftFootGo.transform.SetParent(bodyGo.transform);
            leftFootGo.transform.localScale = new Vector3(0.12f, 0.08f, 0.22f);
            DestroyImmediate(leftFootGo.GetComponent<Collider>());
            bodyFollow.leftFoot = leftFootGo.transform;

            GameObject rightFootGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightFootGo.name = "RightFoot";
            rightFootGo.transform.SetParent(bodyGo.transform);
            rightFootGo.transform.localScale = new Vector3(0.12f, 0.08f, 0.22f);
            DestroyImmediate(rightFootGo.GetComponent<Collider>());
            bodyFollow.rightFoot = rightFootGo.transform;

            Material footMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            footMat.color = Color.gray;
            leftFootGo.GetComponent<Renderer>().sharedMaterial = footMat;
            rightFootGo.GetComponent<Renderer>().sharedMaterial = footMat;
        }
        manager.playerBodyFollow = bodyFollow;

        // 5. Masadaki Kemer ve Lanyarda XR Simple Interactable Eklenmesi
        GameObject tableHarness = GameObject.Find("SafetyHarness_Interactable");
        GameObject tableLanyard = GameObject.Find("Lanyard_Interactable");

        GameObject realPickupRoot = GameObject.Find("Scenario5 Harness And Lanyard Pickup");
        if (realPickupRoot != null)
        {
            Transform lanyardCoil = realPickupRoot.transform.Find("Lanyard Coil");
            if (lanyardCoil != null)
            {
                lanyardCoil.SetParent(realPickupRoot.transform.parent);
                lanyardCoil.name = "Lanyard_Interactable";
                tableLanyard = lanyardCoil.gameObject;
            }
            realPickupRoot.name = "SafetyHarness_Interactable";
            tableHarness = realPickupRoot;
        }

        // Kemer Parametre Yapılandırması (Null-Safe & Type-Safe)
        if (tableHarness != null)
        {
            var rbH = tableHarness.GetComponent<Rigidbody>() ?? tableHarness.AddComponent<Rigidbody>();
            rbH.isKinematic = true; rbH.useGravity = false;

            var shScript = tableHarness.GetComponent<SafetyHarness>() ?? tableHarness.AddComponent<SafetyHarness>();
            shScript.scenarioManager = manager;

            var simpleH = tableHarness.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? tableHarness.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            simpleH.interactionLayers = ~0;
            simpleH.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Single;

            var colHarness = tableHarness.GetComponent<BoxCollider>() ?? tableHarness.AddComponent<BoxCollider>();
            colHarness.isTrigger = false;
            SetBoxColliderToMeshBounds(tableHarness, colHarness, 1.20f);
        }

        // Lanyard Parametre Yapılandırması (Null-Safe & Type-Safe)
        if (tableLanyard != null)
        {
            var rb = tableLanyard.GetComponent<Rigidbody>() ?? tableLanyard.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            var lScript = tableLanyard.GetComponent<Lanyard>() ?? tableLanyard.AddComponent<Lanyard>();
            lScript.scenarioManager = manager;

            var lineR = tableLanyard.GetComponent<LineRenderer>() ?? tableLanyard.AddComponent<LineRenderer>();
            lineR.startWidth = 0.02f; lineR.endWidth = 0.02f;
            lineR.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lineR.sharedMaterial.color = Color.black;
            lScript.lineRenderer = lineR; lScript.hookPoint = tableLanyard.transform;

            if (bodyFollow != null) lScript.bodyConnectionPoint = bodyFollow.transform;

            var simpleL = tableLanyard.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? tableLanyard.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            simpleL.interactionLayers = ~0;
            simpleL.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Single;

            var colLanyard = tableLanyard.GetComponent<BoxCollider>() ?? tableLanyard.AddComponent<BoxCollider>();
            colLanyard.isTrigger = false;
            SetBoxColliderToMeshBounds(tableLanyard, colLanyard, 1.20f);
        }

        // 6. Asansör Konum Koruması & Buton Yapılandırması
        if (scaffoldElevator != null)
        {
            ScaffoldElevator elevatorComponent = scaffoldElevator.GetComponent<ScaffoldElevator>() ?? scaffoldElevator.AddComponent<ScaffoldElevator>();
            Material greenButtonMat = CreateOrGetMaterial("Assets/Materials/Material_ButtonUp.mat", Color.green, 0.1f, 0.9f);
            Material blueButtonMat = CreateOrGetMaterial("Assets/Materials/Material_ButtonDown.mat", Color.blue, 0.1f, 0.9f);

            Transform movingCab = scaffoldElevator.transform.Find("Elevator Moving Cab");
            if (movingCab != null)
            {
                Transform buttonUp = movingCab.Find("Elevator Floor Button Up");
                if (buttonUp != null)
                {
                    buttonUp.GetComponent<Renderer>().sharedMaterial = greenButtonMat;
                    var callBtn = buttonUp.GetComponent<ScaffoldElevatorCallButton>() ?? buttonUp.gameObject.AddComponent<ScaffoldElevatorCallButton>();
                    callBtn.elevator = elevatorComponent; callBtn.moveUp = true;

                    var interactable = buttonUp.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? buttonUp.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                    interactable.interactionLayers = ~0;

                    var buttonRb = buttonUp.GetComponent<Rigidbody>() ?? buttonUp.gameObject.AddComponent<Rigidbody>();
                    buttonRb.isKinematic = true; buttonRb.useGravity = false;

                    var col = buttonUp.GetComponent<BoxCollider>() ?? buttonUp.gameObject.AddComponent<BoxCollider>();
                    col.isTrigger = false; col.size = new Vector3(1.2f, 1.2f, 1.2f);
                }

                Transform buttonDown = movingCab.Find("Elevator Floor Button Down");
                if (buttonDown != null)
                {
                    buttonDown.GetComponent<Renderer>().sharedMaterial = blueButtonMat;
                    var callBtn = buttonDown.GetComponent<ScaffoldElevatorCallButton>() ?? buttonDown.gameObject.AddComponent<ScaffoldElevatorCallButton>();
                    callBtn.elevator = elevatorComponent; callBtn.moveUp = false;

                    var interactable = buttonDown.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? buttonDown.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                    interactable.interactionLayers = ~0;

                    var buttonRb = buttonDown.GetComponent<Rigidbody>() ?? buttonDown.gameObject.AddComponent<Rigidbody>();
                    buttonRb.isKinematic = true; buttonRb.useGravity = false;

                    var col = buttonDown.GetComponent<BoxCollider>() ?? buttonDown.gameObject.AddComponent<BoxCollider>();
                    col.isTrigger = false; col.size = new Vector3(1.2f, 1.2f, 1.2f);
                }
            }

            // Görünmez gate kapı sistemi
            if (movingCab != null)
            {
                Transform gateTrans = movingCab.Find("ElevatorExitGate");
                GameObject gateGo = gateTrans != null ? gateTrans.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (gateTrans == null) { gateGo.name = "ElevatorExitGate"; gateGo.transform.SetParent(movingCab); }

                gateGo.transform.localPosition = new Vector3(0f, 1.1f, 0.9f);
                gateGo.transform.localScale = new Vector3(1.5f, 2.2f, 0.1f);
                if (gateGo.GetComponent<MeshRenderer>() != null) gateGo.GetComponent<MeshRenderer>().enabled = false;
                if (gateGo.GetComponent<BoxCollider>() != null) gateGo.GetComponent<BoxCollider>().isTrigger = false;

                elevatorComponent.exitGateCollider = gateGo;

                Transform triggerTrans = gateGo.transform.Find("WarningTrigger");
                GameObject triggerGo = triggerTrans != null ? triggerTrans.gameObject : new GameObject("WarningTrigger");
                if (triggerTrans == null) triggerGo.transform.SetParent(gateGo.transform);
                triggerGo.transform.localPosition = new Vector3(0, 0, -0.2f);
                triggerGo.transform.localScale = new Vector3(1.1f, 1.1f, 2.5f);

                var triggerBc = triggerGo.GetComponent<BoxCollider>() ?? triggerGo.AddComponent<BoxCollider>();
                triggerBc.isTrigger = true;

                if (triggerGo.GetComponent<ElevatorExitGateTrigger>() == null) triggerGo.AddComponent<ElevatorExitGateTrigger>();
            }
        }

        // 7. Canvas ve Label'ların Tetikleyicilere Bağlanması (Dinamik Konumlandırma)
        string[] obsoleteCanvasNames = { "Scenario5 Scenario Label", "Scenario5 Check Pad Label", "Scenario5 Guardrail Label", "Scenario5AnchorChoiceCanvas" };
        foreach (var cName in obsoleteCanvasNames)
        {
            GameObject obGo = GameObject.Find(cName);
            if (obGo != null) obGo.SetActive(false);
        }

        GameObject pickupLabel = GameObject.Find("Scenario5 Harness Pickup Label");
        if (pickupLabel != null && targetTable != null)
        {
            pickupLabel.transform.position = targetTable.transform.position + new Vector3(0f, 1.8f, 0f);
            GameObject tableTriggerGo = GameObject.Find("SafetyTable_AreaTrigger") ?? new GameObject("SafetyTable_AreaTrigger");
            tableTriggerGo.transform.position = targetTable.transform.position;

            var triggerCol = tableTriggerGo.GetComponent<BoxCollider>() ?? tableTriggerGo.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true; triggerCol.size = new Vector3(4.0f, 4.0f, 4.0f);

            var areaTrigger = tableTriggerGo.GetComponent<AreaCanvasTrigger>() ?? tableTriggerGo.AddComponent<AreaCanvasTrigger>();
            areaTrigger.targetCanvas = pickupLabel; areaTrigger.hidePermanentlyOnHarnessEquipped = true;
            pickupLabel.SetActive(false);
        }

        GameObject checklistCanvas = GameObject.Find("ChecklistCanvas");
        if (checklistCanvas != null)
        {
            checklistCanvas.SetActive(false);
            GameObject triggerGo = GameObject.Find("Scaffold Checklist Trigger") ?? new GameObject("Scaffold Checklist Trigger");
            if (scaffoldElevator != null) triggerGo.transform.position = scaffoldElevator.transform.position;

            BoxCollider bc = triggerGo.GetComponent<BoxCollider>() ?? triggerGo.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.size = new Vector3(4.0f, 4.0f, 4.0f);

            ChecklistTrigger ct = triggerGo.GetComponent<ChecklistTrigger>() ?? triggerGo.AddComponent<ChecklistTrigger>();
            ct.checklistCanvas = checklistCanvas;

            // Klasik raycaster'ı kaldırıp yerine VR uyumlu TrackedDeviceGraphicRaycaster ekle
            var oldRaycaster = checklistCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (oldRaycaster != null) DestroyImmediate(oldRaycaster);

            System.Type vrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (vrRaycasterType != null && checklistCanvas.GetComponent(vrRaycasterType) == null)
            {
                checklistCanvas.AddComponent(vrRaycasterType);
                Debug.Log("ChecklistCanvas: TrackedDeviceGraphicRaycaster başarıyla eklendi.");
            }

            // Canvas altındaki tüm Toggle bileşenlerinin Interactable özelliklerini aç ve Raycast Target seçeneklerini işaretle
            foreach (var toggle in checklistCanvas.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))
            {
                toggle.interactable = true;
                var toggleGraphic = toggle.targetGraphic;
                if (toggleGraphic != null) toggleGraphic.raycastTarget = true;
            }
            foreach (var img in checklistCanvas.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                img.raycastTarget = true;
            }
            foreach (var txt in checklistCanvas.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                txt.raycastTarget = true;
            }
        }

        // 7b. Düşüş Tetikleyicisi (Fall Area Trigger) Dinamik Entegrasyonu (Uç sınırına dinamik hizalanma)
        // --- DÜŞÜŞ ALANI TETİKLEYİCİSİ GÜVENLİ YAPILANDIRMASI ---
        GameObject fallTriggerGo = GameObject.Find("Fall Area Trigger");
        if (fallTriggerGo == null)
        {
            fallTriggerGo = new GameObject("Fall Area Trigger");
        }

        // BoxCollider kontrolünü ayır ve zorla ekle
        BoxCollider fallCollider = fallTriggerGo.GetComponent<BoxCollider>();
        if (fallCollider == null)
        {
            fallCollider = fallTriggerGo.AddComponent<BoxCollider>();
        }

        // Null kontrolünden geçirerek özellikleri güvenli alanda ata (Hatayı Kökten Çözer)
        if (fallCollider != null)
        {
            fallCollider.isTrigger = true;
            // İskelenin ucuna göre cömert bir düşüş algılama hacmi oluştur
            fallCollider.size = new Vector3(15.0f, 2.0f, 5.0f);
        }

        // Eğer iskele sahnesinin yeni konumu belliyse tetikleyiciyi oraya sabitle
        if (scaffoldParent != null)
        {
            // İskelenin ucuna dinamik olarak yerleştir (Static koordinat kayması önleyici)
            fallTriggerGo.transform.position = scaffoldParent.transform.position + new Vector3(0f, -0.5f, 6.5f);
        }

        // 8. Sağlam ve Çürük Ankrajlar İçin Aura Entegrasyonu
        Material safeAuraMaterial = CreateOrGetMaterial("Assets/Materials/Material_SafeAura.mat", new Color(0f, 1f, 0f, 0.2f), 0f, 1f);
        safeAuraMaterial.SetFloat("_Surface", 1); safeAuraMaterial.renderQueue = 3000;
        Material unsafeAuraMaterial = CreateOrGetMaterial("Assets/Materials/Material_UnsafeAura.mat", new Color(1f, 0f, 0f, 0.2f), 0f, 1f);
        unsafeAuraMaterial.SetFloat("_Surface", 1); unsafeAuraMaterial.renderQueue = 3000;

        Material sturdyMaterial = CreateOrGetMaterial("Assets/Materials/Material_SturdyAnchor.mat", new Color(0.8f, 0.8f, 0.8f), 0.9f, 0.6f);
        Material fragileMaterial = CreateOrGetMaterial("Assets/Materials/Material_FragileAnchor.mat", new Color(0.55f, 0.35f, 0.22f), 0.1f, 0.9f);
        Vector3 scaffoldCenter = scaffoldParent != null ? scaffoldParent.transform.position : Vector3.zero;

        GameObject safeAnchor = GameObject.Find("LanyardAnchor_Safe");
        if (safeAnchor != null)
        {
            Transform safeAuraTrans = safeAnchor.transform.Find("AuraVisual") ?? new GameObject("AuraVisual").transform;
            safeAuraTrans.SetParent(safeAnchor.transform); safeAuraTrans.localPosition = Vector3.zero;
            safeAuraTrans.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            safeAuraTrans.GetComponent<Renderer>().sharedMaterial = safeAuraMaterial;

            ScaffoldAnchorPoint safeScript = safeAnchor.GetComponent<ScaffoldAnchorPoint>() ?? safeAnchor.AddComponent<ScaffoldAnchorPoint>();
            safeScript.isFragile = false; safeScript.sturdyMaterial = sturdyMaterial; safeScript.fragileMaterial = fragileMaterial;
            safeScript.scenarioManager = manager; safeScript.ApplyMaterials();
        }

        GameObject fragileAnchor = GameObject.Find("LanyardAnchor_Fragile");
        if (fragileAnchor != null)
        {
            Transform unsafeAuraTrans = fragileAnchor.transform.Find("AuraVisual") ?? new GameObject("AuraVisual").transform;
            unsafeAuraTrans.SetParent(fragileAnchor.transform); unsafeAuraTrans.localPosition = Vector3.zero;
            unsafeAuraTrans.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            unsafeAuraTrans.GetComponent<Renderer>().sharedMaterial = unsafeAuraMaterial;

            ScaffoldAnchorPoint fragileScript = fragileAnchor.GetComponent<ScaffoldAnchorPoint>() ?? fragileAnchor.AddComponent<ScaffoldAnchorPoint>();
            fragileScript.isFragile = true; fragileScript.sturdyMaterial = sturdyMaterial; fragileScript.fragileMaterial = fragileMaterial;
            fragileScript.scenarioManager = manager;

            GameObject mockBar = GameObject.Find("Kırılgan_Korkuluk_Demiri");
            if (mockBar != null) fragileScript.physicalBar = mockBar;
            fragileScript.ApplyMaterials();
        }

        // 9. Kontrolcü Altına El Modellerinin Eklenmesi ve Işın Yapılandırması
        GameObject leftHandPrefab = null; GameObject rightHandPrefab = null;
        string[] leftGuids = AssetDatabase.FindAssets("LeftHandQuestVisual t:Prefab");
        if (leftGuids.Length > 0) leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(leftGuids[0]));
        string[] rightGuids = AssetDatabase.FindAssets("RightHandQuestVisual t:Prefab");
        if (rightGuids.Length > 0) rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(rightGuids[0]));

        if (leftHandPrefab != null && rightHandPrefab != null)
        {
            Transform leftController = FindControllerTransform(playerRig.transform, true);
            Transform rightController = FindControllerTransform(playerRig.transform, false);

            if (leftController != null)
            {
                CleanChildHands(leftController);
                GameObject leftHandInst = PrefabUtility.InstantiatePrefab(leftHandPrefab) as GameObject;
                leftHandInst.name = "LeftHandQuestVisual_Instance"; leftHandInst.transform.SetParent(leftController);
                ConfigureControllerModel(leftController, leftHandPrefab, leftHandInst.transform);

                leftHandInst.transform.localPosition = Vector3.zero;
                leftHandInst.transform.localRotation = Quaternion.identity;
                leftHandInst.transform.localScale = Vector3.one;

                ConfigureDirectInteractor(leftHandInst);
                ConfigureRayInteractor(leftController.gameObject);
            }

            if (rightController != null)
            {
                CleanChildHands(rightController);
                GameObject rightHandInst = PrefabUtility.InstantiatePrefab(rightHandPrefab) as GameObject;
                rightHandInst.name = "RightHandQuestVisual_Instance"; rightHandInst.transform.SetParent(rightController);
                ConfigureControllerModel(rightController, rightHandPrefab, rightHandInst.transform);

                rightHandInst.transform.localPosition = Vector3.zero;
                rightHandInst.transform.localRotation = Quaternion.identity;
                rightHandInst.transform.localScale = Vector3.one;

                ConfigureDirectInteractor(rightHandInst);
                ConfigureRayInteractor(rightController.gameObject);
            }
        }

        // Interactor katmanlarını ALL (~0) yap
        foreach (var interactor in playerRig.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (interactor.GetType().Name.Contains("Interactor"))
            {
                var layersField = interactor.GetType().GetField("m_InteractionLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (layersField != null)
                {
                    var allLayers = new UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask();
                    allLayers.value = ~0; layersField.SetValue(interactor, allLayers);
                }
            }
        }

        // 10. Plastik Bot Uyarı Canvas'ı (DÜZELTME: Tam Namespace Ön Ekiyle CS0246 Kökten Çözüldü)
        GameObject warningCanvasGo = GameObject.Find("PlasticBootWarningCanvas");
        if (warningCanvasGo == null)
        {
            warningCanvasGo = new GameObject("PlasticBootWarningCanvas");
            Canvas canvas = warningCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            warningCanvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            warningCanvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject panelGo = new GameObject("PanelBackground");
            panelGo.transform.SetParent(warningCanvasGo.transform, false);
            UnityEngine.UI.Image panelImg = panelGo.AddComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0, 0, 0, 0.9f);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.sizeDelta = Vector2.zero;

            GameObject imgGo = new GameObject("WarningImage");
            imgGo.transform.SetParent(warningCanvasGo.transform, false);
            UnityEngine.UI.Image warningImg = imgGo.AddComponent<UnityEngine.UI.Image>();
            Sprite warningSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Texture_PlasticBootWarning.png");
            if (warningSprite != null) warningImg.sprite = warningSprite;
            RectTransform imgRect = imgGo.GetComponent<RectTransform>();
            imgRect.sizeDelta = new Vector2(400, 400); imgRect.anchoredPosition = new Vector2(0, 80);

            GameObject textGo = new GameObject("WarningText");
            textGo.transform.SetParent(warningCanvasGo.transform, false);
            UnityEngine.UI.Text warningText = textGo.AddComponent<UnityEngine.UI.Text>();
            warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningText.fontSize = 32; warningText.alignment = TextAnchor.MiddleCenter; warningText.color = Color.red;
            warningText.text = "Güvensiz Ankraj Noktası!\nPlastik veya dayanıksız malzemelere lanyard bağlanamaz!";
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(800, 200); textRect.anchoredPosition = new Vector2(0, -220);
        }
        manager.plasticBootWarningCanvas = warningCanvasGo;
        warningCanvasGo.SetActive(false);

        // Simulator Yüklemesi
        GameObject simulatorGo = GameObject.Find("XR Device Simulator");
        if (simulatorGo == null)
        {
            GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab");
            if (simulatorPrefab != null)
            {
                simulatorGo = PrefabUtility.InstantiatePrefab(simulatorPrefab) as GameObject;
                simulatorGo.name = "XR Device Simulator";
            }
        }

        // G. Yürüme/Işınlanma Alanlarını Sınırlandırma (Boşluktaki sahte zeminleri temizleme)
        GameObject[] allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int removedZeminCount = 0;
        foreach (var go in allGo)
        {
            if (go == null) continue;
            string nameLower = go.name.ToLower();
            if (nameLower.Contains("teleport") || nameLower.Contains("teleportation") || nameLower.Contains("walkway_plane"))
            {
                bool isUnderAllowed = false;
                Transform p = go.transform.parent;
                while (p != null)
                {
                    if (p.gameObject == scaffoldParent || p.gameObject == targetTable || p.gameObject == playerRig)
                    {
                        isUnderAllowed = true;
                        break;
                    }
                    p = p.parent;
                }
                
                if (!isUnderAllowed)
                {
                    var col = go.GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                    
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = false;
                    
                    removedZeminCount++;
                }
            }
        }
        Debug.Log($"Çevre: {removedZeminCount} adet boşlukta kalan sahte yürüme/ışınlanma zemini pasifleştirildi.");

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Senaryo Sahnesi Güncellendi", "Tüm UI hataları temizlendi, el ışınları ve dinamik tetikleyiciler başarıyla senkronize edildi!", "Tamam");
    }

    // --- ADIM GÜVENLİ HALE GETİRİLDİ: MissingComponentException ÇÖZÜLDÜ ---
    private void ConfigureDirectInteractor(GameObject handGo)
    {
        if (handGo == null) return;

        // Önce Rigidbody'yi zorla ekle ve anında referans al
        var rb = handGo.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = handGo.AddComponent<Rigidbody>();
        }

        // Null kontrolünden geçirerek isKinematic değerini ata (Exception Önleyici Güvenli Alan)
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var box = handGo.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = handGo.AddComponent<BoxCollider>();
        }

        if (box != null)
        {
            box.isTrigger = true;
            box.size = new Vector3(0.25f, 0.25f, 0.25f);
            box.center = Vector3.zero;
        }

        System.Type directType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor, Unity.XR.Interaction.Toolkit") ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRDirectInteractor, Unity.XR.Interaction.Toolkit");
        if (directType != null)
        {
            Component direct = handGo.GetComponent(directType) ?? handGo.AddComponent(directType);
            var layersField = directType.GetField("m_InteractionLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (layersField != null && direct != null)
            {
                var allLayers = new UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask();
                allLayers.value = ~0;
                layersField.SetValue(direct, allLayers);
            }
        }
    }

    private void ConfigureRayInteractor(GameObject controllerGo)
    {
        if (controllerGo == null) return;

        // XRRayInteractor Ekle/Bul
        System.Type rayType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor, Unity.XR.Interaction.Toolkit") ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
        if (rayType != null)
        {
            Component ray = controllerGo.GetComponent(rayType) ?? controllerGo.AddComponent(rayType);
            var layersField = rayType.GetField("m_InteractionLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (layersField != null && ray != null)
            {
                var allLayers = new UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask();
                allLayers.value = ~0;
                layersField.SetValue(ray, allLayers);
            }
        }

        // Kırmızı ışın ipi için LineRenderer Yapılandırması
        var lr = controllerGo.GetComponent<LineRenderer>() ?? controllerGo.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f;
        lr.endWidth = 0.005f;
        lr.enabled = true; // Işını görünür yap

        // Işın rengini kırmızı yapmak için materyal ataması
        lr.sharedMaterial = CreateOrGetMaterial("Assets/Materials/Material_RayLine.mat", Color.red, 0f, 1f);

        // XRInteractorLineVisual Ekle/Bul (Işının düzgün kırılmasını sağlar)
        System.Type lineVisualType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Visuals.XRInteractorLineVisual, Unity.XR.Interaction.Toolkit") ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual, Unity.XR.Interaction.Toolkit");
        if (lineVisualType != null && controllerGo.GetComponent(lineVisualType) == null)
        {
            controllerGo.AddComponent(lineVisualType);
        }
    }

    private void SetBoxColliderToMeshBounds(GameObject target, BoxCollider collider, float multiplier)
    {
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var filter in target.GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh != null)
            {
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 worldVertex = filter.transform.TransformPoint(vertex);
                    Vector3 localVertex = target.transform.InverseTransformPoint(worldVertex);
                    if (first) { localBounds = new Bounds(localVertex, Vector3.zero); first = false; }
                    else { localBounds.Encapsulate(localVertex); }
                }
            }
        }
        if (!first && collider != null) { collider.center = localBounds.center; collider.size = localBounds.size * multiplier; }
    }

    private Transform FindControllerTransform(Transform parent, bool isLeft)
    {
        string term = isLeft ? "left" : "right";
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLower().Contains(term) && child.name.ToLower().Contains("controller")) return child;
        }
        return null;
    }

    private void CleanChildHands(Transform parent)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (child.name.Contains("HandQuestVisual") || child.name.Contains("Instance") || child.name.Contains("Visual")) toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy) DestroyImmediate(go);
    }

    private void ConfigureControllerModel(Transform controller, GameObject handPrefab, Transform handInstance)
    {
        foreach (var c in controller.GetComponents<MonoBehaviour>())
        {
            if (c.GetType().Name == "ActionBasedController" || c.GetType().Name == "XRController")
            {
                var prefabField = c.GetType().GetField("m_ModelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prefabField != null) prefabField.SetValue(c, handPrefab);
                var parentField = c.GetType().GetField("m_ModelParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (parentField != null) parentField.SetValue(c, handInstance);
            }
        }
    }

    private Material CreateOrGetMaterial(string path, Color color, float metallic, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness);
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}