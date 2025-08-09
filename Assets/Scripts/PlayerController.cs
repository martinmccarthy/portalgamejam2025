using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviourPun
{
    public int movementSpeed = 1;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float g = -9.81f;
    public TMP_Text nametag;
    
    CharacterController controller;

    float pitch;
    float verticalVelocity;
    Camera cam;

    [Header("MouseInput")]
    [SerializeField] float mouseSensitivity = 180f;
    [SerializeField] float pitchMin = -85f;
    [SerializeField] float pitchMax = 85f;

    [Header("ButtonInputs")]
    [SerializeField] KeyCode slideButton = KeyCode.LeftControl;

    [Header("Slide")]
    [SerializeField] float slideSpeed = 10f;
    [SerializeField] float slideDuration = 0.75f;
    [SerializeField] float slideCooldown = 2f;
    [SerializeField] float slideHeight = 1.0f;
    float slideTimer;
    float nextSlideTime;
    bool isSliding;
    Vector3 slideDir;
    float originalHeight;
    Vector3 originalCenter;


    void Start()
    {
        HandleCameraLoad();
        controller = GetComponent<CharacterController>();
        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        nametag.text = photonView.Owner.NickName;
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            DoRotation();
            DoMotion();
        }
    }

    private void DoRotation()
    {
        float inputHorizontal = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float inputVertical = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * inputHorizontal);


        transform.Translate(inputHorizontal * movementSpeed * Time.deltaTime * Vector3.right);
        pitch -= inputVertical;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        if (cam)
        {
            cam.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }
    }

    private void DoMotion()
    {
        float inputHMovement = Input.GetAxisRaw("Horizontal");
        float inputVMovement = Input.GetAxisRaw("Vertical");
        Vector3 movementVector = (transform.right * inputHMovement + transform.forward * inputVMovement).normalized;
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f; // got this from gippity i think im not quite sure why they do this
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space)) 
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * g); // again not sure about the -2 here but i get why it's negative to flip the g
        }
        if(!isSliding && controller.isGrounded && inputVMovement > 0f && Input.GetKeyDown(slideButton) && Time.time >= nextSlideTime)
        {
            Slide();
        }

        if(isSliding)
        {
            InSlideMotion();
        }
        verticalVelocity += g * Time.deltaTime;

        controller.Move((movementVector * movementSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private void Slide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        nextSlideTime = Time.time + slideCooldown + slideDuration;
        controller.height = slideHeight;
        Vector3 c = controller.center;
        controller.center = new(c.x, slideHeight * 0.5f, c.z);
    }

    private void InSlideMotion()
    {
        slideTimer -= Time.deltaTime;
        Vector3 hv = slideDir * slideSpeed;
        // controller.Move();
    }

    private void EndSlide()
    {
        isSliding = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
    }

    private void HandleCameraLoad()
    {
        Transform camera = transform.Find("Camera");
        cam = camera.GetComponent<Camera>();
        cam.enabled = photonView.IsMine;
        AudioListener a = cam.GetComponent<AudioListener>();
        a.enabled = photonView.IsMine;
    }

}
