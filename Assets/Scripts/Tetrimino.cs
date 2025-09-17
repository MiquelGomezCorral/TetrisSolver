using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Tetrimino : MonoBehaviour {
    private GameManager gameM; // Do to look for it every time
    private GridManager gridM; // Do to look for it every time

    public TetriminoEnum piezeType;
    public DirectionEnum pieceOrientation;
    public List<Vector2Int> positionsList;
    public Vector2Int position;


    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        // ============= GAME MANAGER =============
        gameM = FindObjectOfType<GameManager>();
        piezeType = gameM.CurrentPieceType;
        positionsList = TetriminoSettings.getTetriminoPositions(piezeType);

        gameM.CurrentPiece = this;
        // ============= GRID MANAGER =============
        gridM = FindObjectOfType<GridManager>();
        position = gridM.startingPosition;
        pieceOrientation = DirectionEnum.LEFT; // by default 0, nothing special

        gridM.updateGrid();
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {}


    // ========================================================
    //                          METHODS
    // ========================================================
    public void movePieze(DirectionEnum direction) {
        Vector2Int delta = direction switch {
            DirectionEnum.LEFT => Vector2Int.left,
            DirectionEnum.RIGHT => Vector2Int.right,
            DirectionEnum.UP => Vector2Int.up,
            DirectionEnum.DOWN => Vector2Int.down,
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



    public bool rotatePiece(RorateEnum direction) {
        if (direction == RorateEnum.R180) {
            // Try 180° ACLOCK first
            if (rotate(RorateEnum.ACLOCK)) {
                if (rotate(RorateEnum.ACLOCK))
                    return true;
                else //undo last rotation
                    rotate(RorateEnum.CLOCK);
            }
            // If failed, try 180° CLOCK
            if (rotate(RorateEnum.CLOCK)) {
                if (rotate(RorateEnum.CLOCK))
                    return true;
                else //undo last rotation
                    rotate(RorateEnum.ACLOCK);
            }
            return false; // both failed
        } else {// normal rotation
            return rotate(direction);
        }
    }

    private bool rotate(RorateEnum direction) {
        DirectionEnum newDirection = TetriminoSettings.getNewDirection(pieceOrientation, direction);
        List<Vector2Int>offSets = TetriminoSettings.getTetriminoOffsets(piezeType, newDirection);
        List<Vector2Int> rotationMatrix = TetriminoSettings.getRotationMatrix(direction);

        bool rotationSuccess = false;
        List<Vector2Int> newPositions = positionsList; 
        foreach (Vector2Int offSet in offSets) {
            rotationSuccess = true;
            newPositions = new List<Vector2Int>();

            foreach (Vector2Int pos in positionsList) {
                Vector2Int newPos = rotate(pos, rotationMatrix) + offSet;
                if (gridM.isValidPosition(newPos)) {
                    newPositions.Add(newPos);
                } else {
                    rotationSuccess = false;
                    break;
                }
            }
            if (rotationSuccess) 
                break;
        }

        if (rotationSuccess) {
            positionsList = newPositions;
            pieceOrientation = newDirection;
        }
        return rotationSuccess;

    }

    private Vector2Int rotate(Vector2Int pos, List<Vector2Int> rotationMatrix) {
        return new Vector2Int(
            rotationMatrix[0].x * pos.x + rotationMatrix[0].y * pos.y,
            rotationMatrix[1].x * pos.x + rotationMatrix[1].y * pos.y
        );
    }

}
