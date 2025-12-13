using UnityEngine;
using TMPro; // Thư viện TextMeshPro
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public TMP_Dropdown roomDropdown;
    public ARNavigationManager navManager;

    void Start()
    {
        InitializeDropdown();
    }

    void InitializeDropdown()
    {
        // 1. Xóa hết các options mặc định (Option A, Option B...)
        roomDropdown.ClearOptions();

        // 2. Tạo list tên mới
        List<string> options = new List<string>();

        // Thêm dòng đầu tiên mặc định
        options.Add("Chọn phòng cần đến...");

        // 3. Lấy tên từ NavManager đổ vào
        foreach (var dest in navManager.destinationList)
        {
            options.Add(dest.roomName);
        }

        // 4. Add vào Dropdown
        roomDropdown.AddOptions(options);

        // 5. Đăng ký sự kiện khi chọn
        roomDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDropdownChanged(int index)
    {
        // Vì dòng 0 là "Chọn phòng...", nên index thực tế trong List destinations là index - 1
        int realIndex = index - 1;

        if (realIndex >= 0)
        {
            navManager.SetDestinationByIndex(realIndex);
            Debug.Log("Đã chọn: " + navManager.destinationList[realIndex].roomName);
        }
        else
        {
            // Nếu chọn lại dòng 0 -> Hủy dẫn đường
            navManager.SetDestinationByIndex(-1); 
        }
    }
}