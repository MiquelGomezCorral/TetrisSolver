using Unity.VisualScripting;
using UnityEngine;
using System;

public enum AleoType { 
    Simple, Double,  SwapSimple, SwapDoble
}


public class Genotype {
    public static int[] movementRagnes = { -9, 9 };
    public static int[] rotateRanges = { 0, 3 };
    public static int[] swapRanges = { 0, 1 };


    public int[,] movement;
    public int nPieces;
    public AleoType aleoType;


    // ========================================================
    //                       CONSTRUCTOR
    // ========================================================
    public Genotype(AleoType aleoType, int nPieces) {
        this.aleoType = aleoType;
        this.nPieces = nPieces;
        switch (aleoType) {
            case AleoType.SwapDoble:
                movement = new int[nPieces, 5];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomSwap();
                    movement[i, 1] = getRandomRotate();
                    movement[i, 2] = getRandomMovement();
                    movement[i, 3] = getRandomMovement();
                    movement[i, 4] = getRandomRotate();
                }
                break;
            case AleoType.SwapSimple:
                movement = new int[nPieces, 3];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomSwap();
                    movement[i, 1] = getRandomRotate();
                    movement[i, 2] = getRandomMovement();
                }
                break;
            case AleoType.Double:
                movement = new int[nPieces, 4];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomRotate();
                    movement[i, 1] = getRandomMovement();
                    movement[i, 2] = getRandomMovement();
                    movement[i, 3] = getRandomRotate();
                }
                break;
            case AleoType.Simple:
            default:
                movement = new int[nPieces, 2];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomRotate();
                    movement[i, 1] = getRandomMovement();
                }
                break;
        }
    }

    // ========================================================
    //                       METHODS
    // ========================================================
    public int getRandomMovement() {
        return UnityEngine.Random.Range(movementRagnes[0], movementRagnes[1]);
    }
    public int getRandomRotate() {
        return UnityEngine.Random.Range(rotateRanges[0], rotateRanges[1]);
    }
    public int getRandomSwap() {
        return UnityEngine.Random.Range(swapRanges[0], swapRanges[1]);
    }

    // ========================================================
    //                         GSA
    // ========================================================

}

public class OptimizerManager : MonoBehaviour{
    [SerializeField] float timePerSearch = 1.0f;
    [SerializeField] int initialPoblation = 200;
    [SerializeField] int nPieces = 10;
    [SerializeField] float mutationChange = 0.3f;
    [SerializeField] AleoType aleoType = AleoType.Simple;

    public Genotype[] pobation;
    public int[] scores;

    private GameManager gameM; // Do to look for it every time

    // ========================================================
    //                          START
    // ========================================================
    void Start(){
        gameM = FindFirstObjectByType<GameManager>();


        scores = new int[initialPoblation];
        pobation = new Genotype[initialPoblation];
        for (int i = 0; i < initialPoblation; i++) {
            pobation[i] = new Genotype(aleoType, nPieces);
        }
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update(){
        for(int genI = 0; genI < pobation.Length; genI++) {
            Genotype genotype = pobation[genI];

            // For each piece
            for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
                // For each movement in that piece
                for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                    // Play a movement
                    playMovement(genotype.movement[pieceI, moveJ], moveJ, aleoType);
                }
                // Then spawn new piece
                gameM.spawnNewPiece();
            }
            scores[genI] = gameM.getScore();
            //gameM.reset();
        }
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    private int playMovement(int movement, int pos, AleoType aleoType) {
        if (pos == 0 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) {
            return gameM.swapCurrentPiece();

        } else if (
            (pos == 0 && (aleoType == AleoType.Simple || aleoType == AleoType.Double)) || 
            (pos == 1 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) || 
            (pos == 3 && (aleoType == AleoType.Double)) || 
            (pos == 4 && (aleoType == AleoType.SwapDoble))
        ) {
             gameM.rotateCurrentPiece((RorateEnum)movement);

        } else if (
            (pos == 1 && (aleoType == AleoType.Simple || aleoType == AleoType.Double)) ||
            (pos == 2 && (aleoType == AleoType.Double)) ||
            (pos == 2 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) ||
            (pos == 3 && (aleoType == AleoType.SwapDoble))
        ) {
            DirectionEnum direction = (pos >=0 ) ? DirectionEnum.RIGHT : DirectionEnum.LEFT;
            for (int i = 0; i < Math.Abs(movement); i++) {
                gameM.moveCurrentPieceSide(direction);
            }
        }

        return 0;
    }
}
