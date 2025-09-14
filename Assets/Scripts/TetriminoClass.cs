using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TetriminoEnum { // X is empty
    X, I, O, T, S, Z, J, L
}
public class TetriminoClass : MonoBehaviour{
    public static TetriminoClass Instance;

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

    public static TetriminoEnum getRandomPiece(bool includeX = false) {
        int startIndex = includeX ? 0 : 1; // 0 = X, 1 = first actual piece
        int endIndex = System.Enum.GetValues(typeof(TetriminoEnum)).Length;
        return (TetriminoEnum)Random.Range(startIndex, endIndex);
    }
}
