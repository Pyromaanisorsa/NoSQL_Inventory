using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private GameObject playerPreFab;
    [SerializeField] private Camera loginCam;
    [SerializeField] private TestDatabaseConnector database;

    // Spawn Player
    public void SpawnPlayer()
    {
        loginCam.gameObject.SetActive(false);
        PlayerController player = Instantiate(playerPreFab, new Vector3(0, -5, 0), transform.rotation).GetComponent<PlayerController>();
        player.inventoryUI = inventoryUI;
        player.SetupPlayer(database);
        gameObject.SetActive(false);
    }
}
