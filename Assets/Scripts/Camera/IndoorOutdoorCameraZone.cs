using UnityEngine;
using Unity.Cinemachine;

public class IndoorOutdoorCameraZone : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineCamera outdoorCam; //Assign outdoor virtual camera
    public CinemachineCamera indoorCam;  //Assign indoor virtual camera

    [Header("Priorities")]
    public int outdoorPriority = 10;     //Lower = background
    public int indoorPriority = 20;      //Higher = foreground

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (indoorCam) indoorCam.Priority = indoorPriority;
        if (outdoorCam) outdoorCam.Priority = outdoorPriority;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (indoorCam) indoorCam.Priority = outdoorPriority;
        if (outdoorCam) outdoorCam.Priority = indoorPriority;
    }
}