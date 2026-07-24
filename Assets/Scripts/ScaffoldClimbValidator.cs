using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using System.Collections;
using System.Collections.Generic;

public class ScaffoldClimbValidator : MonoBehaviour
{
    private Scenario5Manager manager5;
    private Scenario1Manager manager1;
    private List<ClimbInteractable> climbInteractables = new List<ClimbInteractable>();

    [Header("Warning UI (Canvas)")]
    [Tooltip("Tırmanma engellendiğinde gösterilecek uyarı paneli")]
    public GameObject warningCanvas;
    
    [Tooltip("Uyarı panelinin ne kadar süre açık kalacağı")]
    public float warningDuration = 3f;

    private Coroutine warningCoroutine;

    void Start()
    {
        // Sahnedeki yöneticileri buluyoruz
        manager5 = FindAnyObjectByType<Scenario5Manager>();
        manager1 = FindAnyObjectByType<Scenario1Manager>();

        if (manager5 == null && manager1 == null)
        {
            Debug.LogWarning("ScaffoldClimbValidator | Sahne içerisinde herhangi bir Scenario Manager bulunamadı!");
        }

        // İskele altındaki tüm ClimbInteractable bileşenlerini topluyoruz
        climbInteractables.AddRange(GetComponentsInChildren<ClimbInteractable>(true));
        
        // Her bir tırmanma noktasına dinleyici ekliyoruz
        foreach (var climbable in climbInteractables)
        {
            climbable.selectEntered.AddListener(OnClimbAttempted);
        }

        if (warningCanvas != null)
        {
            warningCanvas.SetActive(false);
        }
    }

    void OnDestroy()
    {
        foreach (var climbable in climbInteractables)
        {
            if (climbable != null)
            {
                climbable.selectEntered.RemoveListener(OnClimbAttempted);
            }
        }
    }

    private void OnClimbAttempted(SelectEnterEventArgs args)
    {
        bool hasHarness = false;
        bool isAnchored = false;

        // Sahnedeki aktif yöneticiye göre kontrol et
        if (manager1 != null)
        {
            hasHarness = manager1.IsHarnessEquipped;
            isAnchored = manager1.IsAnchorSelected;
        }
        else if (manager5 != null)
        {
            hasHarness = manager5.IsHarnessEquipped;
            isAnchored = manager5.IsAnchorSelected;
        }
        else
        {
            // Yönetici yoksa güvenlik nedeniyle tırmanmaya izin verme
            return;
        }

        // Tırmanma şartları kontrol ediliyor
        if (!hasHarness || !isAnchored)
        {
            // Tırmanmayı iptal et: Objeyi geçici olarak devre dışı bırakıyoruz ki elden düşsün
            var interactable = args.interactableObject as ClimbInteractable;
            if (interactable != null)
            {
                StartCoroutine(TemporaryDisableInteractable(interactable));
            }

            // Uyarıyı tetikle
            TriggerWarning(hasHarness, isAnchored);
        }
        else
        {
            // Tırmanış başarılı şekilde başladıysa Scenario1Manager'a bildir
            if (manager1 != null)
            {
                manager1.AttemptClimb();
            }
        }
    }

    private IEnumerator TemporaryDisableInteractable(ClimbInteractable interactable)
    {
        interactable.enabled = false;
        yield return new WaitForEndOfFrame();
        interactable.enabled = true;
    }

    private void TriggerWarning(bool hasHarness, bool isAnchored)
    {
        string message = "";
        if (!hasHarness && !isAnchored)
        {
            message = "Güvenlik İhlali! Emniyet kemerini takmadan ve ankraj yapmadan tırmanamazsınız!";
        }
        else if (!hasHarness)
        {
            message = "Güvenlik İhlali! Emniyet kemerini takmadınız!";
        }
        else if (!isAnchored)
        {
            message = "Güvenlik İhlali! Karabinayı ankraj noktasına bağlamadınız!";
        }

        Debug.LogWarning("ScaffoldClimbValidator | " + message);

        if (warningCanvas != null)
        {
            if (warningCoroutine != null)
            {
                StopCoroutine(warningCoroutine);
            }
            warningCoroutine = StartCoroutine(ShowWarningUI(message));
        }
    }

    private IEnumerator ShowWarningUI(string message)
    {
        warningCanvas.SetActive(true);
        
        var textComponent = warningCanvas.GetComponentInChildren<UnityEngine.UI.Text>();
        if (textComponent != null)
        {
            textComponent.text = message;
        }
        else
        {
            var tmProText = warningCanvas.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmProText != null)
            {
                tmProText.text = message;
            }
        }

        yield return new WaitForSeconds(warningDuration);
        warningCanvas.SetActive(false);
        warningCoroutine = null;
    }
}
