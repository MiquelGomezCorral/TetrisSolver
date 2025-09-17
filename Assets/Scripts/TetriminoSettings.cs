using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum TetriminoEnum { // X is empty
    X, I, O, T, S, Z, J, L
}
public enum DirectionEnum {
    LEFT, UP, RIGHT, DOWN
}
public enum RorateEnum {
    ACLOCK, CLOCK, R180
}
public class TetriminoSettings : MonoBehaviour {
    public static TetriminoSettings Instance;

    [Header("Tetromino Sprites")]
    public Sprite textureX, textureI, textureO, textureT, textureS, textureZ, textureJ, textureL;

    public static readonly List<List<Vector2Int>> JLSTZ_OFFSET_DATA = new List<List<Vector2Int>> {
        new List<Vector2Int> { Vector2Int.zero,              Vector2Int.zero,              Vector2Int.zero,              Vector2Int.zero },
        new List<Vector2Int> { Vector2Int.zero,              new Vector2Int(1, 0),         Vector2Int.zero,              new Vector2Int(-1, 0) },
        new List<Vector2Int> { Vector2Int.zero,              new Vector2Int(1, -1),        Vector2Int.zero,              new Vector2Int(-1, -1) },
        new List<Vector2Int> { Vector2Int.zero,              new Vector2Int(0, 2),         Vector2Int.zero,              new Vector2Int(0, 2) },
        new List<Vector2Int> { Vector2Int.zero,              new Vector2Int(1, 2),         Vector2Int.zero,              new Vector2Int(-1, 2) }
    };
    public static readonly List<List<Vector2Int>> I_OFFSET_DATA = new List<List<Vector2Int>> {
        new List<Vector2Int> { Vector2Int.zero,              new Vector2Int(-1, 0),        new Vector2Int(-1, 1),        new Vector2Int(0, 1) },
        new List<Vector2Int> { new Vector2Int(-1, 0),        Vector2Int.zero,              new Vector2Int(1, 1),         new Vector2Int(0, 1) },
        new List<Vector2Int> { new Vector2Int(2, 0),         Vector2Int.zero,              new Vector2Int(-2, 1),        new Vector2Int(0, 1) },
        new List<Vector2Int> { new Vector2Int(-1, 0),        new Vector2Int(0, 1),         new Vector2Int(1, 0),         new Vector2Int(0, -1) },
        new List<Vector2Int> { new Vector2Int(2, 0),         new Vector2Int(0, -2),        new Vector2Int(-2, 0),        new Vector2Int(0, 2) }
    };

    public static readonly List<List<Vector2Int>> O_OFFSET_DATA = new List<List<Vector2Int>> {
        new List<Vector2Int> { Vector2Int.zero,              Vector2Int.down,              new Vector2Int(-1, -1),       Vector2Int.left }
    };

    public static readonly List<Vector2Int> CLOCK_ROTATION = new List<Vector2Int> {
        new Vector2Int(0, 1), new Vector2Int(-1, 0)
    };

    public static readonly List<Vector2Int> ACLOCK_ROTATION = new List<Vector2Int> {
        new Vector2Int(0, -1), new Vector2Int(1, 0)
    };

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static TetriminoEnum getRandomPiece(bool includeX = false) {
        int startIndex = includeX ? 0 : 1; // 0 = X, 1 = first actual piece
        int endIndex = System.Enum.GetValues(typeof(TetriminoEnum)).Length;
        return (TetriminoEnum)Random.Range(startIndex, endIndex);
    }

    // ==============================================================================
    //                                  TEXTURES
    // ==============================================================================
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

    // ==============================================================================
    //                                  SPAWN POINTS
    // ==============================================================================
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

    // ==============================================================================
    //                                  ROTATION
    // ==============================================================================
    public static DirectionEnum getNewDirection(DirectionEnum direction, RorateEnum rotation) {
        int dir = (int)direction;

        switch (rotation) {
            case RorateEnum.CLOCK:
                dir = (dir + 1) % 4;   // clockwise: LEFT→UP→RIGHT→DOWN→LEFT
                break;
            case RorateEnum.ACLOCK:
                dir = (dir + 3) % 4;   // counter-clockwise: LEFT→DOWN→RIGHT→UP→LEFT
                break;
        }

        return (DirectionEnum)dir;
    }
    public static List<Vector2Int> getTetriminoOffsets(TetriminoEnum pieceType, DirectionEnum direction) {
        switch (pieceType) {
            case TetriminoEnum.O:
                return O_OFFSET_DATA[(int)direction];
            case TetriminoEnum.I:
                return I_OFFSET_DATA[(int)direction];
            default:
                return JLSTZ_OFFSET_DATA[(int)direction];
        }
    }
    public static List<Vector2Int> getRotationMatrix(RorateEnum direction) {
        switch (direction) {
            case RorateEnum.CLOCK:
                return CLOCK_ROTATION;
            case RorateEnum.ACLOCK:
            case RorateEnum.R180:
            default:
                return ACLOCK_ROTATION;
        }
    }

}
