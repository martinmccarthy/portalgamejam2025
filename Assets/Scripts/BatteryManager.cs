using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class BatteryManager : MonoBehaviour
{
    [SerializeField] private float holdingMovementSpeed = 3f;
    [SerializeField] private float holdingSprintSpeed = 4.5f;
    private float regularMovementSpeed;
    private float regularSprintSpeed;

    [SerializeField] private GameObject m_battery;
    [SerializeField] private GameObject m_charger;
    [SerializeField] private GameObject m_batterySpawns;
    [SerializeField] private GameObject m_chargerSpawns;
    [SerializeField] private int m_numberOfBatterys = 5;
    [SerializeField] private int m_numberOfBatteryChargers = 5;
    [SerializeField] private float m_useMagnitude = 1f;

    [SerializeField] private KeyCode m_useKey = KeyCode.F;
    [SerializeField] private KeyCode m_dropKey = KeyCode.G;

    private bool m_holdingBattery = false;
    private GameObject heldBattery;

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
        SpawnBatterys();
        SpawnBatteryChargers();
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

    void SpawnBatterys()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Transform parent = m_batterySpawns.transform;
        int available = parent.childCount;
        int toSpawn = Mathf.Clamp(m_numberOfBatterys, 0, available);

        if (toSpawn < m_numberOfBatterys)
            Debug.LogWarning($"BatteryManager: Requested {m_numberOfBatterys} batteries but only {available} spawn points. Spawning {toSpawn}.");

        foreach (int idx in GetUniqueRandomIndices(available, toSpawn))
        {
            Transform sp = parent.GetChild(idx);
            PhotonNetwork.Instantiate(m_battery.name, sp.position, m_battery.transform.rotation);
        }
    }

    void SpawnBatteryChargers()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Transform parent = m_chargerSpawns.transform;
        int available = parent.childCount;
        int toSpawn = Mathf.Clamp(m_numberOfBatteryChargers, 0, available);

        if (toSpawn < m_numberOfBatteryChargers)
            Debug.LogWarning($"BatteryManager: Requested {m_numberOfBatteryChargers} chargers but only {available} spawn points. Spawning {toSpawn}.");

        foreach (int idx in GetUniqueRandomIndices(available, toSpawn))
        {
            Transform sp = parent.GetChild(idx);
            Quaternion baseRot = m_charger.transform.rotation; // prefab's default rotation (-90 X baked in)
            Quaternion rot = Quaternion.Euler(-90f, sp.eulerAngles.y, sp.eulerAngles.z); // spawn point's Y rotation
            PhotonNetwork.Instantiate(m_charger.name, sp.position, rot);
        }
    }

    static IEnumerable<int> GetUniqueRandomIndices(int poolSize, int pickCount)
    {
        // build 0..n-1
        int[] arr = new int[poolSize];
        for (int i = 0; i < poolSize; i++) arr[i] = i;

        // partial shuffle
        for (int i = 0; i < pickCount; i++)
        {
            int j = Random.Range(i, poolSize);
            (arr[i], arr[j]) = (arr[j], arr[i]);
            yield return arr[i];
        }
    }

    private void AttemptBatteryGrabPlace()
    {
        Ray r = new(transform.position, transform.forward);
        Physics.Raycast(r, out RaycastHit hit, maxDistance: m_useMagnitude, layerMask);

        if (!hit.collider) return;

        Debug.Log($"hit object {hit.collider.name}");

        // Grab Battery
        if (hit.collider.gameObject.layer == batteryLayer && !m_holdingBattery)
        {
            m_holdingBattery = true;

            Destroy(hit.collider.gameObject);
            heldBattery = Instantiate(m_battery, transform.Find("PlayerBatteryPlacePoint").position, transform.Find("PlayerBatteryPlacePoint").rotation);
            heldBattery.GetComponent<Collider>().enabled = false;
            GetComponent<PlayerController>().movementSpeed = holdingMovementSpeed;
            GetComponent<PlayerController>().sprintSpeed = holdingSprintSpeed;
        }

        // Place Battery
        if (hit.collider.gameObject.layer == chargerLayer && m_holdingBattery)
        {
            m_holdingBattery = false;

            Destroy(heldBattery);
            GetComponent<PlayerController>().movementSpeed = regularMovementSpeed;
            GetComponent<PlayerController>().sprintSpeed = regularSprintSpeed;

            GameObject modifiedBattery = Instantiate(m_battery, hit.collider.gameObject.transform.Find("BatteryPlacePoint").position, hit.collider.gameObject.transform.Find("BatteryPlacePoint").rotation);
            hit.collider.enabled = false;
            modifiedBattery.GetComponent<Collider>().enabled = false;
        }
    }

    private void DropBattery()
    {
        Ray r = new(transform.position, -transform.up);
        Physics.Raycast(r, out RaycastHit hit, maxDistance: m_useMagnitude);

        if (!hit.collider) return;

        Debug.Log($"hit object {hit.collider.name}");

        Destroy(heldBattery);
        GetComponent<PlayerController>().movementSpeed = regularMovementSpeed;
        GetComponent<PlayerController>().sprintSpeed = regularSprintSpeed;

        Instantiate(m_battery, hit.transform.position, m_battery.transform.rotation);
        m_holdingBattery = false;
    }
}
