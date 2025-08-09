using UnityEngine;
public class DriveLocomotion : MonoBehaviour
{
    Animator anim;
    void Awake() { anim = GetComponent<Animator>(); }
    void Update()
    {
        float input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        anim.SetFloat("Speed", input * 4f); // maps 0..1 input to 0..4 thresholds
    }
}
