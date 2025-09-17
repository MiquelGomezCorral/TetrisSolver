using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] private TetriminoEnum currentPieceType;  
    public TetriminoEnum CurrentPieceType {
        get => currentPieceType;            
        set => currentPieceType = value;    
    }

    [SerializeField] private Tetrimino currentPiece;
    public Tetrimino CurrentPiece {
        get => currentPiece;
        set => currentPiece = value;
    }


    // Start is called before the first frame update
    void Start()
    {
        currentPieceType = TetriminoSettings.getRandomPiece();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
