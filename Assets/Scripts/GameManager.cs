using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject tetriminoPrefab;
    [SerializeField] private Tetrimino currentPiece;
    public Tetrimino CurrentPiece {
        get => currentPiece;
        set => currentPiece = value;
    }
    [SerializeField] private TetriminoEnum currentPieceType;
    public TetriminoEnum CurrentPieceType {
        get => currentPieceType;            
        set => currentPieceType = value;    
    }



    // Start is called before the first frame update
    void Start()
    {
        spawnNewPiece();
    }

    // Update is called once per frame
    void Update()
    {
        // Maybe more than one movement
        if (Input.GetKeyDown(KeyCode.A)) {
            Debug.Log("LEFT");
            currentPiece.movePieze(DirectionEnum.LEFT);
        } if (Input.GetKeyDown(KeyCode.W)) {
            Debug.Log("UP");
            currentPiece.movePieze(DirectionEnum.UP);
        } if (Input.GetKeyDown(KeyCode.D)) {
            Debug.Log("RIGHT");
            currentPiece.movePieze(DirectionEnum.RIGHT);
        } if (Input.GetKeyDown(KeyCode.S)) {
            Debug.Log("DOWN");
            currentPiece.movePieze(DirectionEnum.DOWN);
        }

        // Only one rotation
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            Debug.Log("ACLOCK");
            currentPiece.rotatePiece(RorateEnum.ACLOCK);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            Debug.Log("CLOCK");
            currentPiece.rotatePiece(RorateEnum.CLOCK);
        } else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)) {
            Debug.Log("R180");
            currentPiece.rotatePiece(RorateEnum.R180);
        }


        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("LOCK");
            spawnNewPiece();
        }
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public void spawnNewPiece() {
        if(currentPiece != null) {
            Destroy(currentPiece.gameObject);
        }
        currentPieceType = TetriminoSettings.getRandomPiece();
        Instantiate(tetriminoPrefab);
    }
}
