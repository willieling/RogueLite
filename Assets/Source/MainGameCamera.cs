using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;

// This will work as long as the camera frustum doesn't change.
// If we want to start resizing the frustrum, we need to dynamically move the colliders
public class MainGameCamera : MonoBehaviour
{
    private Camera _camera;

    [SerializeField]
    private BoxCollider2D LeftCollider;
    [SerializeField]
    private BoxCollider2D RightCollider;
    [SerializeField]
    private BoxCollider2D TopCollider;
    [SerializeField]
    private BoxCollider2D BottomCollider;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
