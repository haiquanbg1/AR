using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARNavigationCameraBased : MonoBehaviour
{
    [System.Serializable]
    public struct Destination
    {
        public string roomName;
        public List<Transform> targetTransforms;
    }

    [Header("AR Setup")]
    public ARTrackedImageManager imageManager;
    public Transform worldContent;
    public Transform mainCamera;

    [Header("Navigation")]
    public LineRenderer pathLine;
    public List<Destination> destinationList;
    
    [Header("Settings")]
    public float pathUpdateInterval = 0.3f;
    public float pathHeightOffset = 0.1f; 
    
    private Transform currentTarget;
    private bool isMapAligned = false;
    private float lastPathUpdateTime = 0f;
    private Vector3 lastCameraPosition;
    private float cameraMovementThreshold = 0.1f; 

    void OnEnable() => imageManager.trackedImagesChanged += OnImageChanged;
    void OnDisable() => imageManager.trackedImagesChanged -= OnImageChanged;

    void Start()
    {
        if (pathLine != null)
        {
            pathLine.enabled = false;
        }
        lastCameraPosition = mainCamera.position;
    }

    void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            if (!isMapAligned)
            {
                AlignMapToMarker(trackedImage);
            }
        }
    }

    void AlignMapToMarker(ARTrackedImage marker)
    {
        worldContent.position = marker.transform.position;
        worldContent.rotation = Quaternion.Euler(
            0,
            marker.transform.eulerAngles.y,
            0
        );


        worldContent.gameObject.SetActive(true);
        isMapAligned = true;
        
        Debug.Log("✅ Map aligned to marker!");
    }

    void Update()
    {
        if (!isMapAligned || currentTarget == null) return;

        bool cameraMoved = Vector3.Distance(mainCamera.position, lastCameraPosition) > cameraMovementThreshold;
        bool timeToUpdate = Time.time - lastPathUpdateTime > pathUpdateInterval;

        if (cameraMoved || timeToUpdate)
        {
            DrawPathFromCameraToTarget();
            lastCameraPosition = mainCamera.position;
            lastPathUpdateTime = Time.time;
        }
    }

    void DrawPathFromCameraToTarget()
    {
        if (currentTarget == null)
        {
            pathLine.enabled = false;
            return;
        }

        Vector3 startPos = mainCamera.position;
        
        NavMeshHit startHit;
        if (!NavMesh.SamplePosition(startPos, out startHit, 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Camera không ở gần NavMesh!");
            pathLine.enabled = false;
            return;
        }

        NavMeshPath path = new NavMeshPath();
        bool foundPath = NavMesh.CalculatePath(
            startHit.position,
            currentTarget.position,
            NavMesh.AllAreas,
            path
        );

        if (foundPath && path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 0)
        {
            Vector3[] corners = new Vector3[path.corners.Length];
            for (int i = 0; i < path.corners.Length; i++)
            {
                corners[i] = path.corners[i] + Vector3.up * pathHeightOffset;
            }
            
            pathLine.positionCount = corners.Length;
            pathLine.SetPositions(corners);
            pathLine.enabled = true;
        }
        else
        {
            pathLine.enabled = false;
            Debug.LogWarning($"Không tìm được đường đến {currentTarget.name}");
        }
    }

    public void SetDestinationByIndex(int index)
    {
        if (index >= 0 && index < destinationList.Count)
        {
            currentTarget = GetClosestDoor(destinationList[index].targetTransforms);
            
            if (currentTarget != null)
            {
                DrawPathFromCameraToTarget(); // Vẽ ngay
                Debug.Log($"✅ Đã chọn: {destinationList[index].roomName}");
            }
            else
            {
                pathLine.enabled = false;
                Debug.LogWarning($"❌ Không tìm được đường đến {destinationList[index].roomName}");
            }
        }
        else
        {
            currentTarget = null;
            pathLine.enabled = false;
            pathLine.positionCount = 0;
        }
    }

    Transform GetClosestDoor(List<Transform> doors)
    {
        if (doors == null || doors.Count == 0) return null;
        if (doors.Count == 1) return doors[0];

        Vector3 cameraPos = mainCamera.position;
        NavMeshHit hit;
        
        if (!NavMesh.SamplePosition(cameraPos, out hit, 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Camera không ở gần NavMesh khi tìm cửa gần nhất!");
            return doors[0]; 
        }

        Transform bestDoor = null;
        float shortestDistance = Mathf.Infinity;
        NavMeshPath testPath = new NavMeshPath();

        foreach (Transform door in doors)
        {
            if (door == null) continue;

            bool foundPath = NavMesh.CalculatePath(
                hit.position,
                door.position,
                NavMesh.AllAreas,
                testPath
            );

            if (foundPath && testPath.status == NavMeshPathStatus.PathComplete)
            {
                float pathLength = GetPathLength(testPath);
                
                if (pathLength < shortestDistance)
                {
                    shortestDistance = pathLength;
                    bestDoor = door;
                }
            }
        }

        return bestDoor;
    }

    float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        
        if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 1)
        {
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
        }
        
        return length;
    }

    public float GetRemainingDistance()
    {
        if (currentTarget == null || !isMapAligned) return -1f;

        NavMeshPath path = new NavMeshPath();
        Vector3 cameraPos = mainCamera.position;
        NavMeshHit hit;
        
        if (NavMesh.SamplePosition(cameraPos, out hit, 2f, NavMesh.AllAreas))
        {
            if (NavMesh.CalculatePath(hit.position, currentTarget.position, NavMesh.AllAreas, path))
            {
                return GetPathLength(path);
            }
        }
        
        return -1f;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isMapAligned) return;

        if (mainCamera != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(mainCamera.position, 0.3f);
        }

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
}