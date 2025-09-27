using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using UnityEngine.UIElements;

// =================================================================
//                              ENUMS
// =================================================================
public enum TetriminoEnum { // X is empty
    X, I, O, T, S, Z, J, L
}
public enum DirectionEnum {
    UP, RIGHT, DOWN, LEFT
}
public enum RorateEnum {  // X is empty
    X, ACLOCK, CLOCK, R180
}
public enum ActionEnum {
    MOVE, T_SPIN, MINI_T_SPIN
}

// =================================================================
//                              SETTINGS
// =================================================================
public class TetriminoSettings : MonoBehaviour {
    [SerializeField] public static int width = 10, height = 20;

    public static int PerfectClearPoints = 20;
    public static TetriminoSettings Instance;

    public Sprite textureX, textureI, textureO, textureT, textureS, textureZ, textureJ, textureL;
    public Sprite texturePieceX, texturePieceI, texturePieceO, texturePieceT, texturePieceS, texturePieceZ, texturePieceJ, texturePieceL;

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
        new List<Vector2Int> { Vector2Int.zero,              new Vector2Int(0, -1),              new Vector2Int(-1, -1), new Vector2Int(-1, 0) }
    };

    public static readonly List<Vector2Int> CLOCK_ROTATION = new List<Vector2Int> {
        new Vector2Int(0, 1), new Vector2Int(-1, 0)
    };

    public static readonly List<Vector2Int> ACLOCK_ROTATION = new List<Vector2Int> {
        new Vector2Int(0, -1), new Vector2Int(1, 0)
    };

    public static List<TetriminoEnum> basePiecesBag = new List<TetriminoEnum>{
        TetriminoEnum.I, TetriminoEnum.O, TetriminoEnum.T,
        TetriminoEnum.S, TetriminoEnum.Z, TetriminoEnum.J, TetriminoEnum.L
    };


    // For random Processed
    private static ThreadLocal<System.Random> rng = new ThreadLocal<System.Random>(() => new System.Random());
    // ========================================================
    //                          AWAKE
    // ========================================================
    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

   

    // ==============================================================================
    //                                  TEXTURES
    // ==============================================================================
    public static Sprite getTetriminoTexture(TetriminoEnum pieceType) {
        // Ensure Instance exists (lazy-find) to avoid NullReferenceExceptions
        if (Instance == null) {
            Instance = FindObjectOfType<TetriminoSettings>();
            if (Instance == null) {
                Debug.LogError("TetriminoSettings.Instance is null. Please add a TetriminoSettings MonoBehaviour to the scene.");
                return null;
            }
        }

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
    public static Sprite getTetriminoPieceTexture(TetriminoEnum pieceType) {
        // Ensure Instance exists (lazy-find) to avoid NullReferenceExceptions
        if (Instance == null) {
            Instance = FindObjectOfType<TetriminoSettings>();
            if (Instance == null) {
                Debug.LogError("TetriminoSettings.Instance is null. Please add a TetriminoSettings MonoBehaviour to the scene.");
                return null;
            }
        }

        switch (pieceType) {
            case TetriminoEnum.I: return Instance.texturePieceI;
            case TetriminoEnum.O: return Instance.texturePieceO;
            case TetriminoEnum.T: return Instance.texturePieceT;
            case TetriminoEnum.S: return Instance.texturePieceS;
            case TetriminoEnum.Z: return Instance.texturePieceZ;
            case TetriminoEnum.J: return Instance.texturePieceJ;
            case TetriminoEnum.L: return Instance.texturePieceL;
            case TetriminoEnum.X:
            default: return Instance.texturePieceX;
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
                dir = (dir + 1) % 4; // clockwise: LEFT→UP→RIGHT→DOWN→LEFT
                break;
            case RorateEnum.ACLOCK:
                dir = (dir + 3) % 4; // counter-clockwise: LEFT→DOWN→RIGHT→UP→LEFT
                break;
        }

        return (DirectionEnum)dir;
    }
    public static List<List<Vector2Int>> getTetriminoOffsets(TetriminoEnum pieceType) {
        switch (pieceType) {
            case TetriminoEnum.O:
                return O_OFFSET_DATA;
            case TetriminoEnum.I:
                return I_OFFSET_DATA;
            default:
                return JLSTZ_OFFSET_DATA;
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

    // ==============================================================================
    //                                   BAGS PIECE 
    // ==============================================================================
    public static List<TetriminoEnum> produceRandomBag(int numBags = 1) {
        List<TetriminoEnum> result = new List<TetriminoEnum>();

        for (int b = 0; b < numBags; b++) {
            // Copy base bag
            List<TetriminoEnum> newBag = new List<TetriminoEnum>(basePiecesBag);

            // Shuffle only this bag
            for (int i = 0; i < newBag.Count; i++) {
                int j = rng.Value.Next(0, newBag.Count);
                TetriminoEnum tmp = newBag[i];
                newBag[i] = newBag[j];
                newBag[j] = tmp;
            }

            // Append shuffled bag to result
            result.AddRange(newBag);
        }

        return result;
    }


    // ==============================================================================
    //                                  SCORE
    // ==============================================================================
    public static int computeScore(int count, ActionEnum lastAction, bool PerfectClear) {
        if(count < 0 || count > 4) {
            Debug.LogError("COUNT OF CLEARED LINES NOT VALID, UNCONSISTENT POINTS. Cleared lines:" + count);
        }

        int score = PerfectClear ? PerfectClearPoints : 0;
        if (lastAction == ActionEnum.MOVE) {
            switch (count) {
                case 0: return score;
                case 1: return score + 1;
                case 2: return score + 3;
                case 3: return score + 5;
                case 4: 
                default:
                    return score + 8;
            }
        }else if (lastAction == ActionEnum.T_SPIN) {
            switch (count) {
                case 0: return score; //  + 4;  // DO NOT GIVE EXTRA FOR USELESS TSPINS
                case 1: return score + 0;//8;
                case 2: return score + 0;//12;
                case 3: 
                default:
                    return score + 0;//16;
            }
        } else {  // if (lastAction == ActionEnum.MINI_T_SPIN) {
            switch (count) {
                case 0: return score; // + 1; // DO NOT GIVE EXTRA FOR USELESS TSPINS
                case 1:
                default:
                    return score + 0;//2;
            }
        }
    }
    // ==============================================================================
    //                                  HEURISTICS
    // ==============================================================================
    public static float computeBlocks(TetriminoEnum[,] grid) {
        int count = 0;
        bool flag = true; // to check if there is at least one block
        for (int y = 0; y < grid.GetLength(1) && flag; y++) {
            flag = false;
            for (int x = 0; x < grid.GetLength(0); x++) {
                if (grid[x, y] != TetriminoEnum.X) {
                    flag = true;
                    count++;
                }
            }
        }
        //Debug.Log("Blocks: " + count);
        return count;
    }
    public static float computeWeightedBlocks(TetriminoEnum[,] grid){
        // Level 0 gives 1, 1 gives 2...
        int count = 0;
        bool flag = true; // to check if there is at least one block
        for (int y = 0; y < grid.GetLength(1) && flag; y++) {
            flag = false;
            for (int x = 0; x < grid.GetLength(0); x++) {
                if (grid[x, y] != TetriminoEnum.X) {
                    flag = true;
                    count+= y + 1; //add weight according to height
                }
            }
        }
        //Debug.Log("WeightedBlocks: " + count);
        return count;
    }
    public static float computeClearableLine(TetriminoEnum[,] grid){
        // maximum number of lines clearable by a single “I” (straight) piece in the current board configuration
        // Look for each column and get the highest block, then check the next four rows and compute how many are full

        int[] counts = new int[grid.GetLength(0)];
        for (int x = 0; x < grid.GetLength(0); x++) {
            int highest = grid.GetLength(1)-1;
            while (highest >= 0 && grid[x, highest] == TetriminoEnum.X) {
                highest--;
            } // Can go downto -1 but is okey
            highest++;

            // Too hight to fit and I piece
            if (highest + 3 >= grid.GetLength(1)) continue;


            // Check the four next rows
            for (int yy = highest; yy < highest + 4 && yy < grid.GetLength(1); yy++) {
                bool full = true;
                for (int xx = 0; xx < grid.GetLength(0) && full; xx++) {
                    if (xx == x) continue; // skip current column
                    full &= grid[xx, yy] != TetriminoEnum.X;
                }
                counts[x] += full ? 1 : 0; //1 if full else 0
            }
        }

        int max = counts.Max();
        //Debug.Log("ClearableLine: " + max);
        return max;
    }
    public static float computeRoughness(TetriminoEnum[,] grid){
        // get the max height of each column and from left to righ
        // compute the difference between each level

        int[] maxHeigth = new int[grid.GetLength(0)];
        for (int x = 0; x < grid.GetLength(0); x++) {
            maxHeigth[x] = grid.GetLength(1) - 1;
            while (maxHeigth[x] >= 0 && grid[x, maxHeigth[x]] == TetriminoEnum.X) {
                maxHeigth[x]--;
            } // Can go downto -1 but is okey
            maxHeigth[x]++;
        }
        int roughness = 0;
        for (int x = 0; x < grid.GetLength(0) - 1; x++) {
            roughness += Mathf.Abs(maxHeigth[x] - maxHeigth[x + 1]);
        }

        Debug.Log("Roughness: " + roughness);
        return roughness;
    }
    public static float computeConnectedHoles(TetriminoEnum[,] grid){
        return 0;
    }
    public static float computePitHolePercent(TetriminoEnum[,] grid){
        return 0;
    }
    public static float computeColHoles(TetriminoEnum[,] grid){
        return 0;
    }
    public static float computeDeepestWell(TetriminoEnum[,] grid){
        return 0;
    }

}
