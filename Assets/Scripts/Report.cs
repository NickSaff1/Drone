using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Report : MonoBehaviour
{
    public static Report Instance { get; private set; }

    // Visual representation of report
    public GameObject ReportPrefab;
    public GameObject defectCollection;
    public GameObject defectPrefab;

    // Prefab transform
    float offset = 0;
    Quaternion rotation = Quaternion.identity;

    // How far up/down the report goes when toggled
    [SerializeField] Vector3 shownPosition = new Vector3(0, 300, 0);
    [SerializeField] Vector3 hiddenPosition = new Vector3(0, -500, 0);

    private bool isReportVisible = false;


    string reportName;

    // List of defects captured
    public List<Defect> defects;

    // Singleton so defects can access and update when they are "inspected" (checked, scanned, whatever you wanna call it)
    void Awake()
    {
        // If an instance already exists and it's not this one, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Subscribe to the OnDefectScanned event
        Defect.OnDefectScanned += AddDefectToReport;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        Defect.OnDefectScanned -= AddDefectToReport;
    }

    void Update()
    {
        if (Input.GetButtonDown("Report"))
        {
            ToggleReportVisibility();
        }
    }

    private void AddDefectToReport(Defect defect)
    {
        defects.Add(defect);
        updateReport();
    }

    // Refreshes Report each time it is updated
    // Based off Elliot's ARBI design
    public void updateReport()
    {
        // Clean report list before reprinting (prevents duplicates)
        foreach (Transform child in defectCollection.transform)
        {
            Destroy(child.gameObject);

            // reset offset
            offset = 0;
        }

        foreach (Defect defect in defects)
        {
            // Not sure if this is needed anymore? Positioning should be handled by collection vertial group
            RectTransform rectTransform = defectCollection.GetComponent<RectTransform>();
            Vector2 centerPosition = new Vector2(
                rectTransform.position.x + rectTransform.rect.width * (0.5f - rectTransform.pivot.x),
                rectTransform.position.y + rectTransform.rect.height * (0.5f - rectTransform.pivot.y)
            );


            GameObject currentDefect = Instantiate(defectPrefab, new Vector3(centerPosition.x, centerPosition.y + offset, 0), rotation, defectCollection.transform);


            // Assign defect.classification to DefectName TextMeshPro component
            TextMeshProUGUI defectNameText = currentDefect.transform.Find("DefectName").GetComponent<TextMeshProUGUI>();
            //defectNameText.text = defect.classification;

            // Assign defect.defectCapture to DefectImage Image component
            Image defectImage = currentDefect.transform.Find("DefectImage").GetComponent<Image>();
            defectImage.sprite = defect.defectCapture; // Assuming defectCapture is of type Sprite

            // Assign defect.timeCapture to DefectTime TextMeshPro component
            TextMeshProUGUI defectTimeText = currentDefect.transform.Find("Data Panel/content/DefectTime").GetComponent<TextMeshProUGUI>();
            defectTimeText.text = "Time Captured: " + defect.timeCapture.ToString(); // Assuming timeCapture is a DateTime or similar

            // Assign defect.measurement to DefectMeasurement TextMeshPro component
            TextMeshProUGUI defectMeasurementText = currentDefect.transform.Find("Data Panel/content/DefectMeasurement").GetComponent<TextMeshProUGUI>();
            defectMeasurementText.text = "Measurement: " + defect.measurement.ToString() + "cm";

            offset -= 300;
        }
    }

    void ToggleReportVisibility()
    {
        if (isReportVisible)
        {
            HideReport();
        }
        else
        {
            ShowReport();
        }
        isReportVisible = !isReportVisible;
    }

    IEnumerator MoveReport(Vector3 targetPosition, float duration)
    {
        float time = 0;
        Vector3 startPosition = ReportPrefab.transform.localPosition;

        while (time < duration)
        {
            ReportPrefab.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        ReportPrefab.transform.localPosition = targetPosition;
    }

    public void ShowReport()
    {
        StartCoroutine(MoveReport(shownPosition, 1f));
    }

    public void HideReport()
    {
        StartCoroutine(MoveReport(hiddenPosition, 1f));
    }
}
