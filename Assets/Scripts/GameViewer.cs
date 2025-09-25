using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameViewer : MonoBehaviour {
    // ======================= Managers =======================
    private GridViewer gridV; // Do to look for it every time
    private GameManager gameM;

    // ========================= PIECES =========================
    [SerializeField] public GameObject scorePrefab;
    [SerializeField] public Transform canvasTransform;
    [SerializeField] public TextMeshProUGUI scoreText;

    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        gameM = new GameManager();
        gridV = FindFirstObjectByType<GridViewer>();
        gridV.Init();
        updateGridViewe();
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {
        bool moved = false;

        // Movement keys
        moved |= HandleKey(KeyCode.A, () => gameM.moveLeft());
        moved |= HandleKey(KeyCode.W, () => gameM.moveUp());
        moved |= HandleKey(KeyCode.D, () => gameM.moveRight());
        moved |= HandleKey(KeyCode.S, () => gameM.moveDown());

        // Rotation keys
        moved |= HandleKey(KeyCode.LeftArrow, () => gameM.rotateAClock());
        moved |= HandleKey(KeyCode.RightArrow, () => gameM.rotateClock());
        moved |= HandleKey(KeyCode.DownArrow, () => gameM.rotateR180());
        moved |= HandleKey(KeyCode.UpArrow, () => gameM.swapCurrentPiece());

        // Other actions
        moved |= HandleKey(KeyCode.Space, () => {
            gameM.moveCurrentPieceBootom(); 
            gameM.lockPiece(); 
        });
        moved |= HandleKey(KeyCode.R, () => resetGame());

        if (moved) {
            updateGridViewe();
        }
    }

    private bool HandleKey(KeyCode key, System.Action action) {
        if (Input.GetKeyDown(key)) {
            action();
            return true;
        }
        return false;
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public void resetGame() {
        gridV.resetGrid();
        gameM.resetGame();
    }

    public void updateGridViewe() {
        gridV.updateGrid(gameM.getGrid());
        gridV.updateGridPositions(
            gameM.getPiecePositions(),
            gameM.getPieceType()
        );
        gridV.updateSwapPiece(gameM.getSwapPieceType());
    }
}
