using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameViewer : MonoBehaviour {
    // ========================================================
    //                          Managers
    // ========================================================
    private GridViewer gridV; // Do to look for it every time


    // ========================================================
    //                          PIECES
    // ========================================================
    [SerializeField] private GameObject tetriminoPrefab;
    [SerializeField] private Tetrimino currentPiece;
    public Tetrimino CurrentPiece {
        get => currentPiece;
        set => currentPiece = value;
    }
    [SerializeField] private TetriminoEnum currentPieceType = TetriminoEnum.X;
    public TetriminoEnum CurrentPieceType {
        get => currentPieceType;
        set => currentPieceType = value;
    }
    [SerializeField] private TetriminoEnum swapPieceType = TetriminoEnum.X;
    public TetriminoEnum SwapPieceType {
        get => swapPieceType;
        set => swapPieceType = value;
    }

    [SerializeField] private int score;
    private int Score {
        get => score;
        set {
            score = value;
            if (scoreText != null)
                scoreText.text = "Score: " + score.ToString();
        }
    }
    [SerializeField] public TextMeshProUGUI scoreText;


    // ========================================================
    //                          QUEUE
    // ========================================================
    // is okey to be empty, fist call will add 7, then take 1 so 6 and in the
    // next call will add 7 more so 13. No need to initialize here.
    public Queue<TetriminoEnum> bagQueue = new Queue<TetriminoEnum>();


    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        gridV = FindFirstObjectByType<GridViewer>();

        // add two new bags
        produceRandomBag();
        produceRandomBag();

        // Spawn the piece
        spawnNewPiece();
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {
        // Maybe more than one movement
        if (Input.GetKeyDown(KeyCode.A)) {
            currentPiece.movePieze(DirectionEnum.LEFT);
        } if (Input.GetKeyDown(KeyCode.W)) {
            currentPiece.movePieze(DirectionEnum.UP);
        } if (Input.GetKeyDown(KeyCode.D)) {
            currentPiece.movePieze(DirectionEnum.RIGHT);
        } if (Input.GetKeyDown(KeyCode.S)) {
            currentPiece.movePieze(DirectionEnum.DOWN);
        }

        // Only one rotation
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            currentPiece.rotatePiece(RorateEnum.ACLOCK);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            currentPiece.rotatePiece(RorateEnum.CLOCK);
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            currentPiece.rotatePiece(RorateEnum.R180);
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            swapCurrentPiece();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            spawnNewPiece();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            resetGame();
        }
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public void resetGame(Queue<TetriminoEnum> bagQueueSaved = null) {
        if (bagQueueSaved != null) {
            bagQueue.Clear();
            // Create a temporary copy to avoid modifying the passed queue
            Queue<TetriminoEnum> tempSaved = new Queue<TetriminoEnum>(bagQueueSaved);

            while (tempSaved.Count > 0) {
                bagQueue.Enqueue(tempSaved.Dequeue());
            }
        }

        gridV.resetGrid();

        currentPieceType = getRandomPiece();
        currentPiece.resetPeace();

        swapPieceType = TetriminoEnum.X;
        gridV.swapPlaceholder.changeType(TetriminoEnum.X);
        
        Score = 0;
    }

    public void spawnNewPiece() {
        if (currentPiece == null) {
            currentPieceType = getRandomPiece();
            Instantiate(tetriminoPrefab);
        } else {
            currentPiece.lockPeace();
            currentPieceType = getRandomPiece();
            currentPiece.resetPeace();
        }
    }

    public void swapCurrentPiece() {
        if (currentPieceType == TetriminoEnum.X && swapPieceType == TetriminoEnum.X)
            return;

        // If no piece get a new one
        if (swapPieceType == TetriminoEnum.X) {
            swapPieceType = getRandomPiece();
        }

        // Swap types
        TetriminoEnum auxPieceType = currentPieceType;
        currentPieceType = swapPieceType;
        swapPieceType = auxPieceType;

        // swap with new current type,will take it from this class
        currentPiece.swapPiece();

        return;
    }

    public void moveCurrentPieceSide(DirectionEnum direction) { 
        currentPiece.movePieze(direction);
    }
    public void rotateCurrentPiece(RorateEnum direction) {
         currentPiece.rotatePiece(direction);
    }
    public void moveCurrentPieceBootom() {
        currentPiece.movePieceBootom();
    }

    // ========================================================
    //                          SCORE
    // ========================================================
    public int addScore(int points) {
        Score += points;
        return Score;
    }
    public int getScore() {
        return Score;
    }

    // ========================================================
    //                      GET PIECE
    // ========================================================
    public void produceRandomBag() {
        List<TetriminoEnum> newBag = new List<TetriminoEnum>(TetriminoSettings.basePiecesBag);

        // Suffle the bags with Fisher–Yates 
        for (int i = 0; i < newBag.Count; i++) {
            int j = UnityEngine.Random.Range(0, newBag.Count); // 1..Count-1
            TetriminoEnum tmp = newBag[i];
            newBag[i] = newBag[j];
            newBag[j] = tmp;
        }

        // Enque new pieces
        foreach (TetriminoEnum newPiece in newBag)
            bagQueue.Enqueue(newPiece);
    }
    public TetriminoEnum getRandomPiece() {
        if (bagQueue.Count < 7) {
            produceRandomBag();
        }
        return bagQueue.Dequeue();
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

    public Queue<TetriminoEnum> getBagsCopy(bool includeCurrent = true) {
        // build a temporary list (does not touch the real bagQueue)
        Queue<TetriminoEnum> newBags =  new Queue<TetriminoEnum>();

        if (includeCurrent && currentPieceType != TetriminoEnum.X)
            newBags.Enqueue(currentPieceType);

        // Create a temporary copy to iterate without modifying original
        Queue<TetriminoEnum> tempQueue = new Queue<TetriminoEnum>(bagQueue);
        while (tempQueue.Count > 0) {
            newBags.Enqueue(tempQueue.Dequeue());
        }

        return newBags; 
    }
}
