using Photon.Pun;
using UnityEngine;

public class BatteryManager : MonoBehaviour
{
    [SerializeField] private float holdingMovementSpeed = 3f;
    [SerializeField] private float holdingSprintSpeed = 4.5f;
    [SerializeField] private float regularMovementSpeed;
    [SerializeField] private float regularSprintSpeed;

    [SerializeField] private GameObject m_battery;
    [SerializeField] private GameObject m_charger;
    [SerializeField] private float m_numberOfBatterys = 5f;
    [SerializeField] private float m_numberOfBatteryChargers = 5f;
    [SerializeField] private float m_useMagnitude = 1f;

    [SerializeField] private KeyCode m_useKey = KeyCode.F;
    [SerializeField] private KeyCode m_dropKey = KeyCode.G;

    private bool m_holdingBattery = false;

    private LayerMask layerMask;
    static readonly int batteryLayer = 11;
    static readonly int chargerLayer = 9;

    void Awake()
    {
        layerMask = LayerMask.GetMask("Battery", "Charger");
        regularMovementSpeed = GetComponent<PlayerController>().movementSpeed;
        regularMovementSpeed = GetComponent<PlayerController>().sprintSpeed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(m_useKey) && !GetComponent<PlayerController>().isKiller && GetComponent<PlayerController>().isAlive)
        {
            AttemptBatteryGrabPlace();
        }

        if (Input.GetKeyDown(m_dropKey) && m_holdingBattery)
        {
            DropBattery();
        }
    }

    private void SpawnBatterys()
    {

    }

    private void SpawnBatteryChargers()
    {

    }

    private void AttemptBatteryGrabPlace()
    {
        Ray r = new(transform.position, transform.forward);
        Physics.Raycast(r, out RaycastHit hit, maxDistance: m_useMagnitude, layerMask);

        if (!hit.collider) return;

        Debug.Log($"hit object {hit.collider.name}");

        if (hit.collider.gameObject.layer == batteryLayer && !m_holdingBattery)
        {
            // Place battery infront of player
            GetComponent<PlayerController>().movementSpeed = holdingMovementSpeed;
            GetComponent<PlayerController>().sprintSpeed = holdingSprintSpeed;
            m_holdingBattery = true;
        }

        if (hit.collider.gameObject.layer == chargerLayer && m_holdingBattery)
        {
            // Hide battery infront of player
            GetComponent<PlayerController>().movementSpeed = regularMovementSpeed;
            GetComponent<PlayerController>().sprintSpeed = regularSprintSpeed;

            GameObject modifiedBattery = Instantiate(m_battery, hit.collider.gameObject.transform.Find("BatteryPlacePoint").position, hit.collider.gameObject.transform.Find("BatteryPlacePoint").rotation);
            hit.collider.enabled = false;
            modifiedBattery.GetComponent<Collider>().enabled = false;
            m_holdingBattery = false;
        }
    }

    private void DropBattery()
    {
        Ray r = new(transform.position, -transform.up);
        Physics.Raycast(r, out RaycastHit hit, maxDistance: m_useMagnitude);

        if (!hit.collider) return;

        Debug.Log($"hit object {hit.collider.name}");

        // Hide battery infront of player
        GetComponent<PlayerController>().movementSpeed = regularMovementSpeed;
        GetComponent<PlayerController>().sprintSpeed = regularSprintSpeed;

        Instantiate(m_battery, hit.transform.position, m_battery.transform.rotation);
        m_holdingBattery = false;
    }
}
