using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoringSystem : MonoBehaviour
{
    private int totalScore = 0;
    //private int capturedDefects = 0;
    [Header("Total Scannable Defects in the Scene")]
    [SerializeField] public int totalDefects = 0;
    private const int BaseScorePerDefect = 100;
    private const int MaxAdditionalScorePerDefect = 100;
    private const int MaxScorePerDefect = BaseScorePerDefect + 300; // 100 base + 100 distance + 100 angle + 100 condition

    // Frontend UI
    public GameObject gradeCard;    // UI Canvas for score 
    public GameObject gradePrefab; // UI representation of scores
    public GameObject gradeCollection; // UI Scroll containing all gradePrefabs

    // This should be event based or something with how scattered UI toggling is :/
    public GameObject reportUI;

    // Contains all defects ; Filled in inspector
    [Header("Populate with all defects per scene")]
    [SerializeField] public List<Defect> defects = new List<Defect>();

    private void Awake()
    {
        Debug.Log("Starting up Scoring system...");
        gradeCard.SetActive(false);
    }

    void OnEnable()
    {
        Defect.OnDefectScanned += HandleDefectScanned;
    }

    void OnDisable()
    {
        Defect.OnDefectScanned -= HandleDefectScanned;
    }

    private void HandleDefectScanned(Defect defect)
    {
        // Add base score and defect-specific score
        //totalScore += BaseScorePerDefect + defect.defectScore;
        //capturedDefects++;

        // Each defect contains information about its score
        //defects.Add(defect);

        // NEW APPROACH : Add all defects from the start instead of storing references y'know??

        // Can add scoring here lel



        Debug.Log($"Updated Score: {totalScore}");
    }

    private string CalculateLetterGrade(float percentage)
    {
        string letterGrade = "E";

        if (percentage >= 90)
        {
            letterGrade = "A";
        }
        else if (percentage >= 80)
        {
            letterGrade = "B";
        }
        else if (percentage >= 70)
        {
            letterGrade = "C";
        }
        else if (percentage >= 60)
        {
            letterGrade = "D";
        }
        else
        {
            letterGrade = "F";
        }

        return letterGrade;
    }

    public void CalculateFinalGrade()
    {
        GameManager.Instance.HUD.SetActive(false);
        reportUI.SetActive(false);
        gradeCard.SetActive(true);

        Debug.Log("Calculating Grade...");

        if (defects.Count > 0)
        {
            foreach (Defect defect in defects)
            {
                // Update UI
                GameObject currentGrade = Instantiate(gradePrefab, transform.position, Quaternion.identity, gradeCollection.transform);
                currentGrade.transform.Find("Breakdown/Score Name/Name").GetComponent<TextMeshProUGUI>().text = defect.classification;
                currentGrade.transform.Find("Breakdown/Picture Distance/Score").GetComponent<TextMeshProUGUI>().text = defect.distanceScore.ToString() + " / 100";
                currentGrade.transform.Find("Breakdown/Picture Angle/Score").GetComponent<TextMeshProUGUI>().text = defect.angleScore.ToString() + " / 100";
                currentGrade.transform.Find("Breakdown/Classification/Score").GetComponent<TextMeshProUGUI>().text =
                    defect.classificationScore.ToString() + " / 100";

                // Calculate letter grade
                // Base score = 100
                // Distance score max = 100
                // Angle Score max = 100
                // 300 points total
                // Classification score max = 100 (binary atm, factor in measurement and condition later)
                // Convert score out of 300 to letter grade
                // score = defect.defectScore + defect.distanceScore + defect.angleScore

                int currentScore = defect.defectScore + defect.distanceScore + defect.angleScore + defect.classificationScore;
                totalScore += currentScore;

                // Convert total score out of 300 into a percentage
                float percentage = (float)currentScore / 400 * 100;

                currentGrade.transform.Find("Total Score Area/Score").GetComponent<TextMeshProUGUI>().text = CalculateLetterGrade(percentage);
            }

            int maxPossibleScore = defects.Count * MaxScorePerDefect;
            float grade = (float)totalScore / maxPossibleScore;

            gradeCard.transform.Find("Gradecard/Final Grade").GetComponent<TextMeshProUGUI>().text = "Final Score: " + CalculateLetterGrade(grade * 100);

            Debug.Log($"Final Grade: {grade * 100}%");
        }
        else
        {
            // If no defects are captured, F
            totalScore = 0;
            Debug.Log($"Final Grade: {totalScore * 100}%");
        }
    }

    // End of level ; restart game
    public void CloseGradeCard()
    {
        // Reload Scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}
