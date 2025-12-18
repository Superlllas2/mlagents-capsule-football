using UnityEngine;
using UnityEngine.InputSystem;

public class BallControl : MonoBehaviour
{
    private Rigidbody rig;
    [SerializeField] private float force;
    
    void Start()
    {
        rig = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (Keyboard.current.wKey.isPressed) rig.AddForce(0, 0, force, ForceMode.Impulse);
        if (Keyboard.current.sKey.isPressed) rig.AddForce(0, 0, -force, ForceMode.Impulse);
        if (Keyboard.current.aKey.isPressed) rig.AddForce(-force, 0, 0, ForceMode.Impulse);
        if (Keyboard.current.dKey.isPressed) rig.AddForce(force, 0, 0, ForceMode.Impulse);
        if (Keyboard.current.spaceKey.isPressed) rig.AddForce(0, force, 0, ForceMode.Impulse);
    }
}
