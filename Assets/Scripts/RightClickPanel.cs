using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RightClickPanel : MonoBehaviour
{
    public GameObject buttonPrefab; // A prefab for buttons in the context menu
    public Transform buttonContainer; // A container (e.g., Vertical Layout Group) for the buttons
    public RectTransform panelTransform;
    public List<string> keys;

    // Dictionary to store functions from InventoryUI
    public Dictionary<string, Action> actions = new Dictionary<string, Action>();

    public void SetupMenu()
    {
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Create buttons for each action/function
        foreach (KeyValuePair<string, Action> action in actions)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();

            // Set button label and assign action/function
            buttonText.text = action.Key;
            button.onClick.AddListener(() => action.Value.Invoke());
        }

        // Set context panel height
        float newHeight = actions.Count * 26;
        panelTransform.sizeDelta = new Vector2(panelTransform.sizeDelta.x, newHeight);
    }

    public void Show(Vector2 position)
    {
        gameObject.SetActive(true);
        transform.position = position;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ClearActions() 
    {
        actions.Clear();
    }
}
