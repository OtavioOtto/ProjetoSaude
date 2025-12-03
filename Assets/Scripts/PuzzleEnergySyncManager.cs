using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PuzzleEnergySyncManager : MonoBehaviourPunCallbacks
{
    public static PuzzleEnergySyncManager Instance { get; private set; }

    private bool morfeusReady = false;
    private bool alexcraxyReady = false;
    private bool syncComplete = false;

    // Chamado quando Morfeus ativa seu puzzle
    [PunRPC]
    public void MorfeusActivated()
    {
        morfeusReady = true;
        Debug.Log("Morfeus activated puzzle");
        CheckSyncStatus();
    }

    // Chamado quando AlexCraxy ativa seu puzzle
    [PunRPC]
    public void AlexCraxyActivated()
    {
        alexcraxyReady = true;
        Debug.Log("AlexCraxy activated puzzle");
        CheckSyncStatus();
    }

    void CheckSyncStatus()
    {
        if (morfeusReady && alexcraxyReady && !syncComplete)
        {
            syncComplete = true;
            Debug.Log("Both puzzles synchronized!");

            // Ativar ambos os handlers
            photonView.RPC("ActivateBothHandlers", RpcTarget.All);
        }
    }

    [PunRPC]
    void ActivateBothHandlers()
    {
        // Ativar handlers em ambos os clients
        MapPuzzleHandler mapHandler = FindFirstObjectByType<MapPuzzleHandler>();
        ReactivateEnergyHandler energyHandler = FindFirstObjectByType<ReactivateEnergyHandler>();

        if (mapHandler != null)
        {
            mapHandler.puzzleActive = true;
            mapHandler.puzzleComplete = false;
        }

        if (energyHandler != null)
        {
            energyHandler.puzzleActive = true;
            energyHandler.puzzleComplete = false;
        }
    }

    public void LocalPlayerActivatedPuzzle(int playerType)
    {
        if (playerType == 1) // Morfeus
        {
            photonView.RPC("MorfeusActivated", RpcTarget.Others);
            morfeusReady = true;
        }
        else if (playerType == 2) // AlexCraxy
        {
            photonView.RPC("AlexCraxyActivated", RpcTarget.Others);
            alexcraxyReady = true;
        }

        CheckSyncStatus();
    }

    public bool IsSyncComplete()
    {
        return syncComplete;
    }
}