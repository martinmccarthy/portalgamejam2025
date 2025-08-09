using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Move")]
    public float movementSpeed = 4f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float g = -9.81f;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 180f;
    [SerializeField] float pitchMin = -85f;
    [SerializeField] float pitchMax = 85f;

    [Header("Buttons")]
    [SerializeField] KeyCode slideButton = KeyCode.LeftControl;

    [Header("Slide")]
    [SerializeField] float slideSpeed = 10f;
    [SerializeField] float slideDuration = 0.75f;
    [SerializeField] float slideCooldown = 2f;
    [SerializeField] float slideHeight = 1.0f;

    [Header("UI")]
    public TMP_Text nametag;

    // components
    CharacterController controller;
    Camera cam;

    // state
    float pitch;
    float verticalVelocity;
    bool isSliding;
    float slideTimer;
    float nextSlideTime;
    Vector3 slideDir;
    float originalHeight;
    Vector3 originalCenter;

    // net interpolation (for non-owners)
    Vector3 netTargetPos;
    Quaternion netTargetRot;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        var cap = GetComponent<CapsuleCollider>() ?? gameObject.AddComponent<CapsuleCollider>();
        cap.direction = 1; // Y
        cap.radius = controller.radius;
        cap.height = controller.height;
        cap.center = controller.center;

        if (photonView.IsMine) 
        { 
            controller.enabled = true;
            cap.enabled = false;
            transform.Find("Icosphere.001").gameObject.layer = LayerMask.NameToLayer("Self");
        }
        else 
        { 
            controller.enabled = false; 
            cap.enabled = true; 
        }
    }


    void Start()
    {
        HandleCameraLoad();

        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (nametag)
            nametag.text = photonView.Owner != null ? photonView.Owner.NickName : "Player";
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            DoRotation();
            DoMotion();
        }
        else
        {
            // Smoothly follow networked transform
            transform.position = Vector3.Lerp(transform.position, netTargetPos, 12f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, netTargetRot, 12f * Time.deltaTime);
        }
    }

    // ---------- Look / Rotate (owner only) ----------
    private void DoRotation()
    {
        float inputHorizontal = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float inputVertical = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Yaw on the body
        transform.Rotate(Vector3.up * inputHorizontal);

        // NO translate here (conflicts with CC). Keep translation in DoMotion() only.

        // Pitch on the camera
        pitch -= inputVertical;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        if (cam) cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    // ---------- Move / Jump / Slide (owner only) ----------
    private void DoMotion()
    {
        float inputH = Input.GetAxisRaw("Horizontal");
        float inputV = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = (transform.right * inputH + transform.forward * inputV).normalized;

        // Ground handling
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small stick-to-ground

        // Jump
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * g);

        // Slide start
        if (!isSliding && controller.isGrounded && inputV > 0f && Input.GetKeyDown(slideButton) && Time.time >= nextSlideTime)
            Slide();

        // Slide update
        if (isSliding)
            InSlideMotion();

        // Gravity
        verticalVelocity += g * Time.deltaTime;

        // Final motion
        Vector3 velocity = moveDir * movementSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // Slide end when timer expires or we leave ground
        if (isSliding && (slideTimer <= 0f || !controller.isGrounded))
            EndSlide();
    }

    private void Slide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        nextSlideTime = Time.time + slideCooldown + slideDuration;

        // keep horizontal direction of current forward
        slideDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // crouch capsule
        controller.height = slideHeight;
        Vector3 c = controller.center;
        controller.center = new Vector3(c.x, slideHeight * 0.5f, c.z);
    }

    private void InSlideMotion()
    {
        slideTimer -= Time.deltaTime;

        // apply a burst along slideDir on XZ
        Vector3 slideVel = slideDir * slideSpeed;
        slideVel.y = 0f;

        controller.Move(slideVel * Time.deltaTime);
    }

    private void EndSlide()
    {
        isSliding = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
    }

    private void HandleCameraLoad()
    {
        // find a child named "Camera" or any Camera under this object
        if (!cam)
        {
            Transform t = transform.Find("Camera");
            if (t) cam = t.GetComponent<Camera>();
            if (!cam) cam = GetComponentInChildren<Camera>(true);
        }

        if (cam)
        {
            cam.enabled = photonView.IsMine;
            var a = cam.GetComponent<AudioListener>();
            if (a) a.enabled = photonView.IsMine;
        }
    }

    // ---------- Photon Sync ----------
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // owner sends
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(pitch); // optional: send local camera pitch if you need it
        }
        else // remotes receive
        {
            netTargetPos = (Vector3)stream.ReceiveNext();
            netTargetRot = (Quaternion)stream.ReceiveNext();
            pitch = (float)stream.ReceiveNext();

            // apply remote camera pitch if the remote has a visible head/camera bone you care about
            if (cam && !photonView.IsMine)
                cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}