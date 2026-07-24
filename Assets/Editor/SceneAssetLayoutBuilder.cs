#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneAssetLayoutBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Yuksekten Dusme/Arrange Scene Assets")]
    public static void ArrangeSceneAssets()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var scene = SceneManager.GetActiveScene();
        ClearGeneratedSceneObjects(scene);

        var grass = LoadMaterial("Assets/Materials/grass.mat");
        var stone = LoadMaterial("Assets/Materials/mStone.mat");
        var metal = LoadMaterial("Assets/Materials/mMetal.mat");
        var black = LoadMaterial("Assets/Materials/_Black.mat");
        var logo = LoadMaterial("Assets/Materials/Logo.mat");
        var skybox = LoadMaterial("Assets/Textures/skyboxmat.mat");
        var safetyYellow = EnsureMaterial("Assets/Materials/Scene_SafetyYellow.mat", new Color(1f, 0.72f, 0.08f));
        var safetyRed = EnsureMaterial("Assets/Materials/Scene_SafetyRed.mat", new Color(0.82f, 0.12f, 0.08f));
        var safetyGreen = EnsureMaterial("Assets/Materials/Scene_SafetyGreen.mat", new Color(0.1f, 0.58f, 0.22f));
        var trainingBlue = EnsureMaterial("Assets/Materials/Scene_TrainingBlue.mat", new Color(0.05f, 0.23f, 0.62f));
        var plasticWhite = EnsureMaterial("Assets/Materials/Scene_PlasticWhite.mat", new Color(0.9f, 0.94f, 0.98f));

        UpgradeSceneMaterialsForUrp();
        ConfigureSkybox(skybox);
        RenderSettings.skybox = skybox;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.62f, 0.70f, 0.74f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0025f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.15f;

        var university = EnsureModel("university", "Assets/Models/university.fbx", scene);
        SetWorldTransform(university, new Vector3(37.4f, 0f, 210.3f), new Vector3(0f, 270f, 0f), Vector3.one);
        SetStaticRecursive(university);

        var scaffoldWasMissing = FindSceneObject("Scaffold", scene) == null;
        var scaffold = EnsureModel("Scaffold", "Assets/Models/Scaffold.fbx", scene);
        if (scaffoldWasMissing)
        {
            SetWorldTransform(scaffold, new Vector3(84f, 0.02f, 169.2f), new Vector3(0f, 270f, 0f), new Vector3(3f, 2f, 2f));
        }
        SetStaticRecursive(scaffold);
        RestoreScaffoldTriggerArtifacts(scaffold);

        var ground = EnsurePlane(scene);
        SetWorldTransform(ground, new Vector3(50f, -0.015f, 145f), Vector3.zero, new Vector3(18f, 1f, 24f));
        ApplyMaterial(ground, grass);
        SetStaticRecursive(ground);

        var table = EnsureModel("Table", "Assets/Models/Table.fbx", scene);
        SetWorldTransform(table, MapDefaultPointToScaffold(scaffold, new Vector3(112f, 0f, 129.2f)), MapDefaultEulerToScaffold(scaffold, new Vector3(0f, 180f, 0f)), new Vector3(1.75f, 1.5f, 1f));
        CleanImportedTable(table);

        var extraTable = EnsureModel("Safety Equipment Table", "Assets/Models/Table.fbx", scene);
        SetWorldTransform(extraTable, MapDefaultPointToScaffold(scaffold, new Vector3(121f, 0f, 129.2f)), MapDefaultEulerToScaffold(scaffold, new Vector3(0f, 180f, 0f)), new Vector3(1.5f, 1.35f, 0.9f));
        CleanImportedTable(extraTable);

        MoveWorkerModel(scene, scaffold);
        ArrangeLogoPlane(logo);
        ArrangeChecklist(scaffold);
        ArrangeScenario5(scaffold, stone, metal, black, safetyYellow, safetyRed, safetyGreen, trainingBlue, plasticWhite);
        ArrangeXrRig();
        ArrangeLighting();
        BuildScaffoldFrontTrainingLayout(scaffold, stone, metal, black, logo, safetyYellow, safetyRed, safetyGreen, trainingBlue, plasticWhite);
        AddUsefulColliders(scaffold, university, table, extraTable);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SampleScene assets arranged and saved.");
    }

    private static void ClearGeneratedSceneObjects(Scene scene)
    {
        DestroySceneObjectsNamed(scene, "Scene Composition Helpers");
        DestroySceneObjectsNamed(scene, "Scaffold Checklist Trigger");
        DestroySceneObjectsNamed(scene, "Scenario 5 - Mobile Scaffold PPE");
        DestroySceneObjectsNamed(scene, "Scaffold Front Training Layout");
        DestroySceneObjectsNamed(scene, "Scaffold Elevator - 3 Stop Lift");
        DestroySceneObjectsNamed(scene, "Scaffold Elevator - Full Height Lift");
    }

    private static void DestroySceneObjectsNamed(Scene scene, string objectName)
    {
        var objectsToDestroy = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                objectsToDestroy.Add(root);
            }
        }

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == objectName && go.scene == scene && !EditorUtility.IsPersistent(go) && !objectsToDestroy.Contains(go))
            {
                objectsToDestroy.Add(go);
            }
        }

        foreach (var go in objectsToDestroy)
        {
            Object.DestroyImmediate(go);
        }
    }

    private static GameObject EnsureModel(string objectName, string assetPath, Scene scene)
    {
        var existing = FindSceneObject(objectName, scene);
        if (existing != null)
        {
            existing.SetActive(true);
            existing.transform.SetParent(null);
            return existing;
        }

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            throw new System.InvalidOperationException("Missing model asset: " + assetPath);
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
        instance.name = objectName;
        return instance;
    }

    private static GameObject EnsurePlane(Scene scene)
    {
        var ground = FindSceneObject("Plane", scene);
        if (ground != null)
        {
            ground.SetActive(true);
            ground.transform.SetParent(null);
            return ground;
        }

        ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Plane";
        SceneManager.MoveGameObjectToScene(ground, scene);
        return ground;
    }

    private static void SetWorldTransform(GameObject go, Vector3 position, Vector3 euler, Vector3 scale)
    {
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
    }

    private static void ArrangeLogoPlane(Material logo)
    {
        var logoPlane = FindSceneObject("PlaneLogo", SceneManager.GetActiveScene());
        if (logoPlane == null)
        {
            logoPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            logoPlane.name = "PlaneLogo";
            SceneManager.MoveGameObjectToScene(logoPlane, SceneManager.GetActiveScene());
            logoPlane.transform.position = new Vector3(64f, 14.7f, 150f);
            logoPlane.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            logoPlane.transform.localScale = new Vector3(5.6f, 2.8f, 1f);
        }

        logoPlane.SetActive(true);
        logoPlane.transform.SetParent(null);
        if (logo != null && logo.HasProperty("_Cull"))
        {
            logo.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(logo);
        }
        ApplyMaterial(logoPlane, logo);
    }

    private static void ArrangeChecklist(GameObject scaffold)
    {
        var scene = SceneManager.GetActiveScene();
        var canvas = FindSceneObject("ChecklistCanvas", scene);
        if (canvas == null)
        {
            canvas = CreateChecklistCanvas(scene);
        }

        if (canvas != null)
        {
            canvas.SetActive(true);
            canvas.transform.SetParent(null, true);
            canvas.transform.position = MapDefaultPointToScaffold(scaffold, new Vector3(107f, 1.55f, 127.7f));
            canvas.transform.rotation = Quaternion.Euler(MapDefaultEulerToScaffold(scaffold, new Vector3(0f, 75f, 0f)));
            canvas.transform.localScale = new Vector3(0.0024f, 0.0024f, 0.0024f);

            var xrCamera = FindSceneObjectByPath("XR Origin (XR Rig)/Camera Offset/Main Camera", scene);
            var cameraComponent = xrCamera != null ? xrCamera.GetComponent<Camera>() : null;
            var canvasComponent = canvas.GetComponent<Canvas>();
            if (canvasComponent != null)
            {
                canvasComponent.worldCamera = cameraComponent;
                canvasComponent.renderMode = RenderMode.WorldSpace;
            }
        }

        var trigger = FindSceneObject("Scaffold Checklist Trigger", scene);
        if (trigger == null)
        {
            trigger = new GameObject("Scaffold Checklist Trigger");
            trigger.name = "Scaffold Checklist Trigger";
            SceneManager.MoveGameObjectToScene(trigger, scene);
        }

        trigger.name = "Scaffold Checklist Trigger";
        trigger.transform.SetParent(null, true);
        trigger.transform.position = MapDefaultPointToScaffold(scaffold, new Vector3(116.5f, 1.15f, 128.8f));
        trigger.transform.rotation = Quaternion.Euler(MapDefaultEulerToScaffold(scaffold, Vector3.zero));
        trigger.transform.localScale = new Vector3(8f, 2.3f, 6f);

        foreach (var meshCollider in trigger.GetComponents<MeshCollider>())
        {
            Object.DestroyImmediate(meshCollider);
        }

        var colliders = trigger.GetComponents<BoxCollider>();
        for (var i = colliders.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        var triggerCollider = trigger.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        GameObjectUtility.SetStaticEditorFlags(trigger, 0);

        var checklistTrigger = trigger.GetComponent<ChecklistTrigger>();
        if (checklistTrigger == null)
        {
            checklistTrigger = trigger.AddComponent<ChecklistTrigger>();
        }

        checklistTrigger.checklistCanvas = canvas;
    }

    private static void ArrangeScenario5(
        GameObject scaffold,
        Material stone,
        Material metal,
        Material black,
        Material safetyYellow,
        Material safetyRed,
        Material safetyGreen,
        Material trainingBlue,
        Material plasticWhite)
    {
        var scene = SceneManager.GetActiveScene();
        var root = new GameObject("Scenario 5 - Mobile Scaffold PPE");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.SetParent(null, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var managerObject = new GameObject("Scenario5Manager");
        managerObject.transform.SetParent(root.transform, false);
        var manager = managerObject.AddComponent<Scenario5Manager>();
        manager.requiredAnchorCapacityKg = 1000;
        manager.neutralAnchorMaterial = plasticWhite;
        manager.safeAnchorMaterial = safetyGreen;
        manager.unsafeAnchorMaterial = safetyRed;
        manager.selectedAnchorMaterial = safetyYellow;
        manager.brokenAnchorMaterial = safetyRed;

        CreateScenario5Panel(root.transform, manager, scene);
        BuildCollectiveProtectionHighlights(root.transform, safetyGreen, safetyYellow);

        var checkPad = CreateCube("Scenario5 Collective Protection Check Pad", root.transform, new Vector3(106.9f, 0.09f, 151.6f), new Vector3(2.6f, 0.12f, 2.6f), trainingBlue);
        AddScenario5Interactable(checkPad, manager, Scenario5Action.CheckCollectiveProtection);
        CreateWorldLabel("Scenario5 Check Pad Label", root.transform, "Iskele kontrol\n100 cm korkuluk\nsupurgelik", new Vector3(106.9f, 1.25f, 151.6f), Quaternion.Euler(0f, 180f, 0f), new Vector2(360f, 170f), 26);

        var harnessPickup = CreateHarnessPickup(root.transform, safetyYellow, black, metal);
        AddScenario5Interactable(harnessPickup, manager, Scenario5Action.EquipHarness);

        var equippedHarness = CreateEquippedHarnessVisual(root.transform, safetyYellow, black);
        manager.harnessVisual = equippedHarness;

        var safeAnchor = CreateCylinder("Scenario5 Safe Anchor - Steel Profile 1000kg", root.transform, new Vector3(104.6f, 5.75f, 167.6f), new Vector3(0f, 0f, 90f), new Vector3(0.11f, 1.25f, 0.11f), safetyGreen);
        var unsafeAnchor = CreateCylinder("Scenario5 Unsafe Anchor - Plastic Pipe", root.transform, new Vector3(116.2f, 5.35f, 166.8f), new Vector3(0f, 0f, 90f), new Vector3(0.1f, 1.2f, 0.1f), plasticWhite);
        AddScenario5Interactable(safeAnchor, manager, Scenario5Action.SelectSafeAnchor);
        AddScenario5Interactable(unsafeAnchor, manager, Scenario5Action.SelectUnsafeAnchor);

        var safeAnchorPoint = new GameObject("Scenario5 Safe Anchor Point").transform;
        safeAnchorPoint.SetParent(root.transform, false);
        safeAnchorPoint.position = safeAnchor.transform.position;
        manager.safeAnchorPoint = safeAnchorPoint;

        var unsafeAnchorPoint = new GameObject("Scenario5 Unsafe Anchor Point").transform;
        unsafeAnchorPoint.SetParent(root.transform, false);
        unsafeAnchorPoint.position = unsafeAnchor.transform.position;
        manager.unsafeAnchorPoint = unsafeAnchorPoint;

        manager.safeAnchorRenderers = safeAnchor.GetComponentsInChildren<Renderer>(true);
        manager.unsafeAnchorRenderers = unsafeAnchor.GetComponentsInChildren<Renderer>(true);

        manager.safeAnchorMarker = CreateCube("Scenario5 Safe Anchor Selected Marker", root.transform, safeAnchor.transform.position + new Vector3(0f, 0.55f, 0f), new Vector3(0.45f, 0.08f, 0.45f), safetyYellow);
        manager.safeAnchorMarker.SetActive(false);

        manager.unsafeAnchorBreakMarker = CreateBreakMarker(root.transform, unsafeAnchor.transform.position + new Vector3(0f, 0.5f, 0f), safetyRed);
        manager.unsafeAnchorBreakMarker.SetActive(false);

        CreateWorldLabel("Scenario5 Safe Anchor Label", root.transform, "Guvenli ankraj\ncelik profil / yasam hatti\n1000 kg", safeAnchor.transform.position + new Vector3(0f, 0.95f, -0.1f), Quaternion.Euler(0f, 180f, 0f), new Vector2(430f, 170f), 24);
        CreateWorldLabel("Scenario5 Unsafe Anchor Label", root.transform, "Guvensiz nokta\nplastik boru", unsafeAnchor.transform.position + new Vector3(0f, 0.95f, -0.1f), Quaternion.Euler(0f, 180f, 0f), new Vector2(360f, 145f), 24);

        var fallDummy = CreateFallDummy(root.transform, safetyRed, safetyYellow, black);
        manager.fallDummy = fallDummy.transform;

        var lanyardStart = new GameObject("Scenario5 Lanyard Start Point").transform;
        lanyardStart.SetParent(fallDummy.transform, false);
        lanyardStart.localPosition = new Vector3(0f, 0.62f, -0.04f);
        manager.lanyardStartPoint = lanyardStart;

        var fallTarget = new GameObject("Scenario5 Fall Target").transform;
        fallTarget.SetParent(root.transform, false);
        fallTarget.position = new Vector3(112.4f, 1.05f, 167.4f);
        manager.fallTarget = fallTarget;

        var lanyardLine = new GameObject("Scenario5 Lanyard Line");
        SceneManager.MoveGameObjectToScene(lanyardLine, scene);
        lanyardLine.transform.SetParent(root.transform, false);
        var lineRenderer = lanyardLine.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.045f;
        lineRenderer.endWidth = 0.045f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sharedMaterial = safetyYellow;
        lanyardLine.SetActive(false);
        manager.lanyardLine = lineRenderer;

        var anchorChoicePanel = CreateAnchorChoicePanel(root.transform, manager, scene);
        anchorChoicePanel.transform.position = new Vector3(110.5f, 5.55f, 161.9f);
        anchorChoicePanel.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        if (scaffold != null)
        {
            CreateWorldLabel("Scenario5 Scenario Label", root.transform, "Senaryo 5\nMobil Iskele ve KKD", new Vector3(109.2f, 3.2f, 150.25f), Quaternion.Euler(0f, 180f, 0f), new Vector2(430f, 170f), 30);
        }

        ConformScenario5RootToScaffold(root.transform, scaffold);
    }

    private static void ConformScenario5RootToScaffold(Transform scenarioRoot, GameObject scaffold)
    {
        if (scenarioRoot == null || scaffold == null)
        {
            return;
        }

        var defaultScaffoldPosition = new Vector3(110f, 0.02f, 169.2f);
        var defaultScaffoldRotation = Quaternion.Euler(0f, 270f, 0f);
        var scaffoldRotationDelta = scaffold.transform.rotation * Quaternion.Inverse(defaultScaffoldRotation);

        scenarioRoot.rotation = scaffoldRotationDelta;
        scenarioRoot.position = scaffold.transform.position - scaffoldRotationDelta * defaultScaffoldPosition;
    }

    private static Vector3 MapDefaultPointToScaffold(GameObject scaffold, Vector3 defaultWorldPosition)
    {
        if (scaffold == null)
        {
            return defaultWorldPosition;
        }

        var defaultScaffoldPosition = new Vector3(110f, 0.02f, 169.2f);
        var scaffoldRotationDelta = GetScaffoldRotationDelta(scaffold);
        return scaffold.transform.position + scaffoldRotationDelta * (defaultWorldPosition - defaultScaffoldPosition);
    }

    private static Vector3 MapDefaultEulerToScaffold(GameObject scaffold, Vector3 defaultEuler)
    {
        if (scaffold == null)
        {
            return defaultEuler;
        }

        return (GetScaffoldRotationDelta(scaffold) * Quaternion.Euler(defaultEuler)).eulerAngles;
    }

    private static Quaternion GetScaffoldRotationDelta(GameObject scaffold)
    {
        var defaultScaffoldRotation = Quaternion.Euler(0f, 270f, 0f);
        return scaffold.transform.rotation * Quaternion.Inverse(defaultScaffoldRotation);
    }

    private static void CreateScenario5Panel(Transform root, Scenario5Manager manager, Scene scene)
    {
        var canvasObject = new GameObject("Scenario5Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        canvasObject.transform.SetParent(root, false);
        canvasObject.transform.position = new Vector3(122.7f, 1.65f, 134.3f);
        canvasObject.transform.rotation = Quaternion.Euler(0f, 235f, 0f);
        canvasObject.transform.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);

        var rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1120f, 720f);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;
        AddWorldCanvasRaycaster(canvasObject);

        var panel = CreateUiRect("Panel", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.055f, 0.065f, 0.94f);

        manager.titleText = CreateText("Title", panel.transform, "Senaryo 5: Mobil Iskele ve KKD Disiplini", 32, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), new Vector2(-80f, 60f));
        manager.titleText.alignment = TextAnchor.MiddleLeft;

        manager.stepText = CreateText("StepText", panel.transform, "", 27, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -118f), new Vector2(-80f, 70f));
        manager.stepText.alignment = TextAnchor.MiddleLeft;

        manager.metricsText = CreateText("MetricsText", panel.transform, "", 23, FontStyle.Normal, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, -42f), new Vector2(-92f, -250f));
        manager.metricsText.alignment = TextAnchor.MiddleLeft;

        manager.collectiveProtectionButton = CreateButton(panel.transform, "Scenario5CollectiveButton", "1 Iskele kontrol", new Vector2(-395f, -292f), new Vector2(250f, 58f));
        manager.harnessButton = CreateButton(panel.transform, "Scenario5HarnessButton", "2 Kemer + lanyard", new Vector2(-130f, -292f), new Vector2(250f, 58f));
        manager.safeAnchorButton = CreateButton(panel.transform, "Scenario5SafeAnchorButton", "3 Guvenli ankraj", new Vector2(135f, -292f), new Vector2(250f, 58f));
        manager.unsafeAnchorButton = CreateButton(panel.transform, "Scenario5UnsafeAnchorButton", "Plastik boru", new Vector2(400f, -292f), new Vector2(250f, 58f));
        manager.resetButton = CreateButton(panel.transform, "Scenario5ResetButton", "Sifirla", new Vector2(0f, -352f), new Vector2(220f, 52f));

        TintButton(manager.collectiveProtectionButton, new Color(0.08f, 0.26f, 0.58f, 1f), Color.white);
        TintButton(manager.harnessButton, new Color(0.88f, 0.63f, 0.08f, 1f), Color.black);
        TintButton(manager.safeAnchorButton, new Color(0.1f, 0.56f, 0.22f, 1f), Color.white);
        TintButton(manager.unsafeAnchorButton, new Color(0.74f, 0.12f, 0.09f, 1f), Color.white);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(manager.collectiveProtectionButton.onClick, manager.CheckCollectiveProtection);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(manager.harnessButton.onClick, manager.EquipHarness);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(manager.safeAnchorButton.onClick, manager.SelectSafeAnchor);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(manager.unsafeAnchorButton.onClick, manager.SelectUnsafeAnchor);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(manager.resetButton.onClick, manager.ResetScenario);
    }

    private static GameObject CreateAnchorChoicePanel(Transform root, Scenario5Manager manager, Scene scene)
    {
        var canvasObject = new GameObject("Scenario5AnchorChoiceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        canvasObject.transform.SetParent(root, false);
        canvasObject.transform.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        var rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(620f, 240f);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;
        AddWorldCanvasRaycaster(canvasObject);

        var panel = CreateUiRect("Panel", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.055f, 0.065f, 0.88f);

        CreateText("Title", panel.transform, "Ankraj noktasini sec", 29, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-50f, 54f));
        var safeButton = CreateButton(panel.transform, "TopSafeAnchorButton", "Celik profil", new Vector2(-155f, -92f), new Vector2(250f, 62f));
        var unsafeButton = CreateButton(panel.transform, "TopUnsafeAnchorButton", "Plastik boru", new Vector2(155f, -92f), new Vector2(250f, 62f));
        TintButton(safeButton, new Color(0.1f, 0.56f, 0.22f, 1f), Color.white);
        TintButton(unsafeButton, new Color(0.74f, 0.12f, 0.09f, 1f), Color.white);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(safeButton.onClick, manager.SelectSafeAnchor);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(unsafeButton.onClick, manager.SelectUnsafeAnchor);
        return canvasObject;
    }

    private static void BuildCollectiveProtectionHighlights(Transform root, Material safetyGreen, Material safetyYellow)
    {
        CreateCube("Scenario5 Guardrail 100cm Gauge", root, new Vector3(102.3f, 5.08f, 166.4f), new Vector3(0.08f, 1f, 0.08f), safetyGreen);
        CreateCube("Scenario5 Guardrail Top Highlight", root, new Vector3(107.2f, 5.58f, 166.4f), new Vector3(9.8f, 0.08f, 0.08f), safetyGreen);
        CreateCube("Scenario5 Guardrail Mid Highlight", root, new Vector3(107.2f, 5.12f, 166.4f), new Vector3(9.8f, 0.07f, 0.07f), safetyYellow);
        CreateCube("Scenario5 Toe Board Highlight", root, new Vector3(107.2f, 4.62f, 166.4f), new Vector3(9.8f, 0.16f, 0.08f), safetyYellow);
        CreateWorldLabel("Scenario5 Guardrail Label", root, "100 cm korkuluk\nsupurgelik kontrolu", new Vector3(103.8f, 6.35f, 166.2f), Quaternion.Euler(0f, 180f, 0f), new Vector2(390f, 140f), 23);
    }

    private static GameObject CreateHarnessPickup(Transform root, Material safetyYellow, Material black, Material metal)
    {
        var pickup = new GameObject("Scenario5 Harness And Lanyard Pickup");
        pickup.transform.SetParent(root, false);
        pickup.transform.position = new Vector3(121.2f, 1.04f, 129.25f);
        pickup.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CreateCube("Harness Back Strap", pickup.transform, pickup.transform.position + new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.9f, 0.06f), safetyYellow);
        CreateCube("Harness Left Strap", pickup.transform, pickup.transform.position + new Vector3(-0.25f, 0.08f, 0f), new Vector3(0.1f, 0.75f, 0.06f), safetyYellow);
        CreateCube("Harness Right Strap", pickup.transform, pickup.transform.position + new Vector3(0.25f, 0.08f, 0f), new Vector3(0.1f, 0.75f, 0.06f), safetyYellow);
        CreateCube("Harness Waist Belt", pickup.transform, pickup.transform.position + new Vector3(0f, -0.18f, 0f), new Vector3(0.72f, 0.1f, 0.08f), black);
        CreateCylinder("Lanyard Coil", pickup.transform, pickup.transform.position + new Vector3(0.85f, -0.2f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.18f, 0.04f, 0.18f), metal);

        var collider = pickup.AddComponent<BoxCollider>();
        collider.size = new Vector3(1.8f, 1.2f, 0.7f);
        collider.center = new Vector3(0f, 0f, 0f);

        CreateWorldLabel("Scenario5 Harness Pickup Label", root, "Tam vucut emniyet kemeri\nve lanyard", new Vector3(121.2f, 2.05f, 129.25f), Quaternion.Euler(0f, 180f, 0f), new Vector2(420f, 150f), 25);
        return pickup;
    }

    private static GameObject CreateEquippedHarnessVisual(Transform root, Material safetyYellow, Material black)
    {
        var harness = new GameObject("Scenario5 Equipped Harness Visual");
        harness.transform.SetParent(root, false);
        harness.transform.position = new Vector3(112.4f, 5.45f, 167.4f);
        harness.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CreateCube("Equipped Chest Strap", harness.transform, harness.transform.position + new Vector3(0f, 0.24f, 0f), new Vector3(0.72f, 0.08f, 0.08f), safetyYellow);
        CreateCube("Equipped Waist Belt", harness.transform, harness.transform.position + new Vector3(0f, -0.15f, 0f), new Vector3(0.82f, 0.09f, 0.08f), black);
        CreateCube("Equipped Left Shoulder", harness.transform, harness.transform.position + new Vector3(-0.25f, 0.1f, 0f), new Vector3(0.08f, 0.65f, 0.08f), safetyYellow);
        CreateCube("Equipped Right Shoulder", harness.transform, harness.transform.position + new Vector3(0.25f, 0.1f, 0f), new Vector3(0.08f, 0.65f, 0.08f), safetyYellow);
        harness.SetActive(false);
        return harness;
    }

    private static GameObject CreateFallDummy(Transform root, Material safetyRed, Material safetyYellow, Material black)
    {
        var dummy = new GameObject("Scenario5 Fall Simulation Dummy");
        dummy.transform.SetParent(root, false);
        dummy.transform.position = new Vector3(112.4f, 5.2f, 167.4f);
        dummy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CreateCube("Dummy Body", dummy.transform, dummy.transform.position + new Vector3(0f, 0.28f, 0f), new Vector3(0.48f, 0.72f, 0.22f), safetyYellow);
        CreateCube("Dummy Head", dummy.transform, dummy.transform.position + new Vector3(0f, 0.82f, 0f), new Vector3(0.28f, 0.28f, 0.28f), safetyRed);
        CreateCube("Dummy Left Leg", dummy.transform, dummy.transform.position + new Vector3(-0.14f, -0.34f, 0f), new Vector3(0.13f, 0.65f, 0.14f), black);
        CreateCube("Dummy Right Leg", dummy.transform, dummy.transform.position + new Vector3(0.14f, -0.34f, 0f), new Vector3(0.13f, 0.65f, 0.14f), black);
        CreateCube("Dummy Left Arm", dummy.transform, dummy.transform.position + new Vector3(-0.38f, 0.25f, 0f), new Vector3(0.12f, 0.58f, 0.12f), safetyRed);
        CreateCube("Dummy Right Arm", dummy.transform, dummy.transform.position + new Vector3(0.38f, 0.25f, 0f), new Vector3(0.12f, 0.58f, 0.12f), safetyRed);
        return dummy;
    }

    private static GameObject CreateBreakMarker(Transform root, Vector3 position, Material safetyRed)
    {
        var marker = new GameObject("Scenario5 Weak Anchor Break Marker");
        marker.transform.SetParent(root, false);
        marker.transform.position = position;
        CreateCube("Break Slash A", marker.transform, position, new Vector3(0.08f, 0.7f, 0.08f), safetyRed).transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        CreateCube("Break Slash B", marker.transform, position, new Vector3(0.08f, 0.7f, 0.08f), safetyRed).transform.rotation = Quaternion.Euler(0f, 0f, -45f);
        return marker;
    }

    private static void AddScenario5Interactable(GameObject target, Scenario5Manager manager, Scenario5Action action)
    {
        if (target == null)
        {
            return;
        }

        var interactable = target.GetComponent<Scenario5Interactable>();
        if (interactable == null)
        {
            interactable = target.AddComponent<Scenario5Interactable>();
        }

        interactable.manager = manager;
        interactable.action = action;
        interactable.allowMouseClick = true;
        interactable.triggerOnPlayerEnter = false;
        interactable.oneShot = false;
    }

    private static GameObject CreateWorldLabel(string name, Transform parent, string value, Vector3 position, Quaternion rotation, Vector2 sizeDelta, int fontSize)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, SceneManager.GetActiveScene());
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.position = position;
        canvasObject.transform.rotation = rotation;
        canvasObject.transform.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        var rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = sizeDelta;

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;

        var panel = CreateUiRect("Panel", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var image = panel.AddComponent<Image>();
        image.color = new Color(0.04f, 0.055f, 0.065f, 0.86f);

        var text = CreateText("Text", panel.transform, value, fontSize, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, -18f));
        text.alignment = TextAnchor.MiddleCenter;
        return canvasObject;
    }

    private static void AddWorldCanvasRaycaster(GameObject canvasObject)
    {
        var raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (raycasterType != null)
        {
            canvasObject.AddComponent(raycasterType);
        }
        else
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void TintButton(Button button, Color background, Color textColor)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = background;
        }

        var label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = textColor;
        }
    }

    private static GameObject CreateChecklistCanvas(Scene scene)
    {
        var canvasObject = new GameObject("ChecklistCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(ChecklistManager));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        var rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(940f, 520f);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;

        var raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (raycasterType != null)
        {
            canvasObject.AddComponent(raycasterType);
        }
        else
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        var panel = CreateUiRect("Panel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.08f, 0.9f);

        CreateText("Title", panel.transform, "ISG Kontrol Listesi", 32, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -45f), new Vector2(-80f, 60f));

        var toggles = new Toggle[4];
        toggles[0] = CreateToggle(panel.transform, "Toggle", "Baret takildi", -125f);
        toggles[1] = CreateToggle(panel.transform, "Toggle1", "Emniyet kemeri kontrol edildi", -55f);
        toggles[2] = CreateToggle(panel.transform, "Toggle2", "Iskele zemini kontrol edildi", 15f);
        toggles[3] = CreateToggle(panel.transform, "Toggle3", "Korkuluklar kontrol edildi", 85f);

        var backButton = CreateButton(panel.transform, "BackButton", "Geri", new Vector2(-185f, -205f), new Vector2(220f, 58f));
        var okayButton = CreateButton(panel.transform, "ForwardButton", "Tamam", new Vector2(185f, -205f), new Vector2(220f, 58f));

        var manager = canvasObject.GetComponent<ChecklistManager>();
        manager.toggles = toggles;
        manager.okayButton = okayButton;

        foreach (var toggle in toggles)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(toggle.onValueChanged, delegate { manager.CheckAllToggles(); });
        }

        UnityEditor.Events.UnityEventTools.AddPersistentListener(okayButton.onClick, manager.OnClickOkay);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(backButton.onClick, manager.OnClickBack);

        return canvasObject;
    }

    private static GameObject CreateUiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return go;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = CreateUiRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        var text = go.AddComponent<Text>();
        text.text = value;
        text.font = GetDefaultFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private static Toggle CreateToggle(Transform parent, string name, string label, float y)
    {
        var root = CreateUiRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 54f));
        var toggle = root.AddComponent<Toggle>();

        var background = CreateUiRect("Background", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(34f, 34f));
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.22f, 1f);

        var checkmark = CreateUiRect("Checkmark", background.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
        var checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.color = new Color(1f, 0.72f, 0.08f, 1f);

        CreateText("Label", root.transform, label, 25, FontStyle.Normal, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(70f, 0f), new Vector2(-90f, 0f)).alignment = TextAnchor.MiddleLeft;

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        toggle.isOn = false;
        return toggle;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var root = CreateUiRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, sizeDelta);
        var image = root.AddComponent<Image>();
        image.color = name == "ForwardButton" ? new Color(1f, 0.72f, 0.08f, 1f) : new Color(0.2f, 0.22f, 0.24f, 1f);
        var button = root.AddComponent<Button>();
        button.targetGraphic = image;
        CreateText("Text", root.transform, label, 24, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).color = name == "ForwardButton" ? Color.black : Color.white;
        return button;
    }

    private static Font GetDefaultFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void ArrangeXrRig()
    {
        var scene = SceneManager.GetActiveScene();
        var xrOrigin = FindSceneObject("XR Origin (XR Rig)", scene);
        if (xrOrigin != null)
        {
            xrOrigin.tag = "Player";
        }

        var cameraOffset = FindSceneObjectByPath("XR Origin (XR Rig)/Camera Offset", scene);
        if (cameraOffset != null)
        {
            cameraOffset.tag = "Untagged";
            cameraOffset.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cameraOffset.transform.localRotation = Quaternion.identity;
            cameraOffset.transform.localScale = Vector3.one;
        }

        var xrCamera = FindSceneObjectByPath("XR Origin (XR Rig)/Camera Offset/Main Camera", scene);
        if (xrCamera != null)
        {
            xrCamera.tag = "MainCamera";
            xrCamera.transform.localPosition = Vector3.zero;
            xrCamera.transform.localRotation = Quaternion.identity;
            xrCamera.transform.localScale = Vector3.one;
            var cam = xrCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = 68f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 600f;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
        }

        var xrManager = FindSceneObject("XR Interaction Manager", scene);
        if (xrManager != null)
        {
            xrManager.transform.position = new Vector3(125f, 0f, 128.6f);
        }

        var rootCamera = FindRootMainCamera(scene);
        if (rootCamera != null)
        {
            rootCamera.name = "Overview Camera (disabled)";
            rootCamera.tag = "Untagged";
            rootCamera.SetActive(false);
        }
    }

    private static void ArrangeLighting()
    {
        var lightObject = FindSceneObject("Directional Light", SceneManager.GetActiveScene());
        if (lightObject == null)
        {
            lightObject = new GameObject("Directional Light");
            lightObject.AddComponent<Light>().type = LightType.Directional;
            SceneManager.MoveGameObjectToScene(lightObject, SceneManager.GetActiveScene());
        }

        lightObject.transform.position = new Vector3(35f, 45f, 115f);
        lightObject.transform.rotation = Quaternion.Euler(46f, 322f, 0f);
        var light = lightObject.GetComponent<Light>();
        if (light != null)
        {
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.95f, 0.86f, 1f);
            light.shadows = LightShadows.Soft;
        }
    }

    private static void BuildScaffoldFrontTrainingLayout(
        GameObject scaffold,
        Material stone,
        Material metal,
        Material black,
        Material logo,
        Material safetyYellow,
        Material safetyRed,
        Material safetyGreen,
        Material trainingBlue,
        Material plasticWhite)
    {
        var layoutRoot = new GameObject("Scaffold Front Training Layout");
        SceneManager.MoveGameObjectToScene(layoutRoot, SceneManager.GetActiveScene());
        layoutRoot.transform.SetParent(null, false);

        var layout = layoutRoot.transform;
        CreateCube("Inspection Walkway", layout, new Vector3(112f, 0.025f, 143.6f), new Vector3(10f, 0.05f, 40f), stone);
        CreateCube("Inspection Station Pad", layout, new Vector3(116.5f, 0.035f, 128.8f), new Vector3(17f, 0.07f, 7f), stone);

        CreateBarrierRun(layout, new Vector3(101f, 0.75f, 156.6f), new Vector3(0.18f, 1.5f, 14f), safetyYellow);
        CreateBarrierRun(layout, new Vector3(101f, 0.75f, 178.1f), new Vector3(0.18f, 1.5f, 12.6f), safetyYellow);
        CreateBarrierRun(layout, new Vector3(120f, 0.75f, 167f), new Vector3(0.18f, 1.5f, 35f), safetyYellow);
        CreateBarrierRun(layout, new Vector3(110.5f, 0.75f, 149.6f), new Vector3(19f, 1.5f, 0.18f), safetyYellow);
        CreateBarrierRun(layout, new Vector3(110.5f, 0.75f, 184.4f), new Vector3(19f, 1.5f, 0.18f), safetyYellow);

        CreatePost(layout, new Vector3(101f, 0.8f, 149.6f), safetyRed);
        CreatePost(layout, new Vector3(120f, 0.8f, 149.6f), safetyRed);
        CreatePost(layout, new Vector3(101f, 0.8f, 184.4f), safetyRed);
        CreatePost(layout, new Vector3(120f, 0.8f, 184.4f), safetyRed);
        CreatePost(layout, new Vector3(101f, 0.8f, 167f), safetyYellow);
        CreatePost(layout, new Vector3(120f, 0.8f, 167f), safetyYellow);

        var signBack = CreateCube("Campus Logo Sign Back", layout, new Vector3(107f, 1.25f, 126.9f), new Vector3(3.2f, 1.7f, 0.08f), black);
        signBack.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var sign = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sign.name = "Campus Logo Sign";
        sign.transform.SetParent(layout, false);
        sign.transform.position = new Vector3(107f, 1.3f, 126.84f);
        sign.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        sign.transform.localScale = new Vector3(2.65f, 1.05f, 1f);
        ApplyMaterial(sign, logo);

        CreateCube("Sign Stand", layout, new Vector3(107f, 0.62f, 126.96f), new Vector3(0.18f, 1.2f, 0.18f), metal);
        CreateCube("Sign Foot", layout, new Vector3(107f, 0.06f, 126.96f), new Vector3(1.6f, 0.12f, 0.7f), metal);
        CreateScaffoldElevator(layout, stone, metal, black, safetyYellow, safetyGreen, trainingBlue, plasticWhite);

        ConformScenario5RootToScaffold(layoutRoot.transform, scaffold);
    }

    private static void CreateScaffoldElevator(
        Transform root,
        Material stone,
        Material metal,
        Material black,
        Material safetyYellow,
        Material safetyGreen,
        Material trainingBlue,
        Material plasticWhite)
    {
        var elevatorRoot = new GameObject("Scaffold Elevator - Full Height Lift");
        elevatorRoot.transform.SetParent(root, false);

        var center = new Vector3(110f, 0f, 151.2f);
        var stops = new[] { 0.28f, 2.45f, 4.65f, 6.85f, 9.05f, 11.25f, 13.45f, 15.65f };
        var lowerStop = stops[0];
        var upperStop = stops[stops.Length - 1];

        CreateCube("Elevator Base Pad", elevatorRoot.transform, new Vector3(center.x, 0.04f, center.z), new Vector3(2.8f, 0.08f, 3.1f), stone);
        CreateElevatorShaft(elevatorRoot.transform, center, stops, metal, safetyYellow);
        CreateElevatorLandings(elevatorRoot.transform, center, stops, stone, metal, safetyYellow);

        var cab = new GameObject("Elevator Moving Cab");
        cab.transform.SetParent(elevatorRoot.transform, false);
        cab.transform.position = new Vector3(center.x, lowerStop, center.z);

        var sensor = cab.AddComponent<BoxCollider>();
        sensor.isTrigger = true;
        sensor.center = new Vector3(0f, 0.95f, 0f);
        sensor.size = new Vector3(1.9f, 2.1f, 2.45f);

        var body = cab.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        var elevator = cab.AddComponent<ScaffoldElevator>();
        elevator.localStops = stops;
        elevator.speed = 1.2f;
        elevator.allowKeyboardControls = false;

        CreateElevatorCab(cab.transform, elevator, center, lowerStop, metal, black, safetyYellow, safetyGreen, trainingBlue, plasticWhite);
        CreateElevatorControlPanel(cab.transform, elevator, center, lowerStop);
        SaveElevatorPrefab(elevatorRoot);
    }

    private static void CreateElevatorShaft(Transform root, Vector3 center, float[] stops, Material metal, Material safetyYellow)
    {
        var upperStop = stops[stops.Length - 1];
        var shaftHeight = upperStop + 1.15f;
        var shaftCenterY = shaftHeight * 0.5f;
        var xOffset = 1.05f;
        var zOffset = 1.35f;

        CreateCube("Elevator Shaft Post FL", root, new Vector3(center.x - xOffset, shaftCenterY, center.z - zOffset), new Vector3(0.12f, shaftHeight, 0.12f), metal);
        CreateCube("Elevator Shaft Post FR", root, new Vector3(center.x + xOffset, shaftCenterY, center.z - zOffset), new Vector3(0.12f, shaftHeight, 0.12f), metal);
        CreateCube("Elevator Shaft Post BL", root, new Vector3(center.x - xOffset, shaftCenterY, center.z + zOffset), new Vector3(0.12f, shaftHeight, 0.12f), metal);
        CreateCube("Elevator Shaft Post BR", root, new Vector3(center.x + xOffset, shaftCenterY, center.z + zOffset), new Vector3(0.12f, shaftHeight, 0.12f), metal);

        var topY = upperStop + 1.15f;
        CreateCube("Elevator Top Beam Front", root, new Vector3(center.x, topY, center.z - zOffset), new Vector3(2.3f, 0.12f, 0.12f), safetyYellow);
        CreateCube("Elevator Top Beam Back", root, new Vector3(center.x, topY, center.z + zOffset), new Vector3(2.3f, 0.12f, 0.12f), safetyYellow);
        CreateCube("Elevator Top Beam Left", root, new Vector3(center.x - xOffset, topY, center.z), new Vector3(0.12f, 0.12f, 2.8f), safetyYellow);
        CreateCube("Elevator Top Beam Right", root, new Vector3(center.x + xOffset, topY, center.z), new Vector3(0.12f, 0.12f, 2.8f), safetyYellow);

        foreach (var stop in stops)
        {
            var crossbarY = stop + 0.95f;
            CreateCube("Elevator Shaft Crossbar Front", root, new Vector3(center.x, crossbarY, center.z - zOffset), new Vector3(2.25f, 0.08f, 0.08f), metal);
            CreateCube("Elevator Shaft Crossbar Back", root, new Vector3(center.x, crossbarY, center.z + zOffset), new Vector3(2.25f, 0.08f, 0.08f), metal);
        }
    }

    private static void CreateElevatorLandings(Transform root, Vector3 center, float[] stops, Material stone, Material metal, Material safetyYellow)
    {
        for (var i = 0; i < stops.Length; i++)
        {
            var stopY = stops[i];
            var landing = CreateCube("Elevator Landing " + (i + 1), root, new Vector3(center.x + 1.65f, stopY - 0.03f, center.z), new Vector3(1.75f, 0.12f, 2.2f), stone);
            landing.isStatic = true;

            CreateCube("Elevator Landing Toe Board " + (i + 1), root, new Vector3(center.x + 1.65f, stopY + 0.22f, center.z - 1.08f), new Vector3(1.75f, 0.18f, 0.08f), safetyYellow);
            CreateCube("Elevator Landing Guardrail " + (i + 1), root, new Vector3(center.x + 1.65f, stopY + 1.05f, center.z + 1.08f), new Vector3(1.75f, 0.08f, 0.08f), metal);
        }
    }

    private static void CreateElevatorCab(
        Transform cab,
        ScaffoldElevator elevator,
        Vector3 center,
        float lowerStop,
        Material metal,
        Material black,
        Material safetyYellow,
        Material safetyGreen,
        Material trainingBlue,
        Material plasticWhite)
    {
        CreateCube("Elevator Cab Floor", cab, new Vector3(center.x, lowerStop, center.z), new Vector3(1.75f, 0.16f, 2.2f), black);
        CreateCube("Elevator Cab Left Rail", cab, new Vector3(center.x - 0.86f, lowerStop + 0.95f, center.z), new Vector3(0.1f, 1.9f, 2.1f), metal);
        CreateCube("Elevator Cab Front Rail", cab, new Vector3(center.x, lowerStop + 0.95f, center.z - 1.05f), new Vector3(1.75f, 1.9f, 0.1f), metal);
        CreateCube("Elevator Cab Back Rail", cab, new Vector3(center.x, lowerStop + 0.95f, center.z + 1.05f), new Vector3(1.75f, 1.9f, 0.1f), metal);
        CreateCube("Elevator Cab Top Frame", cab, new Vector3(center.x, lowerStop + 1.95f, center.z), new Vector3(1.85f, 0.1f, 2.25f), safetyYellow);

        var upPad = CreateCube("Elevator Floor Button Up", cab, new Vector3(center.x - 0.36f, lowerStop + 0.13f, center.z - 0.48f), new Vector3(0.46f, 0.05f, 0.46f), safetyGreen);
        ConfigureElevatorCallButton(upPad, elevator, true);

        var downPad = CreateCube("Elevator Floor Button Down", cab, new Vector3(center.x + 0.36f, lowerStop + 0.13f, center.z - 0.48f), new Vector3(0.46f, 0.05f, 0.46f), trainingBlue);
        ConfigureElevatorCallButton(downPad, elevator, false);

        CreateCube("Elevator Direction Arrow Up", cab, new Vector3(center.x - 0.36f, lowerStop + 0.18f, center.z - 0.48f), new Vector3(0.12f, 0.03f, 0.32f), plasticWhite);
        CreateCube("Elevator Direction Arrow Down", cab, new Vector3(center.x + 0.36f, lowerStop + 0.18f, center.z - 0.48f), new Vector3(0.12f, 0.03f, 0.32f), safetyYellow);
    }

    private static void ConfigureElevatorCallButton(GameObject buttonObject, ScaffoldElevator elevator, bool moveUp)
    {
        var collider = buttonObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        var callButton = buttonObject.AddComponent<ScaffoldElevatorCallButton>();
        callButton.elevator = elevator;
        callButton.moveUp = moveUp;
        callButton.triggerOnPlayerEnter = true;
        callButton.allowMouseClick = true;
    }

    private static void CreateElevatorControlPanel(Transform parent, ScaffoldElevator elevator, Vector3 center, float lowerStop)
    {
        var canvasObject = new GameObject("Elevator Control Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, SceneManager.GetActiveScene());
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.position = new Vector3(center.x - 0.82f, lowerStop + 1.25f, center.z - 0.72f);
        canvasObject.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
        canvasObject.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);

        var rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 300f);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;
        AddWorldCanvasRaycaster(canvasObject);

        var panel = CreateUiRect("Panel", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var image = panel.AddComponent<Image>();
        image.color = new Color(0.04f, 0.055f, 0.065f, 0.9f);

        CreateText("Title", panel.transform, "ASANSOR", 32, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-34f, 58f));
        var upButton = CreateButton(panel.transform, "ElevatorUpButton", "YUKARI", new Vector2(0f, 86f), new Vector2(250f, 70f));
        var downButton = CreateButton(panel.transform, "ElevatorDownButton", "ASAGI", new Vector2(0f, 8f), new Vector2(250f, 70f));

        TintButton(upButton, new Color(0.1f, 0.56f, 0.22f, 1f), Color.white);
        TintButton(downButton, new Color(0.05f, 0.23f, 0.62f, 1f), Color.white);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(upButton.onClick, elevator.MoveUp);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(downButton.onClick, elevator.MoveDown);
    }

    private static void SaveElevatorPrefab(GameObject elevatorRoot)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(elevatorRoot, "Assets/Prefabs/ScaffoldElevator.prefab");
    }

    private static void CreateBarrierRun(Transform root, Vector3 position, Vector3 scale, Material material)
    {
        var lower = CreateCube("Safety Rail Lower", root, position + Vector3.down * 0.35f, scale, material);
        lower.transform.localScale = new Vector3(scale.x, 0.14f, scale.z);

        var upper = CreateCube("Safety Rail Upper", root, position + Vector3.up * 0.35f, scale, material);
        upper.transform.localScale = new Vector3(scale.x, 0.14f, scale.z);
    }

    private static void CreatePost(Transform root, Vector3 position, Material material)
    {
        CreateCube("Safety Rail Post", root, position, new Vector3(0.28f, 1.6f, 0.28f), material);
    }

    private static GameObject CreateCube(string name, Transform root, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = scale;
        ApplyMaterial(go, material);
        return go;
    }

    private static GameObject CreateCylinder(string name, Transform root, Vector3 position, Vector3 euler, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        ApplyMaterial(go, material);
        return go;
    }

    private static void MoveWorkerModel(Scene scene, GameObject scaffold)
    {
        var worker = FindSceneObject("Worker Inspector", scene) ?? FindSceneObject("worker2", scene);
        if (worker == null)
        {
            return;
        }

        worker.name = "Worker Inspector";
        worker.transform.SetParent(null, true);
        worker.transform.position = MapDefaultPointToScaffold(scaffold, new Vector3(108f, 0f, 132.1f));
        worker.transform.rotation = Quaternion.Euler(MapDefaultEulerToScaffold(scaffold, new Vector3(0f, 250f, 0f)));
        worker.transform.localScale = Vector3.one;

        if (PrefabUtility.IsPartOfPrefabInstance(worker))
        {
            PrefabUtility.UnpackPrefabInstance(worker, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
    }

    private static void AddUsefulColliders(params GameObject[] targets)
    {
        foreach (var target in targets)
        {
            if (target == null)
            {
                continue;
            }

            var filters = target.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    collider = filter.gameObject.AddComponent<MeshCollider>();
                }

                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                collider.isTrigger = false;
            }
        }
    }

    private static void CleanImportedTable(GameObject tableRoot)
    {
        if (tableRoot == null)
        {
            return;
        }

        for (var i = tableRoot.transform.childCount - 1; i >= 0; i--)
        {
            var child = tableRoot.transform.GetChild(i).gameObject;
            if (child.GetComponent<Camera>() != null || child.GetComponent<Light>() != null)
            {
                Object.DestroyImmediate(child);
            }
        }

        var tableMeshRoot = FindDirectChild(tableRoot.transform, "Table");
        if (tableMeshRoot == null)
        {
            return;
        }

        tableMeshRoot.transform.localPosition = Vector3.zero;
        tableMeshRoot.transform.localRotation = Quaternion.Euler(270.019775f, 0f, 0f);
        tableMeshRoot.transform.localScale = Vector3.one * 26.881063f;
    }

    private static void RestoreScaffoldTriggerArtifacts(GameObject scaffold)
    {
        if (scaffold == null)
        {
            return;
        }

        foreach (var trigger in scaffold.GetComponentsInChildren<ChecklistTrigger>(true))
        {
            var go = trigger.gameObject;
            Object.DestroyImmediate(trigger);

            foreach (var box in go.GetComponents<BoxCollider>())
            {
                Object.DestroyImmediate(box);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }

            if (go.name == "Scaffold Checklist Trigger")
            {
                go.name = "Cube";
            }
        }
    }

    private static void SetStaticRecursive(GameObject root)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(transform.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static Material EnsureMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = System.IO.Path.GetFileNameWithoutExtension(path);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void UpgradeSceneMaterialsForUrp()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            return;
        }

        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials", "Assets/Textures" });
        foreach (var guid in materialGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("skyboxmat.mat", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                continue;
            }

            var texture = GetTextureIfAvailable(material, "_BaseMap");
            if (texture == null)
            {
                texture = GetTextureIfAvailable(material, "_MainTex");
            }

            var color = Color.white;
            if (material.HasProperty("_BaseColor"))
            {
                color = material.GetColor("_BaseColor");
            }
            else if (material.HasProperty("_Color"))
            {
                color = material.GetColor("_Color");
            }

            material.shader = lit;
            if (texture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.38f);
            }

            EditorUtility.SetDirty(material);
        }
    }

    private static Texture GetTextureIfAvailable(Material material, string property)
    {
        return material.HasProperty(property) ? material.GetTexture(property) : null;
    }

    private static void ConfigureSkybox(Material skybox)
    {
        if (skybox == null)
        {
            return;
        }

        var shader = Shader.Find("Skybox/Panoramic");
        if (shader != null)
        {
            skybox.shader = shader;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Textures/charolettenbrunn_park_4k.exr");
        if (texture != null && skybox.HasProperty("_MainTex"))
        {
            skybox.SetTexture("_MainTex", texture);
        }

        if (skybox.HasProperty("_Tint"))
        {
            skybox.SetColor("_Tint", Color.white);
        }

        if (skybox.HasProperty("_Exposure"))
        {
            skybox.SetFloat("_Exposure", 1f);
        }

        if (skybox.HasProperty("_Rotation"))
        {
            skybox.SetFloat("_Rotation", 0f);
        }

        EditorUtility.SetDirty(skybox);
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        if (material == null)
        {
            return;
        }

        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = material;
        }
    }

    private static GameObject FindRootMainCamera(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Main Camera" && root.GetComponent<Camera>() != null)
            {
                return root;
            }
        }

        return null;
    }

    private static GameObject FindSceneObject(string name, Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == name && go.scene == scene && !EditorUtility.IsPersistent(go))
            {
                return go;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectByPath(string path, Scene scene)
    {
        var parts = path.Split('/');
        if (parts.Length == 0)
        {
            return null;
        }

        var current = FindSceneObject(parts[0], scene);
        for (var i = 1; current != null && i < parts.Length; i++)
        {
            current = FindDirectChild(current.transform, parts[i]);
        }

        return current;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
#endif
