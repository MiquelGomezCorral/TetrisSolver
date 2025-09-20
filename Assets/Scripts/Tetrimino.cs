using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Tetrimino : MonoBehaviour {
    private GameManager gameM; // Do to look for it every time
    private GridManager gridM; // Do to look for it every time

    public TetriminoEnum pieceType;
    public DirectionEnum pieceOrientation;
    public List<Vector2Int> positionsList;
    public Vector2Int position;


    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        // GAME MANAGER 
        gameM = FindFirstObjectByType<GameManager>();
        gameM.CurrentPiece = this;

        // GRID MANAGER 
        gridM = FindFirstObjectByType<GridManager>();

        resetPeace();
    }


    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {}


    // ========================================================
    //                      Reset peace
    // ========================================================
    public void resetPeace() {
        pieceType = gameM.CurrentPieceType;

        positionsList = TetriminoSettings.getTetriminoPositions(pieceType);
        position = gridM.startingPosition;
        pieceOrientation = DirectionEnum.UP; // by default 0 looking up

        gridM.updateGrid();
    }

    public void lockPeace() {
        List<Vector2Int> absPositions = getAbsPositions();
        gridM.unRenderPiece(absPositions);

        // Move the piece to the bottom
        while (movePieze(DirectionEnum.DOWN, false)) { }

        // Get the absolute positions and fix those cells
        absPositions = getAbsPositions();
        gridM.lockPiece(absPositions, pieceType);
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public bool movePieze(DirectionEnum direction, bool uptateGrid = true) {
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
            if (!gridM.isValidPosition(newPos + pos)) 
                return false;
        }
        position = newPos;

        if (uptateGrid)
            gridM.updateGrid();

        return true;
    }


    public List<Vector2Int> getAbsPositions() {
        List<Vector2Int> absPositions = new List<Vector2Int>(positionsList);
        for (int i = 0; i < positionsList.Count; i++)
            absPositions[i] += position;

        return absPositions;
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
        List<List<Vector2Int>> offSetTable = TetriminoSettings.getTetriminoOffsets(pieceType);
        List<Vector2Int> rotationMatrix = TetriminoSettings.getRotationMatrix(direction);

        bool rotationSuccess = false;
        List<Vector2Int> newPositions = this.positionsList;
        Vector2Int newOffSet = Vector2Int.zero;
        foreach (List<Vector2Int> offSetTest in offSetTable) {
            rotationSuccess = true;
            newPositions = new List<Vector2Int>();

            // compute offset: current - new
            Vector2Int offSet = offSetTest[(int)pieceOrientation] - offSetTest[(int)newDirection];
            foreach (Vector2Int pos in positionsList) {
                // compute new position 
                Vector2Int rotatedPos = rotateVector(pos, rotationMatrix) + offSet;
                Vector2Int absPos = rotatedPos + position;

                if (!gridM.isValidPosition(absPos)) {
                    rotationSuccess = false; break;
                }
                    
                newPositions.Add(rotatedPos);
            }
            if (rotationSuccess) {
                newOffSet = offSet;
                break;
            }
        }

        if (rotationSuccess) {
            normalizePositionList(newPositions, newOffSet);
            pieceOrientation = newDirection;
        }
        return rotationSuccess;

    }

    private Vector2Int rotateVector(Vector2Int pos, List<Vector2Int> rotationMatrix) {
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

        position += offSet;
        positionsList = newPositions;
    }

    
}
