using System.Collections.Generic;
using UnityEngine;

public class Tetrimino {
    private GridManager gridM;

    public TetriminoEnum pieceType;
    public DirectionEnum pieceOrientation;
    public GridPos[] positionsList;
    public GridPos position;
    public ActionEnum lastAction = ActionEnum.MOVE;


    // ========================================================
    //                          CONSTRUCTOR
    // ========================================================
    public Tetrimino(GridManager gridM, TetriminoEnum pieceType) {
        this.gridM = gridM;
        resetPeace(pieceType);
    }


    // ========================================================
    //                      Reset peace
    // ========================================================
    public void resetPeace(TetriminoEnum pieceType) {
        this.pieceType = pieceType;

        positionsList = TetriminoSettings.getTetriminoPositions(pieceType);
        position = gridM.startingPosition;
        pieceOrientation = DirectionEnum.UP; // by default 0 looking up
    }

    public void lockPeace() {
        GridPos[] absPositions = getAbsPositions();
        movePieceBootom();

        // Get the absolute positions and fix those cells
        absPositions = getAbsPositions();
        gridM.lockPiece(absPositions, pieceType, lastAction);
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public GridPos[] getAbsPositions() {
        GridPos[] absPositions = new GridPos[positionsList.Length];
        for (int i = 0; i < positionsList.Length; i++)
            absPositions[i] = new GridPos(
                positionsList[i].x+position.x,
                positionsList[i].y+position.y
            );

        return absPositions;
    }

    public bool movePieze(DirectionEnum direction) {
        GridPos delta = direction switch {
            DirectionEnum.LEFT => new GridPos(-1, 0),
            DirectionEnum.RIGHT => new GridPos(1, 0),
            DirectionEnum.UP => new GridPos(0, 1),
            DirectionEnum.DOWN => new GridPos(0, -1),
            _ => new GridPos(0, 0)
        };

        GridPos newPos = position + delta;

        // Check all cells of the piece
        foreach (GridPos pos in positionsList) {
            if (!gridM.isValidPosition(newPos + pos))
                return false;
        }

        position = newPos;
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

        return rotationSuccess;
    }

    private bool rotate(RorateEnum direction) {
        DirectionEnum newDirection = TetriminoSettings.getNewDirection(pieceOrientation, direction);
        GridPos[][] offSetTable = TetriminoSettings.getTetriminoOffsets(pieceType);
        GridPos[] rotationMatrix = TetriminoSettings.getRotationMatrix(direction);

        bool rotationSuccess = false;
        GridPos[] newPositions = null;
        GridPos newOffSet = new GridPos(0, 0);

        foreach (GridPos[] offSetTest in offSetTable) {
            rotationSuccess = true;
            newPositions = new GridPos[positionsList.Length];

            // compute offset: current - new
            GridPos offSet = offSetTest[(int)pieceOrientation] - offSetTest[(int)newDirection];

            for (int i = 0; i < positionsList.Length; i++) {
                GridPos rotatedPos = rotateVector(positionsList[i], rotationMatrix) + offSet;
                GridPos absPos = rotatedPos + position;

                if (!gridM.isValidPosition(absPos)) {
                    rotationSuccess = false;
                    break;
                }

                newPositions[i] = rotatedPos;
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

    private GridPos rotateVector(GridPos pos, GridPos[] rotationMatrix) {
        return new GridPos(
            rotationMatrix[0].x * pos.x + rotationMatrix[0].y * pos.y,
            rotationMatrix[1].x * pos.x + rotationMatrix[1].y * pos.y
        );
    }

    private void normalizePositionList(GridPos[] newPositions, GridPos offSet) {
        for (int i = 0; i < newPositions.Length; i++) {
            newPositions[i] -= offSet;
        }

        position += offSet;
        positionsList = newPositions;
    }

    private void checkTSpin() {
        if (pieceType != TetriminoEnum.T) return;

        bool A = !gridM.isValidPosition(position + new GridPos(-1, 1));  // up-left
        bool B = !gridM.isValidPosition(position + new GridPos(1, 1));   // up-right
        bool C = !gridM.isValidPosition(position + new GridPos(-1, -1)); // down-left
        bool D = !gridM.isValidPosition(position + new GridPos(1, -1));  // down-right

        // Rotate adjacency mask based on orientation
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
