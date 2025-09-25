using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GridManager{
    public int width = 10, height = 20;
    public Vector2Int startingPosition;
    private TetriminoEnum[,] gridTypes;

    // ========================================================
    //                          START
    // ========================================================
    public GridManager(int width = 10, int height = 20) { 
        this.width = width;
        this.height = height;
        // ============== Define pieces starting position ==============
        startingPosition = new Vector2Int(
            Mathf.Max(0, Mathf.FloorToInt(width / 2) - 1),
            height - 4
        );

        // ============== Initialize grid ==============
        gridTypes = new TetriminoEnum[width, height];
    }

    // ================================================================================================================
    //                                              METHODS
    // ================================================================================================================
    public void resetGrid() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                gridTypes[x, y] = TetriminoEnum.X;
            }
        }
    }

    public bool areValidPositions(List<Vector2Int> positions) {
        //Efficient implementation without extra variales
        foreach (Vector2Int pos in positions) {
            if (!isValidPosition(pos)) {
                return false;
            }
        }
        return true;
    }
    public bool isValidPosition(Vector2Int position) {
        return (
            position != null &&
            position.x < width && position.x >= 0 &&
            position.y < height && position.y >= 0 &&
            gridTypes[position.x, position.y] == TetriminoEnum.X
        );
    }

    public int lockPiece(List<Vector2Int> positions, TetriminoEnum pieceType, ActionEnum lastAction) {
        foreach (Vector2Int pos in positions) {
            gridTypes[pos.x, pos.y] = pieceType;
        }


        return clearLines(lastAction);
    }

    public int clearLines(ActionEnum lastAction) {
        int y = 0, count = 0;
        bool full;

        // ================ CLEAR LINES ================ 
        while (y < height) {
            // check line clear
            full = true;
            for (int x = 0; x < width; x++) {
                if (gridTypes[x, y] == TetriminoEnum.X) {
                    full = false; break;
                }
            }

            // If clear sum points and move lines down
            if (full) {
                count++;
                for (int yy = y; yy < height - 1; yy++) {
                    for (int xx = 0; xx < width; xx++) {
                        gridTypes[xx, yy] = gridTypes[xx, yy + 1];
                    }
                }
                // last line 
                for (int xx = 0; xx < width; xx++) {
                    gridTypes[xx, height - 1] = TetriminoEnum.X;
                }
            } else { //if not full move to the next
                y++;
            }

            if (count == 4) {
                break; // wont be more lines to clear
            }
        }


        // ================ CHECK ALL CLEAR ================ 
        bool allClear = true; // IF FIRST LINE IS EMPTY -> ALL CLEAR
        for (int x = 0; x < width; x++) {
            if (gridTypes[x, 0] != TetriminoEnum.X) {
                allClear = false; break;
            }
        }
        // ================ COMPUTE SCORE ================ 
        return TetriminoSettings.computeScore(count, lastAction, allClear);
    }


}
