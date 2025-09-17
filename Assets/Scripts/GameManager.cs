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
        currentPieceType = TetriminoSettings.getRandomPiece();
        Instantiate(tetriminoPrefab);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) {
            currentPiece.movePieze(DirectionEnum.LEFT);
        } if (Input.GetKeyDown(KeyCode.W)) {
            currentPiece.movePieze(DirectionEnum.UP);
        } if (Input.GetKeyDown(KeyCode.D)) {
            currentPiece.movePieze(DirectionEnum.RIGHT);
        } if (Input.GetKeyDown(KeyCode.S)) {
            currentPiece.movePieze(DirectionEnum.DOWN);
        }

    }


}
