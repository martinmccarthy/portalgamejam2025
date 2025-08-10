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

    [Header("Animator")]
    [SerializeField] Animator anim;
    [SerializeField] float runCycleLegOffset = 0.2f;

    static readonly int ForwardID = Animator.StringToHash("Forward");
    static readonly int TurnID = Animator.StringToHash("Turn");
    static readonly int CrouchID = Animator.StringToHash("Crouch");
    static readonly int OnGroundID = Animator.StringToHash("OnGround");
    static readonly int JumpID = Animator.StringToHash("Jump");
    static readonly int JumpLegID = Animator.StringToHash("JumpLeg");

    CharacterController controller;
    Camera cam;

    float pitch;
    float verticalVelocity;
    bool isSliding;
    float slideTimer;
    float nextSlideTime;
    Vector3 slideDir;
    float originalHeight;
    Vector3 originalCenter;

    Vector3 netTargetPos;
    Quaternion netTargetRot;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        var cap = GetComponent<CapsuleCollider>() ?? gameObject.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.radius = controller.radius;
        cap.height = controller.height;
        cap.center = controller.center;

        if (!anim) anim = GetComponentInChildren<Animator>();

        if (photonView.IsMine)
        {
            controller.enabled = true;
            cap.enabled = false;
            Transform selfMarker = transform.Find("Icosphere.001");
            if (selfMarker) selfMarker.gameObject.layer = LayerMask.NameToLayer("Self");
        }
        else
        {
            controller.enabled = false;
            cap.enabled = true;
        }

        originalHeight = controller.height;
        originalCenter = controller.center;

        netTargetPos = transform.position;
        netTargetRot = transform.rotation;
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
            transform.position = Vector3.Lerp(transform.position, netTargetPos, 12f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, netTargetRot, 12f * Time.deltaTime);
        }
    }

    private void DoRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float yawDelta = mouseX * mouseSensitivity * Time.deltaTime;
        float pitchDelta = mouseY * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * yawDelta);

        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        if (cam) cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // if (anim) anim.SetFloat(TurnID, mouseX);
        float smoothedTurn = Mathf.Lerp(anim.GetFloat(TurnID), mouseX, Time.deltaTime * 10f);
        anim.SetFloat(TurnID, smoothedTurn);
    }

    private void DoMotion()
    {
        float inputH = Input.GetAxisRaw("Horizontal");
        float inputV = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = (transform.right * inputH + transform.forward * inputV).normalized;

        bool onGround = IsGrounded();
        if (onGround && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (onGround && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * g);

        bool wantsCrouch = Input.GetKey(slideButton);
        if (!isSliding && onGround && inputV > 0f && Input.GetKeyDown(slideButton) && Time.time >= nextSlideTime)
            Slide();

        if (isSliding)
            InSlideMotion();

        verticalVelocity += g * Time.deltaTime;

        Vector3 velocity = moveDir * movementSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (isSliding && (slideTimer <= 0f || !onGround))
            EndSlide();

        if (anim)
        {
            Vector3 vel = controller.velocity;
            Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);

            float forwardAmount = 0f;
            float speed = velXZ.magnitude;
            if (movementSpeed > 0.001f)
                forwardAmount = Mathf.Clamp(Vector3.Dot(velXZ.normalized, transform.forward) * (speed / movementSpeed), -1f, 1f);

            anim.SetFloat(ForwardID, forwardAmount);
            anim.SetBool(CrouchID, isSliding || wantsCrouch);
            anim.SetBool(OnGroundID, onGround);
            anim.SetFloat(JumpID, verticalVelocity);

            float jumpLeg = 0f;
            if (onGround && speed > 0.1f)
            {
                var state = anim.GetCurrentAnimatorStateInfo(0);
                float runCycle = Mathf.Repeat(state.normalizedTime + runCycleLegOffset, 1f);
                jumpLeg = (runCycle < 0.5f ? 1f : -1f) * forwardAmount;
            }
            anim.SetFloat(JumpLegID, jumpLeg);
        }
    }

    bool IsGrounded()
    {
        float rayLength = 0.2f; // distance from bottom of player capsule to check
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // start slightly above feet
        return Physics.Raycast(rayOrigin, Vector3.down, rayLength);
    }

    private void Slide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        nextSlideTime = Time.time + slideCooldown + slideDuration;

        slideDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        controller.height = slideHeight;
        Vector3 c = controller.center;
        controller.center = new Vector3(c.x, slideHeight * 0.5f, c.z);
    }

    private void InSlideMotion()
    {
        slideTimer -= Time.deltaTime;
        Vector3 slideVel = slideDir * slideSpeed; slideVel.y = 0f;
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(pitch);
        }
        else
        {
            netTargetPos = (Vector3)stream.ReceiveNext();
            netTargetRot = (Quaternion)stream.ReceiveNext();
            pitch = (float)stream.ReceiveNext();

            if (cam && !photonView.IsMine)
                cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}