using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class GridManager : MonoBehaviour {
    [SerializeField] public int width = 10, height = 20;
    [SerializeField] public Cell cellPrefab;
    Vector2 sizeInUnits;
    public TetriminoEnum[,] gridTypes;
    public Cell[,] gridCell;

    // Start is called before the first frame update
    void Start() {
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

    // Update is called once per frame
    void Update() {
        GameManager gm = FindObjectOfType<GameManager>();
        Tetrimino currPirce = gm.CurrentPiece;
        foreach (Vector2Int cell in currPirce.position) {
            gridTypes[cell.x, cell.y] = currPirce.piezeType;
            gridCell[cell.x, cell.y].changeType(currPirce.piezeType);
        }

    }
}
