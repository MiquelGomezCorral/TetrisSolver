using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetrimino : MonoBehaviour {
    private GameManager gameM; // Do to look for it every time
    private GridManager gridM; // Do to look for it every time

    public TetriminoEnum piezeType;
    public List<Vector2Int> positionsList;
    public Vector2Int position;



    void Start() {
        // ============= GAME MANAGER =============
        gameM = FindObjectOfType<GameManager>();
        piezeType = gameM.CurrentPieceType;
        positionsList = TetriminoSettings.getTetriminoPositions(piezeType);

        gameM.CurrentPiece = this;
        // ============= GRID MANAGER =============
        gridM = FindObjectOfType<GridManager>();
        position = gridM.startingPosition;

        gridM.updateGrid();
    }

    void Update() {
    
    
    }

    public void movePieze(MoveEnum direction) {
        Vector2Int delta = direction switch {
            MoveEnum.LEFT => Vector2Int.left,
            MoveEnum.RIGHT => Vector2Int.right,
            MoveEnum.UP => Vector2Int.up,
            MoveEnum.DOWN => Vector2Int.down,
            _ => Vector2Int.zero
        };

        Vector2Int newPos = position + delta;


        // Efficiently cheking all the pieces
        foreach(Vector2Int pos in positionsList) {
            if (!gridM.isValidPosition(newPos + pos)) {
                return;
            }
        }
        position = newPos;
        gridM.updateGrid();
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
