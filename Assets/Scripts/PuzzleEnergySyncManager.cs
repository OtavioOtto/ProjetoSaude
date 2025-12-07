using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PuzzleEnergySyncManager : MonoBehaviourPunCallbacks
{
    public static PuzzleEnergySyncManager Instance { get; private set; }

    private bool morfeusReady = false;
    private bool alexcraxyReady = false;
    private bool syncComplete = false;

    [PunRPC]
    public void MorfeusActivated()
    {
        morfeusReady = true;
        CheckSyncStatus();
    }

    [PunRPC]
    public void AlexCraxyActivated()
    {
        alexcraxyReady = true;
        CheckSyncStatus();
    }

    void CheckSyncStatus()
    {
        if (morfeusReady && alexcraxyReady && !syncComplete)
        {
            syncComplete = true;

            photonView.RPC("ActivateBothHandlers", RpcTarget.All);
        }
    }

    [PunRPC]
    void ActivateBothHandlers()
    {
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
        if (playerType == 1)
        {
            photonView.RPC("MorfeusActivated", RpcTarget.Others);
            morfeusReady = true;
        }
        else if (playerType == 2)
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