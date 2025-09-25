using System.Collections.Generic;
using UnityEngine;

public class GridViewer : MonoBehaviour {
    [SerializeField] public Cell cellPrefab;
    [SerializeField] public PiecePlaceholder swapPlaceholderPrefab;
    public PiecePlaceholder swapPlaceholder;

    Vector2 sizeInUnits;
    public Cell[,] gridCells;
    public int width, height;

    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        width = TetriminoSettings.width; height = TetriminoSettings.height;

        // ============== Initialize grid ==============
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
        // ============== Swap Placeholder ==============
        swapPlaceholder = Instantiate(
            swapPlaceholderPrefab,
            new Vector3(
                sizeInUnits.x * width + sizeInUnits.x * 0.5f - offsetX + 1f, 
                sizeInUnits.y * height / 2f - 1f,                            
                0f
            ),
            Quaternion.identity
        );
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update() {}

    // ========================================================
    //                          METHOD
    // ========================================================
    public void updateGrid(TetriminoEnum[,] gridTypes) {
        for(int x = 0; x < gridTypes.GetLength(0); x++) {
            for(int y = 0;y < gridTypes.GetLength(1); y++) {
                gridCells[x, y].changeType(gridTypes[x, y]);
            }
        }
    }

    public void resetGrid() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                gridCells[x, y].changeType(TetriminoEnum.X);
            }
        }
    }

    public void updateGridPositions(List<Vector2Int> positions, TetriminoEnum pieceType) {
        if (positions == null) return;

        foreach (Vector2Int cell in positions) { 
            gridCells[cell.x, cell.y].changeType(pieceType);
        }
    }
    public void unRenderPiece(List<Vector2Int> positions) {
        updateGridPositions(positions, TetriminoEnum.X);
    }

}
