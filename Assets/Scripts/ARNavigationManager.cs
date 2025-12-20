using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARNavigationManager : MonoBehaviour
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
    public NavMeshAgent userAgent;
    public LineRenderer pathLine;
    public List<Destination> destinationList;
    
    [Header("Settings")]
    public float pathUpdateInterval = 0.2f;
    
    private Transform currentTarget;
    private bool isMapAligned = false;
    private float lastPathUpdateTime = 0f;

    void OnEnable() => imageManager.trackedImagesChanged += OnImageChanged;
    void OnDisable() => imageManager.trackedImagesChanged -= OnImageChanged;

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
        worldContent.rotation = marker.transform.rotation;


        worldContent.gameObject.SetActive(true);
        
        Vector3 cameraPos = mainCamera.position;
        userAgent.Warp(new Vector3(cameraPos.x, 0, cameraPos.z));
        
        isMapAligned = true;
    }

    void Update()
    {
        if (!isMapAligned) return;

        UpdateAgentPosition();

        if (currentTarget != null && Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            DrawPathToTarget();
            lastPathUpdateTime = Time.time;
        }
    }

    void UpdateAgentPosition()
    {
        if (!userAgent.isOnNavMesh) return;

        Vector3 cameraPos = mainCamera.position;
        Vector3 targetPos = new Vector3(cameraPos.x, userAgent.transform.position.y, cameraPos.z);
        
        userAgent.transform.position = targetPos;
        
    }

    void DrawPathToTarget()
    {
        if (!userAgent.isOnNavMesh || currentTarget == null)
        {
            pathLine.enabled = false;
            return;
        }

        NavMeshPath path = new NavMeshPath();
        bool foundPath = NavMesh.CalculatePath(
            userAgent.transform.position,
            currentTarget.position,
            NavMesh.AllAreas,
            path
        );

        if (foundPath && path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 0)
        {
            Vector3[] corners = new Vector3[path.corners.Length];
            for (int i = 0; i < path.corners.Length; i++)
            {
                corners[i] = path.corners[i] + Vector3.up * 0.1f;
            }
            
            pathLine.positionCount = corners.Length;
            pathLine.SetPositions(corners);
            pathLine.enabled = true;
        }
        else
        {
            pathLine.enabled = false;
        }
    }

    public void SetDestinationByIndex(int index)
    {
        if (index >= 0 && index < destinationList.Count)
        {
            currentTarget = GetClosestDoor(destinationList[index].targetTransforms);
            
            if (currentTarget != null)
            {
                pathLine.enabled = true;
                DrawPathToTarget(); 
            }
            else
            {
                pathLine.enabled = false;
                pathLine.positionCount = 0;
                Debug.LogWarning($"Không tìm được đường đến {destinationList[index].roomName}");
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

        Transform bestDoor = null;
        float shortestDistance = Mathf.Infinity;
        NavMeshPath testPath = new NavMeshPath();

        foreach (Transform door in doors)
        {
            if (door == null) continue;

            bool foundPath = NavMesh.CalculatePath(
                userAgent.transform.position,
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

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isMapAligned) return;

        if (userAgent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(userAgent.transform.position, 0.3f);
        }

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
}