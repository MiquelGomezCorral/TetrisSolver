using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour {
    private GameManager gameM; // Do to look for it every time
    [SerializeField] private GameObject scorePrefab;
    [SerializeField] private Transform canvasTransform; // assign Canvas here

    [SerializeField] public Cell cellPrefab;
    [SerializeField] public PiecePlaceholder swapPlaceholderPrefab;
    [SerializeField] public int width = 10, height = 20;
    [SerializeField] public Vector2Int startingPosition;

    private List<Vector2Int> lastPositions = new List<Vector2Int> { };


    Vector2 sizeInUnits;
    public TetriminoEnum[,] gridTypes;
    public Cell[,] gridCells;
    public PiecePlaceholder swapPlaceholder;

    // ========================================================
    //                          START
    // ========================================================
    void Start() {
        // ============== get game manager ==============
        gameM = FindFirstObjectByType<GameManager>();

        // ============== Define pieces starting position ==============
        startingPosition = new Vector2Int(
            Mathf.Max(0, Mathf.FloorToInt(width / 2) - 1),
            height - 4
        );

        // ============== Initialize grid ==============
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
        // ============== Swap Placeholder ==============
        swapPlaceholder = Instantiate(
            swapPlaceholderPrefab,
            new Vector3(
                sizeInUnits.x * width + sizeInUnits.x * 0.5f - offsetX + 1f, // right of grid, small offset
                sizeInUnits.y * height / 2f - 1f,                  // vertically centered
                0f
            ),
            Quaternion.identity
        );


        // ============== Initialize text ==============
        // --- instantiate & parent (keep what you already do) ---
        GameObject go = Instantiate(scorePrefab);
        go.transform.SetParent(canvasTransform, false);

        // get rect transforms
        RectTransform canvasRect = canvasTransform as RectTransform;
        RectTransform rt = go.GetComponent<RectTransform>();

        // 1) world position of the top-center of the grid (center of top cell)
        Vector3 worldTopCenter = new Vector3(
            sizeInUnits.x * (width - 1f) / 2f - offsetX,        // x = center column
            sizeInUnits.y * (height - 1f) - offsetY + sizeInUnits.y * 0.5f, // y = top cell center + half cell
            0f
        );

        // 2) choose the camera used by the canvas (null for ScreenSpace-Overlay)
        Canvas canvasComp = canvasTransform.GetComponent<Canvas>();
        Camera uiCamera = (canvasComp != null && canvasComp.renderMode == RenderMode.ScreenSpaceCamera)
                          ? canvasComp.worldCamera
                          : null;

        // 3) convert world -> screen -> canvas local
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldTopCenter);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out localPoint);

        // 4) place the UI element. use anchoredPosition so it's correct regardless of anchors/pivot
        rt.anchoredPosition = localPoint + new Vector2(0f, 4f); // small extra pixel margin (tweak 4f)
        gameM.scoreText = go.GetComponent<TextMeshProUGUI>();
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
    public void lockPiece(List<Vector2Int> positions, TetriminoEnum pieceType, ActionEnum lastAction) {
        foreach (Vector2Int pos in positions) {
            gridTypes[pos.x, pos.y] = pieceType;
            gridCells[pos.x, pos.y].changeType(pieceType);
        }

        updateGridPositions(positions, pieceType);

        int score = clearLines(lastAction);
        gameM.addPoint(score);
        lastPositions = new List<Vector2Int>();
    }

    public int clearLines(ActionEnum lastAction) {
        int y = 0, count = 0;
        bool full;

        // ================ CLEAR LINES ================ 
        while (y < height) {
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

            if(count == 4) {
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
