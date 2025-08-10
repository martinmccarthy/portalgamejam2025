using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("KillerMechanics")]
    public bool isKiller = false;
    [SerializeField] private KeyCode m_killKey = KeyCode.F;
    [SerializeField] private float m_killMagnitude = 1.0f;

    [Header("PlayerInformation")]
    public bool isAlive = true;
    [SerializeField] CapsuleCollider cap;

    [Header("Move")]
    public float movementSpeed = 4f;
    public float sprintSpeed = 6f;
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
    static readonly int RightID = Animator.StringToHash("Right");
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

    // --- Portal placement & teleport ---
    [Header("Portals")]
    [SerializeField] GameObject redPortal;
    [SerializeField] GameObject bluePortal;
    [SerializeField] float clickRayDistance = 200f;
    [SerializeField] LayerMask clickRayMask = ~0;
    [SerializeField] float portalOffset = 0.01f;          // in front of surface along normal
    [SerializeField] Vector3 portalSize = new(0.35f, 0.55f, 0.02f); // small trigger
    [SerializeField] float exitClearance = 0.6f;          // how far in front of exit to place player
    [SerializeField] float portalCooldown = 0.25f;
    [SerializeField] float floorUpwardBoost = 1.25f;         // how much to pop up when arriving at a floor portal
    [SerializeField] float floorExitClearanceMultiplier = 2f; // multiplies exitClearance only when arriving at a floor portal

    PortalEndpoint leftEndpoint;
    PortalEndpoint rightEndpoint;
    float nextTeleportAllowed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
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
            if (!isAlive)
            {
                HandleSpectate();
                return;
            }

            DoRotation();
            DoMotion();
            HandleKiller();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, netTargetPos, 12f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, netTargetRot, 12f * Time.deltaTime);
        }
    }

    private void HandleKiller()
    {
        if (!isKiller)
        {
            return;
        }
        HandlePortalClicks();

        if (Input.GetKeyDown(m_killKey))
        {
            Ray r = new(transform.position, transform.forward);
            Physics.Raycast(r, out RaycastHit hit, maxDistance: m_killMagnitude);
            if (!hit.collider) return;
            Debug.Log($"hit object {hit.collider.name}");

            GameObject collision = hit.collider.gameObject;
            PlayerController victimController = hit.collider.GetComponentInParent<PlayerController>();
            Debug.Log(victimController);
            if (!victimController) return;

            victimController.photonView.RPC(nameof(PlayerController.RpcDie), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            
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

        Vector3 velocity = (Input.GetKey(KeyCode.LeftShift) && inputV >= 0)
            ? moveDir * sprintSpeed
            : moveDir * movementSpeed;

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
            {
                if (inputV > 0 && Input.GetKey(KeyCode.LeftShift)) forwardAmount = 1f;
                else if (inputV > 0) forwardAmount = 0.5f;
                else if (inputV < 0) forwardAmount = -1f;
                else forwardAmount = 0f;
            }

            float rightAmount = 0f;
            if (movementSpeed > 0.001f)
                rightAmount = Mathf.Clamp(Vector3.Dot(velXZ.normalized, transform.right) * (speed / movementSpeed), -1f, 1f);

            anim.SetFloat(ForwardID, forwardAmount, 0.12f, Time.deltaTime);
            anim.SetFloat(RightID, rightAmount, 0.12f, Time.deltaTime);
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
        float rayLength = 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
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

    // ---------- PORTALS ----------
    void HandlePortalClicks()
    {
        if (!cam) return;

        Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = cam.ScreenPointToRay(center);

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickRayMask, QueryTriggerInteraction.Ignore))
                CreateOrMoveEndpoint(ref leftEndpoint, "PortalLeft", hit);
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickRayMask, QueryTriggerInteraction.Ignore))
                CreateOrMoveEndpoint(ref rightEndpoint, "PortalRight", hit);
        }
    }

    void CreateOrMoveEndpoint(ref PortalEndpoint endpoint, string name, RaycastHit hit)
    {
        bool isLeft = name == "PortalLeft";

        if (endpoint == null)
            endpoint = FindExistingEndpoint(isLeft);

        Vector3 pos = hit.point + hit.normal * portalOffset;
        Quaternion rot = Quaternion.LookRotation(hit.normal, Vector3.up);

        if (endpoint == null)
        {
            var prefab = isLeft ? redPortal : bluePortal;
            var go = PhotonNetwork.Instantiate(prefab.name, pos, rot, 0, new object[] { isLeft, photonView.ViewID });
            endpoint = go.GetComponent<PortalEndpoint>();
        }
        else
        {
            if (endpoint.photonView != null)
                endpoint.photonView.RPC(nameof(PortalEndpoint.NetMove), RpcTarget.All, pos, rot);
            else
                endpoint.transform.SetPositionAndRotation(pos, rot);
        }
    }

    PortalEndpoint FindExistingEndpoint(bool isLeft)
    {
        var all = FindObjectsByType<PortalEndpoint>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i].owner == this && all[i].isLeft == isLeft)
                return all[i];
        return null;
    }

    public void TryTeleportFrom(PortalEndpoint from)
    {
        if (Time.time < nextTeleportAllowed) return;

        PortalEndpoint to = FindSiblingEndpoint(from);
        if (to == null) return;

        Vector3 destNormal = to.transform.forward.normalized;

        bool toFloor = Vector3.Dot(destNormal, Vector3.up) > 0.75f;
        bool toCeiling = Vector3.Dot(destNormal, Vector3.down) > 0.75f;
        bool toWall = !toFloor && !toCeiling;

        float clearance = exitClearance;
        if (toFloor) clearance *= floorExitClearanceMultiplier;

        Vector3 exitPos = to.transform.position + destNormal * clearance;
        if (toFloor) exitPos += Vector3.up * floorUpwardBoost;

        Quaternion exitRot;
        if (toWall)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(destNormal, Vector3.up);
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            exitRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }
        else
        {
            Vector3 yawForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (yawForward.sqrMagnitude < 1e-4f)
            {
                yawForward = Vector3.ProjectOnPlane(to.transform.right, Vector3.up);
                if (yawForward.sqrMagnitude < 1e-4f) yawForward = Vector3.forward;
            }
            exitRot = Quaternion.LookRotation(yawForward.normalized, Vector3.up);
        }

        controller.enabled = false;
        transform.SetPositionAndRotation(exitPos, exitRot);
        verticalVelocity = 0f;
        controller.enabled = true;

        nextTeleportAllowed = Time.time + portalCooldown;
    }

    PortalEndpoint FindSiblingEndpoint(PortalEndpoint from)
    {
        var all = FindObjectsByType<PortalEndpoint>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var e = all[i];
            if (e == from) continue;
            if (e.owner == from.owner && e.isLeft != from.isLeft)
                return e;
        }
        return null;
    }

    void OnDrawGizmos()
    {
        if (leftEndpoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(leftEndpoint.transform.position, portalSize);
            Gizmos.DrawRay(leftEndpoint.transform.position, leftEndpoint.transform.forward * 0.3f);
        }
        if (rightEndpoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(rightEndpoint.transform.position, portalSize);
            Gizmos.DrawRay(rightEndpoint.transform.position, rightEndpoint.transform.forward * 0.3f);
        }
    }

    // ---------- Photon Sync ----------
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

    void HandleSpectate()
    {
        // implement me pls
    }

    [PunRPC]
    public void RpcDie(int killerActor)
    {
        isAlive = false;
        GetComponent<CharacterController>().enabled = false;
        cap.enabled = false;
        transform.Find("Icosphere.001").gameObject.SetActive(false);
    }
}

