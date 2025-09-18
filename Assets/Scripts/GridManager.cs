using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

public class GridManager : MonoBehaviour {
    private GameManager gameM; // Do to look for it every time

    [SerializeField] public Cell cellPrefab;
    [SerializeField] public int width = 10, height = 20;
    [SerializeField] public Vector2Int startingPosition;

    private List<Vector2Int> lastPositions = new List<Vector2Int> { };


    Vector2 sizeInUnits;
    public TetriminoEnum[,] gridTypes;
    public Cell[,] gridCell;

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
        gridCell = new Cell[width, height];

        sizeInUnits = new Vector2(
            cellPrefab.texture.texture.width / cellPrefab.texture.pixelsPerUnit,
            cellPrefab.texture.texture.height / cellPrefab.texture.pixelsPerUnit
        );
        float offsetX = sizeInUnits.x * width / 2; // half the width of all the cells
        float offsetY = sizeInUnits.y * height / 2; // half the height of all the cells

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                gridCell[x, y] = Instantiate(
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
        foreach (Vector2Int cell in lastPositions) {
            //gridTypes[cell.x, cell.y] = TetriminoEnum.X; // No need to update new 
            gridCell[cell.x, cell.y].changeType(TetriminoEnum.X);
        }
        lastPositions = currPiece.positionsList
            .Select(cell => cell + currPiece.position)
            .ToList();

        // Add new cells
        foreach (Vector2Int cell in currPiece.positionsList) {
            Vector2Int absPos = cell + currPiece.position;
            //gridTypes[absPos.x, absPos.y] = currPiece.piezeType; // No need to update new 
            gridCell[absPos.x, absPos.y].changeType(currPiece.piezeType);
        }
    }


    // ========================================================
    //                          METHODS
    // ========================================================
    public void lockPiece(List<Vector2Int> positions, TetriminoEnum pieceType) {
        foreach (Vector2Int pos in positions) {
            gridTypes[pos.x, pos.y] = pieceType;
            gridCell[pos.x, pos.y].changeType(pieceType);
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
    public bool isValidPosition( Vector2Int position ) {
        return (
            position != null &&
            position.x < width && position.x >= 0 && 
            position.y < height && position.y >= 0 &&
            gridTypes[position.x, position.y] == TetriminoEnum.X
        );
    }
}
