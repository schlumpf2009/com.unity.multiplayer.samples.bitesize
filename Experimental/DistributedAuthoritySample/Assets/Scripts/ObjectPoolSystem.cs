using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generic NetworkObject pool system used throughout the demo.
/// </summary>
public class ObjectPoolSystem : MonoBehaviour, INetworkPrefabInstanceHandler
{
    public static Dictionary<GameObject, ObjectPoolSystem> ExistingPoolSystems = new Dictionary<GameObject, ObjectPoolSystem>();

    public static ObjectPoolSystem GetPoolSystem(GameObject gameObject)
    {
        if (ExistingPoolSystems.ContainsKey(gameObject))
        {
            return ExistingPoolSystems[gameObject];
        }
        return null;
    }

    [Tooltip("The network prefab to pool.")]
    public GameObject NetworkPrefab;

    [Tooltip("How many instances of the network prefab you want available")]
    public int ObjectPoolSize;

    [Tooltip("For organization purposes: when true, non-spawned instances will be migrated to the object pool's scene. (default is true)")]
    public bool PoolInSystemScene = true;

    [Tooltip("When enabled, the pool will be used to spawn/recylce NetworkObjects")]
    public bool UsePoolForSpawn = true;

    [Tooltip("Enable this to persist the pool objects between sessions (after first load, the pool is pre-loaded).")]
    public bool DontDestroyOnSceneUnload = false;

    [Tooltip("When true, the spawned objects will be configured to use unreliable deltas. Use this option to prevent stutter if packets are dropped due to poor network conditions.")]
    public bool UseUnreliableDeltas = true;

    [Tooltip("When true, debug info will be logged about when objects are despawned and returned to the pool.")]
    public bool DebugHandlerDestroy = false;

    [Tooltip("When enabled, the below transform settings are applied to all spawned NetworkObjects.")]
    public bool EnableTransformOverrides;
    public bool HalfFloat;
    public bool QuaternionSynchronization;
    public bool QuaternionCompression;
    public bool Interpolate;

    [Tooltip("When enabled, this pool will rebuild itself each time it is initialized upon loading a scene.")]
    public bool ForceRebuildPool;

    private Stack<NetworkObject> m_AvailableObjects = new Stack<NetworkObject>();

    private NetworkVariable<bool> m_UsePoolForSpawn = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// When a pooled object's state changes (active to not-active in the scene hierarchy), this method is invoked.
    /// </summary>
    private void HandleInstanceStateChange(GameObject instance, bool isSpawning = false)
    {
        if (PoolInSystemScene)
        {
            if (!isSpawning)
            {
                if (instance.transform.parent != null)
                {
                    instance.transform.SetParent(null);
                }
                if (gameObject.scene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
                }
            }
            else
            {
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
            }
        }
        instance.SetActive(isSpawning);
    }

    private void Start()
    {
        if (ForceRebuildPool && ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            CleanOutPool();
        }
        Initialize();
    }

