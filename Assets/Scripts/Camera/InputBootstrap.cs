using UnityEngine;
using UnityEngine.InputSystem;

public class InputBootstrap : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;
    void Awake()
    {
        actions.FindActionMap("Camera", throwIfNotFound: true).Enable(); // keeps Look alive for Cinemachine
    }
}