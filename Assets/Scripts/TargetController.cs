using UnityEngine;
using System.Collections;
using UnityEngine.Playables;

public class TargetController : MonoBehaviour
{
    public KeyCode keyToPress;
    public int laneIndex;

    public event System.Action<int> OnInputPressed;
    public event System.Action<int> OnInputReleased;

    [Header("Visuals")]
    public MeshRenderer laneVisualRenderer;
    public MeshRenderer judgmentLineRenderer;
    public Material pressedMaterial;

    private Note noteInTrigger;
    private Note heldNote;
    private Note breakpointNoteInTrigger;
    private bool isHoldEffectActive = false;
    private bool isBeingHeld = false;
    private float actualHoldStartTime;

    private Material laneDefaultMaterial;
    private Material judgmentLineDefaultMaterial;

    private GameManager _gameManager;

    private const float COMBO_TICK_INTERVAL = 0.1f;
    private float _lastComboTickTime;

    public bool IsBeingHeld { get { return isBeingHeld; } }

    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager == null)
        {
            Debug.LogError("TargetController could not find GameManager in the scene!");
        }

        if (laneVisualRenderer != null) laneDefaultMaterial = laneVisualRenderer.material;
        if (judgmentLineRenderer != null) judgmentLineDefaultMaterial = judgmentLineRenderer.material;
    }

    void Update()
    {
        if (_gameManager == null) return;

        // Keyboard Input for PC
        if (Input.GetKeyDown(keyToPress))
        {
            HandlePress();
        }
        if (Input.GetKeyUp(keyToPress))
        {
            HandleRelease();
        }

        if (heldNote != null)
        {
            if (!isBeingHeld)
            {
                _gameManager.ShowJudgment("Miss");
                _gameManager.ResetCombo();
                PoolManager.Instance.ReturnToPool("Note", heldNote.gameObject);
                heldNote = null;
                StopHoldEffect();
            }
            else
            {
                float songPosition = (float)_gameManager.director.time;
#if UNITY_WEBGL
                songPosition += _gameManager.webglSyncOffset;
#endif
                if (songPosition > _lastComboTickTime + COMBO_TICK_INTERVAL)
                {
                    _gameManager.AddCombo(); // Just add combo, no score or hit registration
                    _lastComboTickTime = songPosition;
                }

                float holdEndTime = actualHoldStartTime + heldNote.noteData.holdDuration;
                if (songPosition >= holdEndTime)
                {
                    _gameManager.ShowJudgment("Perfect");
                    _gameManager.AddScore(150);
                    _gameManager.AddCombo();
                    _gameManager.RegisterSuccessfulHit(); // Register completion as a successful hit
                    PoolManager.Instance.ReturnToPool("Note", heldNote.gameObject);
                    heldNote = null;
                    StopHoldEffect();
                    SetHighlight(false);
                }
            }
        }

        if (breakpointNoteInTrigger != null)
        {
            if (breakpointNoteInTrigger.transform.position.z <= transform.position.z)
            {
                if (_gameManager.director != null && _gameManager.director.state == PlayState.Playing)
                {
                    _gameManager.director.Pause();
                }
                Destroy(breakpointNoteInTrigger.gameObject);
                breakpointNoteInTrigger = null;
            }
        }
    }

    public void HandlePress()
    {
        if (isBeingHeld || _gameManager == null) return;
        isBeingHeld = true;
        SetHighlight(true);
        OnInputPressed?.Invoke(laneIndex);

        float songPosition = (float)_gameManager.director.time;
#if UNITY_WEBGL
        songPosition += _gameManager.webglSyncOffset;
#endif

        if (noteInTrigger != null)
        {
            if (noteInTrigger.noteData.noteType == NoteType.Hold)
            {
                float distance = Mathf.Abs(transform.position.z - noteInTrigger.transform.position.z);
                if (distance < 1.5f)
                {
                    string judgment = distance < 0.5f ? "Perfect" : "Good";
                    EffectManager.Instance.PlayEffect(transform.position, judgment);
                    
                    _gameManager.AddScore(judgment == "Perfect" ? 100 : 50);
                    _gameManager.AddCombo();
                    
                    heldNote = noteInTrigger;
                    EffectManager.Instance.StartHoldEffect(laneIndex, transform.position);
                    isHoldEffectActive = true;
                    actualHoldStartTime = songPosition;
                    _lastComboTickTime = songPosition;
                    noteInTrigger = null;
                }
            }
            else // TAP note
            {
                float distance = Mathf.Abs(transform.position.z - noteInTrigger.transform.position.z);
                if (distance < 4f)
                {
                    string judgment = distance < 2f ? "Perfect" : "Good";
                    _gameManager.ShowJudgment(judgment);
                    EffectManager.Instance.PlayEffect(transform.position, judgment);

                    _gameManager.AddScore(judgment == "Perfect" ? 100 : 50);
                    _gameManager.AddCombo();
                    _gameManager.RegisterSuccessfulHit(); // Hit of a tap note

                    PoolManager.Instance.ReturnToPool("Note", noteInTrigger.gameObject);
                    noteInTrigger = null;
                }
            }
        }
    }

    public void HandleRelease()
    {
        isBeingHeld = false;
        SetHighlight(false);
        OnInputReleased?.Invoke(laneIndex);
    }

    private void StopHoldEffect()
    {
        if (isHoldEffectActive)
        {
            EffectManager.Instance.StopHoldEffect(laneIndex);
            isHoldEffectActive = false;
        }
    }

    private void SetHighlight(bool pressed)
    {
        if (laneVisualRenderer != null && pressedMaterial != null)
        {
            laneVisualRenderer.material = pressed ? pressedMaterial : laneDefaultMaterial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Note"))
        {
            Note note = other.GetComponent<Note>();
            if (note != null)
            {
                if (note.noteData.noteType == NoteType.Breakpoint)
                {
                    breakpointNoteInTrigger = note;
                }
                else
                {
                    noteInTrigger = note;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Note"))
        {
            Note exitedNote = other.GetComponent<Note>();
            if (exitedNote == noteInTrigger)
            {
                if (_gameManager != null)
                {
                    _gameManager.ShowJudgment("Miss");
                    _gameManager.ResetCombo();
                }
                noteInTrigger = null;
            }
            else if (exitedNote == breakpointNoteInTrigger)
            {
                breakpointNoteInTrigger = null;
            }
        }
    }
}
