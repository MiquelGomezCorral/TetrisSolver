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
        pieceOrientation = DirectionEnum.UP; // by default 0, nothing special

        gridM.updateGrid();
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {}


    // ========================================================
    //                          ONDESTRO
    // ========================================================
    void OnDestroy() {
        // Get the absolute positions and fix those cells
        List<Vector2Int> absPositions = new List<Vector2Int>(positionsList);
        for (int i = 0; i < positionsList.Count; i++)
            absPositions[i] += position;
        gridM.lockPiece(absPositions, piezeType);

        Debug.Log($"{name} destroyed!");
    }
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
        bool rotationSuccess = false;
        if (direction == RorateEnum.R180) {
            // Try 180° ACLOCK first
            if (rotate(RorateEnum.ACLOCK)) {
                if (rotate(RorateEnum.ACLOCK))
                    rotationSuccess = true;
                else //undo last rotation
                    rotate(RorateEnum.CLOCK);
            }
            // If failed, try 180° CLOCK
            if (!rotationSuccess && rotate(RorateEnum.CLOCK)) {
                if (rotate(RorateEnum.CLOCK))
                    rotationSuccess = true;
                else //undo last rotation
                    rotate(RorateEnum.ACLOCK);
            }
        } else {// normal rotation
            rotationSuccess = rotate(direction);
        }

        if (rotationSuccess)
            gridM.updateGrid();
        return rotationSuccess;
    }

    private bool rotate(RorateEnum direction) {
        DirectionEnum newDirection = TetriminoSettings.getNewDirection(pieceOrientation, direction);
        List<Vector2Int>offSets = TetriminoSettings.getTetriminoOffsets(piezeType, newDirection);
        List<Vector2Int> rotationMatrix = TetriminoSettings.getRotationMatrix(direction);

        Debug.Log("rotate 1");
        bool rotationSuccess = false;
        List<Vector2Int> newPositions = this.positionsList;
        Vector2Int newOffSet = Vector2Int.zero;
        Debug.Log(offSets.Count);
        foreach (Vector2Int offSet in offSets) {
            Debug.Log("rotate 1.1");
            rotationSuccess = true;
            newPositions = new List<Vector2Int>();

            foreach (Vector2Int pos in positionsList) {
                Debug.Log("rotate 1.1.1: " + rotationSuccess);
                Vector2Int rotatedPos = rotate(pos, rotationMatrix) + offSet;
                Vector2Int absPos = rotatedPos + position;
                if (gridM.isValidPosition(absPos)) {
                    newPositions.Add(rotatedPos);
                    Debug.Log("rotate 1.1.2: " + rotationSuccess);
                } else {
                    rotationSuccess = false;
                    Debug.Log("rotate 1.1.3: " + rotationSuccess);
                    break;
                }
            }
            if (rotationSuccess)
                newOffSet = offSet;
                break;
        }

        Debug.Log("rotate 2");

        if (rotationSuccess) {
            Debug.Log("rotate 2.1");
            normalizePositionList(newPositions, newOffSet);
            this.pieceOrientation = newDirection;
        }
        Debug.Log("rotate 3");
        return rotationSuccess;

    }

    private Vector2Int rotate(Vector2Int pos, List<Vector2Int> rotationMatrix) {
        return new Vector2Int(
            rotationMatrix[0].x * pos.x + rotationMatrix[0].y * pos.y,
            rotationMatrix[1].x * pos.x + rotationMatrix[1].y * pos.y
        );
    }

    private void normalizePositionList(List<Vector2Int> positions, Vector2Int offSet) {
        List<Vector2Int> newPositions = new List<Vector2Int>();
        foreach(Vector2Int pos in positions) {
            newPositions.Add(pos - offSet);
        }

        position+=offSet;
        positionsList = newPositions;
    }

}
