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

    public void movePieze(MoveEnum direction) {
        if (direction == MoveEnum.LEFT) {
            position += Vector2Int.left;
        }else if (direction == MoveEnum.UP) {
            position += Vector2Int.up;
        }else if (direction == MoveEnum.RIGHT) {
            position += Vector2Int.right;
        }else { // if(direction == DirectionEnum.DOWN){
            position += Vector2Int.down;
        }
    }

    public void rotatePiece(RorateEnum direction) {
        for(int i = 0; i < positionsList.Count; i++) {
            positionsList[i] = rotateCell(positionsList[i], direction);
        }
    }

    private Vector2Int rotateCell(Vector2Int oldPost, RorateEnum direction) {
        return oldPost;
    }
}
