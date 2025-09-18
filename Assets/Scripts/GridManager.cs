using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour {
    private GameManager gameM; // Do to look for it every time

    [SerializeField] public Cell cellPrefab;
    [SerializeField] public int width = 10, height = 20;
    [SerializeField] public Vector2Int startingPosition;

    private List<Vector2Int> lastPositions = new List<Vector2Int> { };


    Vector2 sizeInUnits;
    public TetriminoEnum[,] gridTypes;
    public Cell[,] gridCells;

    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        gameM = FindObjectOfType<GameManager>();

        startingPosition = new Vector2Int(
            Mathf.Max(0, Mathf.FloorToInt(width / 2) - 1),
            height - 4
        );

        gridTypes = new TetriminoEnum[width, height];
        gridCells = new Cell[width, height];

        sizeInUnits = new Vector2(
            cellPrefab.texture.texture.width / cellPrefab.texture.pixelsPerUnit,
            cellPrefab.texture.texture.height / cellPrefab.texture.pixelsPerUnit
        );
        float offsetX = sizeInUnits.x * width / 2; // half the width of all the cells
        float offsetY = sizeInUnits.y * height / 2; // half the height of all the cells

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                gridCells[x, y] = Instantiate(
                    cellPrefab, 
                    new Vector3(
                        sizeInUnits.x * x - offsetX, 
                        sizeInUnits.y * y - offsetY,
                        0
                     ), 
                    Quaternion.identity
                );
            }
        }
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {}
    // ========================================================
    //   AVOID UPDATING IT EVERY FRAME AND JUST WHEN NEEDED
    // ========================================================
    public void updateGrid() {
        Tetrimino currPiece = gameM.CurrentPiece;

        // Clear previous cells
        updateGridPositions(lastPositions, TetriminoEnum.X);

        // Add new cells
        lastPositions = currPiece.positionsList
            .Select(cell => cell + currPiece.position)
            .ToList();
        updateGridPositions(currPiece.getAbsPositions(), currPiece.pieceType);
    }

    public void updateGridPositions(List<Vector2Int> positions, TetriminoEnum pieceType) {
        foreach (Vector2Int cell in positions) { 
            gridCells[cell.x, cell.y].changeType(pieceType);
        }
    }
    public void unRenderPiece(List<Vector2Int> positions) {
        updateGridPositions(positions, TetriminoEnum.X);
    }

    // ================================================================================================================
    //                                              METHODS
    // ================================================================================================================
    public void lockPiece(List<Vector2Int> positions, TetriminoEnum pieceType) {
        Debug.Log("Piece type: " +  pieceType);
        foreach (Vector2Int pos in positions) {
            gridTypes[pos.x, pos.y] = pieceType;
            gridCells[pos.x, pos.y].changeType(pieceType);
        }

        updateGridPositions(positions, pieceType);

        clearLines();
        lastPositions = new List<Vector2Int>();
    }

    public int clearLines() {
        int y = 0, count = 0;
        bool full;
        while(y < height) {
            // check line clear
            full = true;
            for (int x = 0; x < width; x++) {
                if (gridTypes[x,y] == TetriminoEnum.X) {
                    full = false; break;
                }
            }

            // If clear sum points and move lines down
            if (full) {
                count++;
                for(int yy = y; yy < height-1; yy++) {
                    for(int xx = 0; xx < width; xx++) {
                        gridTypes[xx, yy] = gridTypes[xx, yy+1];
                        gridCells[xx, yy].changeType(gridCells[xx, yy+1].pieceType);
                    }
                }
                // last line 
                for (int xx = 0; xx < width; xx++) {
                    gridTypes[xx, height-1] = TetriminoEnum.X;
                    gridCells[xx, height-1].changeType(TetriminoEnum.X);
                }
            } else { //if not full move to the next
                y++;
            }
        }
        return count;
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
    public bool isValidPosition( Vector2Int position ) {
        return (
            position != null &&
            position.x < width && position.x >= 0 && 
            position.y < height && position.y >= 0 &&
            gridTypes[position.x, position.y] == TetriminoEnum.X
        );
    }
}
