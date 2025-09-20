using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{
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


    // Start is called before the first frame update
    void Start(){
        spawnNewPiece();
    }

    // Update is called once per frame
    void Update()
    {
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
        } else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)) {
            currentPiece.rotatePiece(RorateEnum.R180);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            spawnNewPiece();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            swapCurrentPiece();
        }
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public void spawnNewPiece() {
        if (currentPiece == null) {
            currentPieceType = TetriminoSettings.getRandomPiece();
            Instantiate(tetriminoPrefab);
        } else {
            currentPiece.lockPeace();
            currentPieceType = TetriminoSettings.getRandomPiece();
            currentPiece.resetPeace();
        }
    }

    public void swapCurrentPiece() {
        if (currentPieceType == TetriminoEnum.X && swapPieceType == TetriminoEnum.X) 
            return;


        // If no piece get a new one
        if (swapPieceType == TetriminoEnum.X) {
            swapPieceType = TetriminoSettings.getRandomPiece();
        }

        // Swap types
        TetriminoEnum auxPieceType = currentPieceType;
        currentPieceType = swapPieceType;
        swapPieceType = auxPieceType;

        // swap with new current type,will take it from this class
        currentPiece.swapPiece();
    }


    // ========================================================
    //                          SCORE
    // ========================================================
    public int addPoint(int points) {
        Score += points;
        return Score;
    }
    public int getPoint() {
        return Score;
    }

}
