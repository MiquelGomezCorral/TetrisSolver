using System.Collections.Generic;
using UnityEngine;

public class GridViewer : MonoBehaviour {
    [SerializeField] public Cell cellPrefab;
    [SerializeField] public PiecePlaceholder swapPlaceholderPrefab;
    private PiecePlaceholder swapPlaceholder;

    Vector2 sizeInUnits;
    public Cell[,] gridCells;
    public int width, height;

    // ========================================================
    //                          START
    // ========================================================
    void Start() {}
    public void Init() {
        width = TetriminoSettings.width; height = TetriminoSettings.height;

        // Validate prefabs
        if (cellPrefab == null) {
            Debug.LogError("GridViewer: cellPrefab is not assigned. Please assign in the inspector.");
            return;
        }
        if (swapPlaceholderPrefab == null) {
            Debug.LogError("GridViewer: swapPlaceholderPrefab is not assigned. Please assign in the inspector.");
            return;
        }

        // ============== Initialize grid ==============
        gridCells = new Cell[width, height];

        sizeInUnits = new Vector2(
            // guard against null texture
            (cellPrefab.texture != null && cellPrefab.texture.texture != null) ? cellPrefab.texture.texture.width / cellPrefab.texture.pixelsPerUnit : 1f,
            (cellPrefab.texture != null && cellPrefab.texture.texture != null) ? cellPrefab.texture.texture.height / cellPrefab.texture.pixelsPerUnit : 1f
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

    public void updateGridPositions(GridPos[] positions, TetriminoEnum pieceType) {
        if (positions == null) return;

        foreach (GridPos cell in positions) { 
            gridCells[cell.x, cell.y].changeType(pieceType);
        }
    }
    public void unRenderPiece(GridPos[] positions) {
        updateGridPositions(positions, TetriminoEnum.X);
    }


    public void updateSwapPiece(TetriminoEnum newType) {
        swapPlaceholder.changeType(newType);
    }
}
