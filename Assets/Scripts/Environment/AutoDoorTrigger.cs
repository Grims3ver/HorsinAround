using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AutoDoorTrigger : MonoBehaviour
{
    //SlidingDoor to control
    [SerializeField] private SlidingDoor door;
    //Tag that can open the door
    [SerializeField] private string triggerTag = "Player";
    //Close when leaving the trigger
    [SerializeField] private bool closeOnExit = true;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag) && door != null)
        {
            door.SlideOpen();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (closeOnExit && other.CompareTag(triggerTag) && door != null)
        {
            door.SlideClose();
        }
    }
}
