using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetrimino : MonoBehaviour {
    public TetriminoEnum piezeType;
    public List<Vector2Int> positionsList;
    public Vector2Int position;


    void Start() {
        // ============= GAME MANAGER =============
        GameManager gameM = FindObjectOfType<GameManager>();
        piezeType = gameM.CurrentPieceType;
        positionsList = TetriminoSettings.getTetriminoPositions(piezeType);

        gameM.CurrentPiece = this;
        // ============= GRID MANAGER =============
        GridManager gridM = FindObjectOfType<GridManager>();
        position = gridM.startingPosition;
    }

    void Update() {
    
    
    }

    public void movePieze(DirectionEnum direction) {
        if (direction == DirectionEnum.LEFT) {
            position += Vector2Int.left;
        }else if (direction == DirectionEnum.UP) {
            position += Vector2Int.up;
        }else if (direction == DirectionEnum.RIGHT) {
            position += Vector2Int.right;
        }else { // if(direction == DirectionEnum.DOWN){
            position += Vector2Int.down;
        }
    }
}
