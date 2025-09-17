using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetrimino : MonoBehaviour {
    public TetriminoEnum piezeType;
    public List<Vector2Int> position;


    void Start() {
        GameManager gm = FindObjectOfType<GameManager>();
        piezeType = gm.CurrentPieceType;
        position = TetriminoSettings.getTetriminoPositions(piezeType);

        gm.CurrentPiece = this;
    }

    void Update() {
    
    
    }
}
