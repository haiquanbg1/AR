using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using TMPro; // Thư viện cho UI mới

[System.Serializable]
public class RoomData
{
    public string roomName;
    public List<Transform> doors; 
}

public class NavigationManager : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public TMP_Dropdown roomDropdown; // Kéo cái UI Dropdown vào đây

    [Header("Data")]
    public List<RoomData> allRooms;

    private Transform currentActiveDoor = null;

    void Start()
    {
        // Tự động ẩn tất cả các cửa khi bắt đầu game để đỡ phải chỉnh tay
        foreach(var room in allRooms)
        {
            foreach(var door in room.doors)
            {
                if(door.GetComponent<Renderer>())
                    door.GetComponent<Renderer>().enabled = false;
            }
        }

        // Tự động nạp tên phòng vào Dropdown
        SetupDropdown();
    }

    void SetupDropdown()
    {
        roomDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var room in allRooms)
        {
            options.Add(room.roomName);
        }
        roomDropdown.AddOptions(options);
    }

    // Hàm này sẽ gắn vào nút Bấm (Button)
    public void OnStartNavigationCheck()
    {
        // Lấy tên phòng đang chọn trong Dropdown
        string selectedName = roomDropdown.options[roomDropdown.value].text;
        
        // Gọi hàm đi chuyển
        GoToRoom(selectedName);
    }

    public void GoToRoom(string targetRoomName)
    {
        RoomData targetRoom = allRooms.Find(r => r.roomName == targetRoomName);
        if (targetRoom == null) return;

        Transform bestDoor = GetClosestDoor(targetRoom);

        if (bestDoor != null)
        {
            if (currentActiveDoor != null)
                currentActiveDoor.GetComponent<Renderer>().enabled = false;

            currentActiveDoor = bestDoor;
            if (currentActiveDoor.GetComponent<Renderer>())
                currentActiveDoor.GetComponent<Renderer>().enabled = true;

            agent.SetDestination(bestDoor.position);
        }
    }

    Transform GetClosestDoor(RoomData room)
    {
        // (Giữ nguyên logic tính toán như cũ)
        Transform bestDoor = null;
        float shortestDistance = Mathf.Infinity;
        NavMeshPath path = new NavMeshPath();

        foreach (Transform door in room.doors)
        {
            if (agent.CalculatePath(door.position, path))
            {
                float distance = GetPathLength(path);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestDoor = door;
                }
            }
        }
        return bestDoor;
    }

    float GetPathLength(NavMeshPath path)
    {
        float length = 0.0f;
        if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 1)
        {
            for (int i = 1; i < path.corners.Length; ++i)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
        }
        return length;
    }
}