using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Defect : MonoBehaviour
{
    // Contains defect variations ( Currently unimplemented )
    // Goal: Randomly select sprite from list of defect variations
    [SerializeField] List<Sprite> sprites = new List<Sprite>();

    // Prevents defect from being recorded more than once
    public bool isChecked = false;

    // Defect measurements
    public string classification = "unclassified";
    public float measurement = 0.0f;
    public string timeCapture;
    public Sprite defectCapture;

    public int defectScore = 0; // Score from capture ; updated in RobotController
    public int distanceScore = 0; // Score based on photo distance from defect
    public int angleScore = 0; // Score based on photo angle from defect ( perpendicular = max points )
    public int classificationScore = 100; // Score based on classification and condition // hard coded for now smh

    // Event to notify when a defect is scanned
    public static event Action<Defect> OnDefectScanned;

    public virtual void sendDefectToReport()
    {
        isChecked = true;
        timeCapture = GameManager.Instance.timer.ToString();

        // Need to separate scoring into an event or class
        GameManager.Instance.score += 100;
        defectScore += 100;

        // Raise the event for subscribers
        OnDefectScanned?.Invoke(this);

        Debug.Log("Defect Sent to Report!");
    }

    // Determine whether defect is currently visible to active robot's camera
    public virtual void whileInCameraView(Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        if (GeometryUtility.TestPlanesAABB(planes, GetComponent<Collider>().bounds))
        {
            // Defect is visible by drone
            //print("The object" + gameObject.name + "has appeared");
            transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            // Defect is not visible by drone
            //print("The object" + gameObject.name + "has disappeared");
            transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
