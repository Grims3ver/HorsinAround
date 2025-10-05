using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class SlidingDoor : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform door;                   //Transform to move; defaults to this transform
    [SerializeField] private Vector3 slideDirectionLocal = Vector3.right; //Local-space direction relative to parent
    [SerializeField] private float slideDistance = 2f;         //Distance to move along slideDirectionLocal
    [SerializeField] private float slideSeconds = 0.35f;       //Time to complete the slide

    [Header("Motion")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); //Ease curve

    [Header("Start State")]
    [SerializeField] private bool startOpen = false;           //Start opened if true

    [Header("Optional")]
    [SerializeField] private NavMeshObstacle navObstacle;      //Optional NavMeshObstacle to block when closed
    [SerializeField] private bool obstacleWhenClosed = true;   //Enable obstacle only when closed
    [SerializeField] private AudioSource sfxOpen;              //Optional open sound
    [SerializeField] private AudioSource sfxClose;             //Optional close sound

    [Header("Events")]
    public UnityEvent onOpened;                                //Fired after reaching open
    public UnityEvent onClosed;                                //Fired after reaching closed

    //Cached local endpoints
    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    private bool isOpen;
    private bool isMoving;
    private Coroutine moveRoutine;

    void Reset()
    {
        //Auto-assign door to this transform if empty
        if (door == null) door = transform;
    }

    void Awake()
    {
        if (door == null) door = transform;

        //Normalize local direction and cache endpoints relative to the closed pose placed in the editor
        Vector3 dirNorm = slideDirectionLocal == Vector3.zero ? Vector3.right : slideDirectionLocal.normalized;

        closedLocalPos = door.localPosition;
        openLocalPos = closedLocalPos + dirNorm * slideDistance;

        //Snap to initial state without firing events or sounds
        door.localPosition = startOpen ? openLocalPos : closedLocalPos;
        isOpen = startOpen;
        SyncNavObstacle();
    }

    //Public API
    public void SlideOpen()
    {
        if (isOpen) return;
        StartSlide(true);
    }

    public void SlideClose()
    {
        if (!isOpen) return;
        StartSlide(false);
    }

    public void Toggle()
    {
        StartSlide(!isOpen);
    }

    //Core slide logic
    private void StartSlide(bool open)
    {
        if (isMoving && moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(SlideRoutine(open));
    }

    private System.Collections.IEnumerator SlideRoutine(bool open)
    {
        isMoving = true;

        Vector3 from = door.localPosition;
        Vector3 to = open ? openLocalPos : closedLocalPos;

        if (open && sfxOpen != null) sfxOpen.Play();
        if (!open && sfxClose != null) sfxClose.Play();

        float t = 0f;
        float dur = Mathf.Max(0.01f, slideSeconds);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = curve.Evaluate(Mathf.Clamp01(t));
            door.localPosition = Vector3.Lerp(from, to, eased);
            yield return null;
        }

        door.localPosition = to;

        isOpen = open;
        isMoving = false;
        SyncNavObstacle();

        if (isOpen) onOpened?.Invoke();
        else onClosed?.Invoke();
    }

    //Helpers
    private void SyncNavObstacle()
    {
        if (navObstacle == null) return;
        bool active = obstacleWhenClosed && !isOpen;
        navObstacle.enabled = active;
    }
}