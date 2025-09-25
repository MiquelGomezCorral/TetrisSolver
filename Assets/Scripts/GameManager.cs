using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager {
    // ========================================================
    //                          Managers
    // ========================================================
    private GridManager gridM; // Do to look for it every time

    // ========================================================
    //                          PIECES
    // ========================================================
    private Tetrimino currentPiece;
    private TetriminoEnum currentPieceType = TetriminoEnum.X;
    private TetriminoEnum swapPieceType = TetriminoEnum.X;

    private int score;

    // is okey to be empty, fist call will add 7, then take 1 so 6 and in the
    // next call will add 7 more so 13. No need to initialize here.
    public Queue<TetriminoEnum> bagQueue = new Queue<TetriminoEnum>();
    // ========================================================
    //                          CONSTRUCTOR
    // ========================================================
    public GameManager() {
        gridM = new GridManager(TetriminoSettings.width, TetriminoSettings.height);

        // Add two new bags
        fillRandomBag();
        fillRandomBag();

        // Spawn piece
        currentPieceType = bagQueue.Dequeue();
        currentPiece = new Tetrimino(gridM, currentPieceType);
    }

    // ========================================================
    //                          MOVEMENTS
    // ========================================================
    public void moveCurrentPieceSide(DirectionEnum direction) {
        currentPiece.movePieze(direction);
    }
    public void rotateCurrentPiece(RorateEnum direction) {
        currentPiece.rotatePiece(direction);
    }
    public void moveCurrentPieceBootom() {
        currentPiece.movePieceBootom();
    }

    public void lockPiece() {
        gridM.lockPiece(getPiecePositions(), currentPieceType, currentPiece.lastAction);
        getNewRandomPiece();
    }


    // Maybe more than one movement
    public void moveLeft() {
        moveCurrentPieceSide(DirectionEnum.LEFT);
    }
    public void moveUp() {
        moveCurrentPieceSide(DirectionEnum.UP);
    }
    public void moveRight() {
        moveCurrentPieceSide(DirectionEnum.RIGHT);
    }
    public void moveDown() {
        moveCurrentPieceSide(DirectionEnum.DOWN);
    }


    public void rotateAClock() {
        rotateCurrentPiece(RorateEnum.ACLOCK);
    }
    public void rotateClock() {
        rotateCurrentPiece(RorateEnum.CLOCK);
    }
    public void rotateR180() {
        rotateCurrentPiece(RorateEnum.R180);
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public void resetGame(Queue<TetriminoEnum> bagQueueSaved = null) {
        // Get get new piece
        if (bagQueueSaved != null) 
            bagQueue = new Queue<TetriminoEnum>(bagQueueSaved);

        getNewRandomPiece();
        swapPieceType = TetriminoEnum.X;

        // Reset grid
        gridM.resetGrid();

        // Reset socre
        score = 0;
    }


    public void swapCurrentPiece() {
        if (currentPieceType == TetriminoEnum.X && swapPieceType == TetriminoEnum.X)
            return;

        // If no piece get a new one
        if (swapPieceType == TetriminoEnum.X) {
            swapPieceType = getNewRandomPiece();
        }

        // Swap types
        TetriminoEnum auxPieceType = currentPieceType;
        currentPieceType = swapPieceType;
        swapPieceType = auxPieceType;

        currentPiece.resetPeace(currentPieceType);
    }

    // ========================================================
    //                      VALUE ACCESS
    // ========================================================
    public TetriminoEnum[,] getGrid() {
        return gridM.getGrid();
    }
    public int getScore() {
        return score;
    }
    public TetriminoEnum getSwapPieceType() {
        return swapPieceType;
    }

    // ========================================================
    //                      GET PIECE
    // ========================================================
    public List<Vector2Int> getPiecePositions() {
        return currentPiece.getAbsPositions();
    }
    public TetriminoEnum getPieceType() {
        return currentPieceType;
    }
    public void fillRandomBag() {
        List<TetriminoEnum> newBag = TetriminoSettings.produceRandomBag();

        // Enque new pieces
        foreach (TetriminoEnum newPiece in newBag)
            bagQueue.Enqueue(newPiece);
    }
    public TetriminoEnum getNewRandomPiece() {
        if (bagQueue.Count < 7) 
            fillRandomBag();

        currentPieceType = bagQueue.Dequeue();
        currentPiece.resetPeace(currentPieceType);

        return currentPieceType;
    }
    public List<TetriminoEnum> PeekNext(int n) {
        List<TetriminoEnum> nextPieces = new List<TetriminoEnum>();
        int i = 0;
        foreach (var piece in bagQueue) {
            if (i >= n) break;
            nextPieces.Add(piece);
            i++;
        }
        return nextPieces;
    }
}
