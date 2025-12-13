using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SetNavigationTarget : MonoBehaviour
{
    [SerializeField]
    private Camera topDownCamera;
    [SerializeField]
    private GameObject navTargetObject;

    private NavMeshPath path;
    private LineRenderer line;
    private bool lineToggle = false;

    private void Start()
    {
        path = new NavMeshPath();
        line = GetComponent<LineRenderer>();
        
        // Thêm LineRenderer nếu chưa có
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }
        
        // Cấu hình LineRenderer
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.red;
        line.endColor = Color.yellow;
        line.useWorldSpace = true;
        line.enabled = false;
        
        // Kiểm tra NavMesh khi start
        CheckNavMesh();
    }

    private void Update()
    {
        // Toggle khi touch
        if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Began))
        {
            lineToggle = !lineToggle;
        }

        if (lineToggle && navTargetObject != null)
        {
            // Tính toán path
            bool pathFound = NavMesh.CalculatePath(
                transform.position, 
                navTargetObject.transform.position, 
                NavMesh.AllAreas, 
                path
            );
            
            // Hiển thị đường path
            if (pathFound && path.corners.Length > 0)
            {
                line.positionCount = path.corners.Length;
                line.SetPositions(path.corners);
                line.enabled = true;
            }
            else
            {
                line.enabled = false;
            }
        }
        else
        {
            line.enabled = false;
        }
    }
    
    // Kiểm tra NavMesh
    private void CheckNavMesh()
    {
        NavMeshHit hit;
        bool startOnNavMesh = NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas);
        
        if (navTargetObject != null)
        {
            bool targetOnNavMesh = NavMesh.SamplePosition(navTargetObject.transform.position, out hit, 5.0f, NavMesh.AllAreas);
        }
    }
}