    private void Initialize()
    {
        if (!ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(NetworkPrefab, this);
            ExistingPoolSystems.Add(NetworkPrefab, this);
            if (DontDestroyOnSceneUnload)
            {
                DontDestroyOnLoad(gameObject);
            }
            StartCoroutine(CreatePrefabPool());
        }
        else
        {
            // This is registers the prefab handler with NetworkManager
            NetworkManager.Singleton.PrefabHandler.AddHandler(NetworkPrefab, ExistingPoolSystems[NetworkPrefab]);

            // This provides the mechanism that tracks the status of the object pool instance when first instantiating all objects.
            NetworkManagerHelper.Instance.TrackPoolSystemLoading(ExistingPoolSystems[NetworkPrefab], 1.0f);
            if (DontDestroyOnSceneUnload)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (ForceRebuildPool && ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            CleanOutPool();
        }
    }

    private void OnClientStarted()
    {
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    private void OnClientStopped(bool obj)
    {
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        if (ForceRebuildPool && ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(NetworkPrefab);
            CleanOutPool();
            Initialize();
        }
    }

    /// <summary>
    /// Coroutine that instantiates all of the objects over time
    /// </summary>
    private IEnumerator CreatePrefabPool()
    {
        var splitCount = (int)ObjectPoolSize * 0.1f;

        while (m_AvailableObjects.Count < ObjectPoolSize)
        {
            for (int i = 0; i < splitCount; i++)
            {
                var instance = Instantiate(NetworkPrefab);
                instance.name = instance.name.Replace("(Clone)", "");
                instance.name += $"_{m_AvailableObjects.Count}";
                HandleInstanceStateChange(instance);
                instance.GetComponent<NetworkObject>().SetSceneObjectStatus();
                var networkTransforms = instance.GetComponentsInChildren<NetworkTransform>();

                foreach (var networkTransform in networkTransforms)
                {
                    networkTransform.UseUnreliableDeltas = UseUnreliableDeltas;
                    if (networkTransform != null && EnableTransformOverrides)
                    {
                        networkTransform.UseHalfFloatPrecision = HalfFloat;
                        networkTransform.UseQuaternionSynchronization = QuaternionSynchronization;
                        networkTransform.UseQuaternionCompression = QuaternionCompression;
                        networkTransform.Interpolate = Interpolate;
                    }
                }
                m_AvailableObjects.Push(instance.GetComponent<NetworkObject>());
                if (m_AvailableObjects.Count >= ObjectPoolSize)
                {
                    break;
                }
                NetworkManagerHelper.Instance.TrackPoolSystemLoading(this, m_AvailableObjects.Count / (float)ObjectPoolSize);
            }
            yield return null;
        }
        NetworkManagerHelper.Instance.TrackPoolSystemLoading(this, m_AvailableObjects.Count / (float)ObjectPoolSize);
    }

    private void CleanOutPool()
    {
        foreach (var poolObject in m_AvailableObjects)
        {
            if (poolObject != null && poolObject.gameObject != null)
            {
                Destroy(poolObject.gameObject);
            }
        }

        m_AvailableObjects.Clear();
        ExistingPoolSystems.Remove(NetworkPrefab);
    }

    /// <summary>
    /// The owner will use this method to pull already existing objects from the pool
    /// </summary>

    public NetworkObject GetInstance(bool isSpawningLocally = false)
    {
        var returnValue = (NetworkObject)null;

        if (m_UsePoolForSpawn.Value && m_AvailableObjects.TryPop(out NetworkObject instance))
        {
            HandleInstanceStateChange(instance.gameObject, true);
            instance.DontDestroyWithOwner = false;
            instance.DeferredDespawnTick = 0;
            returnValue = instance;
        }
        else
        {
            if (m_UsePoolForSpawn.Value && NetworkManager.Singleton.LogLevel >= LogLevel.Developer)
            {
                NetworkLog.LogWarningServer($"[Object Pool ({name}) Exhausted] Instantiating new instances during network session!");
            }
            returnValue = Instantiate(NetworkPrefab).GetComponent<NetworkObject>();
            returnValue.gameObject.name += "_NP";
        }

        if (isSpawningLocally)
        {
            var networkTransform = returnValue.GetComponent<NetworkTransform>();
            if (networkTransform != null && EnableTransformOverrides)
            {
                networkTransform.UseHalfFloatPrecision = HalfFloat;
                networkTransform.UseQuaternionSynchronization = QuaternionSynchronization;
                networkTransform.UseQuaternionCompression = QuaternionCompression;
                networkTransform.Interpolate = Interpolate;
            }
        }
        return returnValue;
    }

    /// <summary>
    /// Non-owners will have this method called when the object is spawned on their side.
    /// </summary>
    /// <returns>the object instance to spawn locally</returns>
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        var instance = GetInstance(!NetworkManager.Singleton.DistributedAuthorityMode && NetworkManager.Singleton.LocalClientId == ownerClientId);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }



    /// <summary>
    /// Invoked when a spawned object from the pool is despawned and destroyed
    /// </summary>
    public void Destroy(NetworkObject networkObject)
    {
        if (!m_UsePoolForSpawn.Value && networkObject.gameObject.name.Contains("_NP"))
        {
            Destroy(networkObject.gameObject);
        }
        else
        {
            if (!DebugHandlerDestroy)
            {
                HandleInstanceStateChange(networkObject.gameObject);
                m_AvailableObjects.Push(networkObject);
            }
            else
            {
                if (networkObject.IsSpawned)
                {
                    Debug.LogError($"[{networkObject.name}] Is still spawned but is being put back into pool!");
                }
                if (!m_AvailableObjects.Contains(networkObject))
                {
                    HandleInstanceStateChange(networkObject.gameObject);
                    m_AvailableObjects.Push(networkObject);
                }
                else
                {
                    Debug.LogError($"[ObjectPoolSystem] PrefabHandler invoked twice for {networkObject.name}!");
                }
            }
            networkObject.transform.position = Vector3.zero;
            networkObject.transform.rotation = Quaternion.identity;
        }
    }
}


