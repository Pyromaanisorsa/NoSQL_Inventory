using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class PlayerLogin : MonoBehaviour
{
    [SerializeField] private TestDatabaseConnector dbConnector;
    [SerializeField] private PlayerSpawner spawner;
    public TMP_InputField userField;
    public TMP_InputField passField;
    public Button loginButton;
    public Button registerButton;
    private bool working = false;

    // Limit inputfield character limits and add OnClick functions to buttons
    private void Awake()
    {
        userField.characterLimit = 15;
        passField.characterLimit = 15;
        loginButton.onClick.AddListener(() => LoginAccount());
        registerButton.onClick.AddListener(() => RegisterAccount());
        userField.onValueChanged.AddListener(EnforceFirstCharUppercase);
    }

    // Try to login to account; if succesful, start the game
    private async Task LoginAccount() 
    {
        // Only one action at a time allowed, no trolling overloading the database
        if (working)
            return;
        working = true;

        // No short username or passwords
        if (!IsFieldMininumLength()) 
        {
            working = false;
            return;
        }

        // Try login player and then spawn player
        if (await dbConnector.LoginPlayer(userField.text, passField.text))
            SpawnPlayer();
        else
            working = false;
    }

    // Try to register account; if succesful, login and start the game
    private async Task RegisterAccount() 
    {
        // Only one action at a time allowed, no trolling overloading the database
        if (working)
            return;
        working = true;

        // No short username or passwords
        if (!IsFieldMininumLength())
        {
            working = false;
            return;
        }

        // Try registering player and then spawn player
        if (await dbConnector.RegisterPlayer(userField.text, passField.text))
            SpawnPlayer();
        else
            working = false;
    }

    // Spawn player and disable the login panel
    private void SpawnPlayer()
    {
        spawner.SpawnPlayer();
        gameObject.SetActive(false);
    }

    // Check if username and/or password fields are long enough
    private bool IsFieldMininumLength()
    {
        if (userField.text.Length < 3 && passField.text.Length < 3)
        {
            MessagePanel.Instance.DisplayMessage("Username and password must be 3-15 characters length.");
            return false;
        }
        return true;
    }

    // Force the first character in userField to be uppercase
    private void EnforceFirstCharUppercase(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            // Capitalize the first character and preserve the rest of the text
            string modifiedText = char.ToUpper(text[0]) + text.Substring(1);

            // Update the input field only if the text has changed
            if (modifiedText != text)
            {
                userField.text = modifiedText;

                // Move the caret to the end to avoid disrupting the user
                userField.caretPosition = modifiedText.Length;
            }
        }
    }
}