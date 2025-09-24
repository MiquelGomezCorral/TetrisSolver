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
    public ActionEnum lastAction = ActionEnum.MOVE;


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

        movePieceBootom();

        // Get the absolute positions and fix those cells
        absPositions = getAbsPositions();
        gridM.lockPiece(absPositions, pieceType, lastAction);
    }


    // will take the new type from the Game manager
    public void swapPiece() {
        // Unrender piece
        gridM.unRenderPiece(getAbsPositions());


        // Change placeholder type before reseting.
        gridM.swapPlaceholder.changeType(pieceType);

        // Reset piece
        resetPeace();

    }
    // ========================================================
    //                          METHODS
    // ========================================================
    public List<Vector2Int> getAbsPositions() {
        List<Vector2Int> absPositions = new List<Vector2Int>(positionsList);
        for (int i = 0; i < positionsList.Count; i++)
            absPositions[i] += position;

        return absPositions;
    }

    public bool movePieze(DirectionEnum direction) {
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

        // Update grid (may be optimized by just calling it at the end)
        gridM.updateGrid();
        
        // last action was a movement
        lastAction = ActionEnum.MOVE;
        return true;
    }

    public bool movePieceBootom() {
        // Move the piece to the bottom
        bool movedAtLeastOnce = false;
        while (movePieze(DirectionEnum.DOWN)) { 
            movedAtLeastOnce = true;
        }
        return movedAtLeastOnce;
    }

    public bool rotatePiece(RorateEnum direction) {
        if (direction == RorateEnum.X)  // Do nothing
            return true;
        

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

            checkTSpin();
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


    private void checkTSpin() {
        if (pieceType != TetriminoEnum.T) return;

        bool A = !gridM.isValidPosition(position + new Vector2Int(-1, 1)); // occupiedUpLeft
        bool B = !gridM.isValidPosition(position + new Vector2Int(1, 1)); // occupiedUpRight
        bool C = !gridM.isValidPosition(position + new Vector2Int(-1, -1)); // occupiedDownLeft
        bool D = !gridM.isValidPosition(position + new Vector2Int(1, -1)); // occupiedDownRight

        // Rotate for the check as many times as the orientation says UP 0, Right 1, Down 2, Left 3
        for (int i = 0; i < (int)pieceOrientation; i++) {
            bool auxA = A;
            A = C;
            C = D;
            D = B;
            B = auxA;
        }

        if (A && B && (C || D))
            lastAction = ActionEnum.T_SPIN;
        else if (C && D && (A || B))
            lastAction = ActionEnum.MINI_T_SPIN;

    }


}
