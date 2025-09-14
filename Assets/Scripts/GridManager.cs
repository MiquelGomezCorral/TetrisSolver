using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class GridManager : MonoBehaviour {
    [SerializeField] public int width = 10, height = 20;
    [SerializeField] public Cell cellPrefab;
    Vector2 sizeInUnits;
    public TetriminoEnum[,] gridInt;
    public Cell[,] gridCell;

    // Start is called before the first frame update
    void Start() {
        gridInt = new TetriminoEnum[width, height];
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
        int x = Random.Range(0,width);
        int y = Random.Range(0,height);
        TetriminoEnum newType = TetriminoClass.getRandomPiece(true);

        gridInt[x, y] = newType;
        gridCell[x, y].changeType( newType );
    }
}
