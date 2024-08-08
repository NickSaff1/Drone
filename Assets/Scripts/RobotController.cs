using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public abstract class RobotController : MonoBehaviour
{
    public bool isSelected;
    public bool isOn;
    public GameObject CameraFrame;

    // Robot Vision
    public Camera robotCamera;
    public RawImage display;
    public RenderTexture renderTexture;
    private Transform cameraTransform;

    public Texture2D defectCapture;
    public Sprite defectCaptureSprite;

    public Image reticle;

    // Defect Detection
    RaycastHit defect;

    // Call in Start
    public void InitializeCamera()
    {
        cameraTransform = robotCamera.transform;
        renderTexture = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
        robotCamera.targetTexture = renderTexture;
        display.texture = renderTexture;
    }

    // TLDR; Fancy Screenshot
    public void CaptureFrame()
    {
        // Ensure the renderTexture is already assigned and initialized
        if (renderTexture == null)
        {
            Debug.LogError("RenderTexture is not initialized.");
            return;
        }

        // Create a new Texture2D with the same dimensions as the RenderTexture
        defectCapture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // Save the current active RenderTexture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the active RenderTexture to our RenderTexture, so we can read from it
        RenderTexture.active = renderTexture;

        // Read pixels from the RenderTexture and apply them to the Texture2D
        defectCapture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        defectCapture.Apply();

        // Restore the active RenderTexture
        RenderTexture.active = currentActiveRT;

        // Create a Sprite from the Texture2D
        defectCaptureSprite = Sprite.Create(defectCapture, new Rect(0.0f, 0.0f, defectCapture.width, defectCapture.height), new Vector2(0.5f, 0.5f));
    }

    // Call on shutter press
    public void DefectDetection()
    {
        if (!robotCamera.enabled)
        {
            Debug.LogError("Robot Error: Cannot take picture when camera is disabled.");
            return;
        }

        // Cast out ray from camera
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 10.0f))
        {
            Debug.DrawRay(cameraTransform.position, cameraTransform.forward * 10.0f, Color.yellow);

            // Check if the hit object has the "Defect" tag
            if (hit.collider.gameObject.tag == "Defect")
            {
                // The rest of this should be separated into a scoring class of some sort
                var defectObject = hit.collider.gameObject.GetComponent<Defect>();

                if (defectObject != null && !defectObject.isChecked)
                {
                    // Captures camera frame (screenshot)
                    CaptureFrame();

                    // Maximum distance at which points are awarded
                    float maxDistance = 10.0f;
                    int maxPoints = 100;
                    float distance = Vector3.Distance(cameraTransform.position, hit.collider.transform.position);

                    if (distance <= maxDistance)
                    {
                        // Linear scale: Closer distances give more points
                        float scoreMultiplier = (maxDistance - distance) / maxDistance;
                        int additionalPoints = Mathf.RoundToInt(maxPoints * scoreMultiplier);

                        // Add points to the score and save to defect for grading purposes
                        GameManager.Instance.score += additionalPoints;
                        defectObject.distanceScore = additionalPoints;

                        // Calculate the angle score
                        Vector3 rayDirection = (hit.point - cameraTransform.position).normalized;
                        float angleCosine = Mathf.Abs(Vector3.Dot(rayDirection, hit.normal)); // Use Abs to ensure positive values
                        float angleScoreMultiplier = angleCosine; // Directly use cosine value since we want perpendicular angles to score higher
                        int anglePoints = Mathf.RoundToInt(maxPoints * angleScoreMultiplier);

                        // Add angle points to the score and save to defect for grading purposes
                        GameManager.Instance.score += anglePoints;
                        defectObject.angleScore = anglePoints;
                    }
                    else
                    {
                        Debug.Log("No points added, defect is too far away.");
                    }

                    // Update report with picture captured
                    defectObject.defectCapture = defectCaptureSprite;
                    defectObject.sendDefectToReport();
                }
            }
        }
    }

    public void UpdateReticleColor()
    {
        if (!robotCamera.enabled)
        {
            return;
        }

        // Cast out ray from camera
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 10.0f))
        {
            // Check if the hit object has the "Defect" tag
            if (hit.collider.gameObject.tag == "Defect")
            {
                float maxDistance = 10.0f;
                float distance = Vector3.Distance(cameraTransform.position, hit.collider.transform.position);

                // Calculate distance score
                float distanceScore = Mathf.Clamp01((maxDistance - distance) / maxDistance);

                // Calculate angle score
                Vector3 rayDirection = (hit.point - cameraTransform.position).normalized;
                float angleCosine = Mathf.Abs(Vector3.Dot(rayDirection, hit.normal));
                float angleScore = angleCosine;

                // Average score for reticle color
                float averageScore = (distanceScore + angleScore) / 2.0f;

                // Blend colors based on score
                Color reticleColor = BlendColor(averageScore);
                SetReticleColor(reticleColor);
            }
            else
            {
                // No defect detected, set reticle color to default (e.g., red)
                SetReticleColor(Color.red);
            }
        }
        else
        {
            // No hit detected, set reticle color to default (e.g., red)
            SetReticleColor(Color.red);
        }
    }

    void SetReticleColor(Color color)
    {
        reticle.color = color;
    }

    Color BlendColor(float score)
    {
        Color color;
        if (score < 0.5f)
        {
            // Blend from red to yellow
            color = Color.Lerp(Color.red, Color.yellow, score * 2.0f);
        }
        else
        {
            // Blend from yellow to green
            color = Color.Lerp(Color.yellow, Color.green, (score - 0.5f) * 2.0f);
        }
        return color;
    }

    public void toggleRobotSelection()
    {
        isSelected = !isSelected;
    }

    public void toggleRobotPowerState()
    {
        isOn = !isOn;
    }

    // Custom implementations in each derived class
    public abstract void HandleInput();
    public abstract void ApplyPhysics();

    void OnEnable()
    {
        //DroneController.OnAim += DefectDetection;
    }

    void OnDisable()
    {
        //DroneController.OnAim -= DefectDetection;
    }
}
