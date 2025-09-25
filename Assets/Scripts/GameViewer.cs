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
    [SerializeField] private GameObject tetriminoPrefab;
    [SerializeField] private GameObject scorePrefab;
    [SerializeField] public PiecePlaceholder swapPlaceholderPrefab;
    [SerializeField] private Transform canvasTransform;

    [SerializeField] public TextMeshProUGUI scoreText;

    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        gridV = FindFirstObjectByType<GridViewer>();
        gameM = new GameManager();
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
        moved |= HandleKey(KeyCode.Space, () => gameM.swapCurrentPiece());
        moved |= HandleKey(KeyCode.R, () => resetGame());

        if (moved) {
            gridV.updateGrid(gameM.getGrid());
            gridV.updateGridPositions(
                gameM.getPiecePositions(), 
                gameM.getPieceType()
            );
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

}
