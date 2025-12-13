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
        public string roomName;      // Tên hiển thị trên UI (VD: Phòng Họp)
        public Transform targetTransform; // Object vị trí trong Map 3D
    }

    [Header("AR Setup")]
    public ARTrackedImageManager imageManager;
    public Transform worldContent; // Kéo GameObject chứa cả Map vào đây
    public Transform mainCamera;   // Kéo Main Camera vào đây

    [Header("Navigation")]
    public NavMeshAgent userAgent; // Kéo UserAgent vào đây
    public LineRenderer pathLine;  // Kéo LineRenderer vào đây
    public List<Destination> destinationList;
    private Transform currentTarget;
    private bool isMapAligned = false;

    void OnEnable() => imageManager.trackedImagesChanged += OnImageChanged;
    void OnDisable() => imageManager.trackedImagesChanged -= OnImageChanged;

    // 1. Xử lý khi quét được Marker
    void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            // Chỉ cần align lần đầu tiên hoặc khi cần reset
            if (!isMapAligned)
            {
                AlignMapToMarker(trackedImage);
            }
        }
        
        // Nếu cần cập nhật liên tục vị trí map theo marker (cẩn thận bị giật)
        // foreach (var trackedImage in args.updated) { ... }
    }

    void AlignMapToMarker(ARTrackedImage marker)
    {
        // Giả sử trong Map 3D, vị trí đặt Marker ảo chính là gốc (0,0,0) của worldContent
        // Và hướng Forward của worldContent trùng với hướng Up của ảnh Marker
        
        worldContent.position = marker.transform.position;
        worldContent.rotation = marker.transform.rotation;
        
        // Có thể cần xoay chỉnh worldContent 90 hoặc 180 độ tùy vào cách bạn vẽ map so với ảnh
        // worldContent.Rotate(Vector3.up, 180); 

        worldContent.gameObject.SetActive(true); // Hiện map lên
        isMapAligned = true;
    }

    void Update()
    {
        if (!isMapAligned) return;

        // Cập nhật vị trí Agent theo Camera
        Vector3 cameraPos = mainCamera.position;
        userAgent.Warp(
            new Vector3(cameraPos.x, userAgent.transform.position.y, cameraPos.z)
        );

        if (currentTarget != null)
        {
            DrawPathToTarget();
        }
    }

    void DrawPathToTarget()
    {
        if (!userAgent.isOnNavMesh) return;

        NavMeshPath path = new NavMeshPath();
        bool foundPath = NavMesh.CalculatePath(
            userAgent.transform.position,
            currentTarget.position,
            NavMesh.AllAreas,
            path
        );

        if (foundPath && path.status == NavMeshPathStatus.PathComplete)
        {
            pathLine.positionCount = path.corners.Length;
            pathLine.SetPositions(path.corners);
        }
    }

    public void SetDestinationByIndex(int index)
    {
        // Index 0 thường là "Chọn phòng..." nên ta trừ đi hoặc xử lý riêng
        // Giả sử Dropdown dòng đầu tiên là "Vui lòng chọn phòng" -> index thực tế = dropdownIndex - 1
        
        if (index >= 0 && index < destinationList.Count)
        {
            currentTarget = destinationList[index].targetTransform;
            pathLine.enabled = true; // Bật vẽ đường
        }
        else
        {
            currentTarget = null;
            pathLine.enabled = false; // Tắt vẽ đường nếu chọn sai/hủy
            pathLine.positionCount = 0;
        }
    }
}