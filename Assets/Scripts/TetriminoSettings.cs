using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum TetriminoEnum { // X is empty
    X, I, O, T, S, Z, J, L
}
public class TetriminoSettings : MonoBehaviour{
    public static TetriminoSettings Instance;

    [Header("Tetromino Sprites")]
    public Sprite textureX, textureI, textureO, textureT, textureS, textureZ, textureJ, textureL;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public static Sprite getTetriminoTexture(TetriminoEnum pieceType) {
        switch (pieceType) {
            case TetriminoEnum.I: return Instance.textureI;
            case TetriminoEnum.O: return Instance.textureO;
            case TetriminoEnum.T: return Instance.textureT;
            case TetriminoEnum.S: return Instance.textureS;
            case TetriminoEnum.Z: return Instance.textureZ;
            case TetriminoEnum.J: return Instance.textureJ;
            case TetriminoEnum.L: return Instance.textureL;
            case TetriminoEnum.X:
            default: return Instance.textureX;
        }
    }

    public static List<Vector2Int> getTetriminoPositions(TetriminoEnum pieceType) {
        switch (pieceType) {
            case TetriminoEnum.I:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(-1,0),
                    new Vector2Int(1,0),
                    new Vector2Int(2,0)
                };
            case TetriminoEnum.O:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(1,0),
                    new Vector2Int(0,1),
                    new Vector2Int(1,1)
                };
            case TetriminoEnum.T:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(-1,0),
                    new Vector2Int(1,0),
                    new Vector2Int(0,1)
                };
            case TetriminoEnum.S:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(-1,0),
                    new Vector2Int(0,1),
                    new Vector2Int(1,1)
                };
            case TetriminoEnum.Z:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(1,0),
                    new Vector2Int(0,1),
                    new Vector2Int(-1,1)
                };
            case TetriminoEnum.J:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(-1,0),
                    new Vector2Int(1,0),
                    new Vector2Int(-1,1)
                };
            case TetriminoEnum.L:
                return new List<Vector2Int> {
                    new Vector2Int(0,0),
                    new Vector2Int(-1,0),
                    new Vector2Int(1,0),
                    new Vector2Int(1,1)
                };
            case TetriminoEnum.X:
            default:
                return null;
        }
    }

    public static TetriminoEnum getRandomPiece(bool includeX = false) {
        int startIndex = includeX ? 0 : 1; // 0 = X, 1 = first actual piece
        int endIndex = System.Enum.GetValues(typeof(TetriminoEnum)).Length;
        return (TetriminoEnum)Random.Range(startIndex, endIndex);
    }
}
