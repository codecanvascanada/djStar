using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Dictionary to track which finger is pressing which controller
    private Dictionary<int, TargetController> _activeTouches = new Dictionary<int, TargetController>();

    void Update()
    {
        // Handle mouse input for editor testing
#if UNITY_EDITOR
        HandleMouseInput();
#endif

        // Handle touch input for mobile devices
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                TargetController target = hit.collider.GetComponent<TargetController>();
                if (target != null)
                {
                    target.HandlePress();
                    // Use -1 as a special fingerId for the mouse
                    _activeTouches[-1] = target;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_activeTouches.ContainsKey(-1))
            {
                _activeTouches[-1].HandleRelease();
                _activeTouches.Remove(-1);
            }
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    TargetController target = hit.collider.GetComponent<TargetController>();
                    if (target != null)
                    {
                        target.HandlePress();
                        _activeTouches[touch.fingerId] = target;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (_activeTouches.TryGetValue(touch.fingerId, out TargetController target))
                {
                    target.HandleRelease();
                    _activeTouches.Remove(touch.fingerId);
                }
            }
        }
    }
}
