using UnityEngine;
using TMPro; 
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public TMP_Dropdown roomDropdown;
    public ARNavigationCameraBased navManager;

    void Start()
    {
        InitializeDropdown();
    }

    void InitializeDropdown()
    {
        roomDropdown.ClearOptions();

        List<string> options = new List<string>();

        options.Add("Chọn phòng cần đến...");

        foreach (var dest in navManager.destinationList)
        {
            options.Add(dest.roomName);
        }

        roomDropdown.AddOptions(options);

        roomDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDropdownChanged(int index)
    {
        int realIndex = index - 1;

        if (realIndex >= 0)
        {
            navManager.SetDestinationByIndex(realIndex);
            Debug.Log("Đã chọn: " + navManager.destinationList[realIndex].roomName);
        }
        else
        {
            navManager.SetDestinationByIndex(-1); 
        }
    }
}