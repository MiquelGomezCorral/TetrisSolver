using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using static UnityEditor.PlayerSettings;

public enum AleoType { 
    Simple, Double,  SwapSimple, SwapDoble
}


public class Genotype {
    public static int[] movementInitialRagnes = { -5, 5 };
    public static int[] movementRagnes = { -9, 9 };
    public static int[] rotateRanges = { 0, 3 };
    public static int rotateRangesModulo = rotateRanges[1] + 1;
    public static int[] swapRanges = { 0, 1 };


    public int[,] movement;


    // ========================================================
    //                       CONSTRUCTOR
    // ========================================================
    public Genotype(AleoType aleoType, int nPieces) {
        switch (aleoType) {
            case AleoType.SwapDoble:
                movement = new int[nPieces, 5];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomSwap();
                    movement[i, 1] = getRandomRotate();
                    movement[i, 2] = getRandomMovementInitial();
                    movement[i, 3] = getRandomMovement();
                    movement[i, 4] = getRandomRotate();
                }
                break;
            case AleoType.SwapSimple:
                movement = new int[nPieces, 3];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomSwap();
                    movement[i, 1] = getRandomRotate();
                    movement[i, 2] = getRandomMovementInitial();
                }
                break;
            case AleoType.Double:
                movement = new int[nPieces, 4];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomRotate();
                    movement[i, 1] = getRandomMovementInitial();
                    movement[i, 2] = getRandomMovement();
                    movement[i, 3] = getRandomRotate();
                }
                break;
            case AleoType.Simple:
            default:
                movement = new int[nPieces, 2];

                for (int i = 0; i < nPieces; i++) {
                    movement[i, 0] = getRandomRotate();
                    movement[i, 1] = getRandomMovementInitial();
                }
                break;
        }
    }

    // ========================================================
    //                       METHODS
    // ========================================================
    public int getRandomMovementInitial() {
        return UnityEngine.Random.Range(movementInitialRagnes[0], movementInitialRagnes[1]+1);
    }
    public int getRandomMovement() {
        return UnityEngine.Random.Range(movementRagnes[0], movementRagnes[1]+1);
    }
    public int getRandomRotate() {
        return UnityEngine.Random.Range(rotateRanges[0], rotateRanges[1]+1);
    }
    public int getRandomSwap() {
        return UnityEngine.Random.Range(swapRanges[0], swapRanges[1]+1);
    }

    // ========================================================
    //                         GSA
    // ========================================================


    public void mutate(float chance, AleoType aleoType) {
        bool[,] mutates = GetRandomBooleans(movement.GetLength(0), movement.GetLength(1), chance);
        for (int i = 0; i < movement.GetLength(0); i++) {
            for (int pos = 0; pos < movement.GetLength(1); pos++) {
                if (!mutates[i, pos]) continue;

                if (pos == 0 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) {
                    // Swap: XOR -> stay the same if mutates is true, change if is false.
                    // Since we use a 1 it will always change
                    movement[i, pos] ^= 1;

                } else if (
                    (aleoType == AleoType.Simple && (pos == 0)) ||
                    (aleoType == AleoType.Double && (pos == 0 || pos == 3)) ||
                    (aleoType == AleoType.SwapSimple && (pos == 1)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 1 || pos == 4))
                ) { // Rotatae: -1 or +1 if mutates, always in modulo
                    movement[i, pos] = (
                        movement[i, pos] + 
                        (UnityEngine.Random.value > 0.5f ? 1 : -1) + 
                        rotateRangesModulo
                    ) % rotateRangesModulo;

                } else if (
                    (aleoType == AleoType.Simple && (pos == 1)) ||
                    (aleoType == AleoType.Double && (pos == 1)) ||
                    (aleoType == AleoType.SwapSimple && (pos == 2)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 2))
                ) { // Move intitial
                    if (movement[i, pos] <= movementInitialRagnes[0]) {
                        movement[i, pos] =  movementInitialRagnes[0] + 1;  // move up from lower limit
                    } else if (movement[i, pos] >= movementInitialRagnes[1]) {
                        movement[i, pos] = movementInitialRagnes[1] - 1;  // move down from upper limit
                    } else {
                        movement[i, pos] += (UnityEngine.Random.value > 0.5f ? 1 : -1);
                    }

                } else if (
                    (aleoType == AleoType.Double && (pos == 2)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 3))
                ) { // Move middle
                    if (movement[i, pos] <= movementRagnes[0]) {
                        movement[i, pos] = movementRagnes[0] + 1;  // move up from lower limit
                    } else if (movement[i, pos] >= movementRagnes[1]) {
                        movement[i, pos] = movementRagnes[1] - 1;  // move down from upper limit
                    } else {
                        movement[i, pos] += (UnityEngine.Random.value > 0.5f ? 1 : -1);
                    }

                }
            }
        }
    }

    public bool[,] GetRandomBooleans(int n, int m, float probabilityYes = 0.5f) {
        bool[,] matrix = new bool[n, m];
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < m; j++) {
                matrix[i, j] = UnityEngine.Random.value < probabilityYes;
            }
        }
        return matrix;
    }
}

public class OptimizerManager : MonoBehaviour{
    [SerializeField] float timeDelay = 0.1f;
    [SerializeField] float timePerSearch = 1.0f;
    [SerializeField] int initialPoblation = 200;
    [SerializeField] int nPieces = 10;
    [SerializeField] float mutationChance = 0.15f;
    [SerializeField] AleoType aleoType = AleoType.SwapDoble;

    public Genotype[] pobation;
    public int[] scores;

    private GameManager gameM; // Do to look for it every time
    public static TetriminoEnum[] bagQueueSaved;

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

        StartCoroutine(EvaluateGenotypes());
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update(){
    }

    IEnumerator EvaluateGenotypes() {
        // Create a copy of the bag at that moment
        bagQueueSaved = gameM.getBagsCopy();
        Debug.Log("1 " + string.Join(", ", bagQueueSaved));

        for (int genI = 0; genI < pobation.Length; genI++) {
            Genotype genotype = pobation[genI];

            // For each piece
            for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
                // For each movement in that piece
                for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                    // Wait 0.2s before next movement
                    yield return new WaitForSeconds(timeDelay);
                    // Play a movement
                    playMovement(genotype.movement[pieceI, moveJ], moveJ, aleoType);
                }
                // Then spawn new piece
                gameM.spawnNewPiece();
            }
            scores[genI] = gameM.getScore();
            Debug.Log("Genotype: " + genI + " scored: " + scores[genI]);

            // Create a copy of the bag saved
            Debug.Log("2: " + string.Join(", ", bagQueueSaved));
            gameM.resetGame(new Queue<TetriminoEnum>(bagQueueSaved));
        }
    }
    // ========================================================
    //                          METHODS
    // ========================================================
    private void playMovement(int movement, int pos, AleoType aleoType) {
        if (pos == 0 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) {
            if(movement == 1)
                gameM.swapCurrentPiece();

        } else if (
            (aleoType == AleoType.Simple     && (pos == 0)) ||
            (aleoType == AleoType.Double     && (pos == 0 || pos == 3)) ||
            (aleoType == AleoType.SwapSimple && (pos == 1)) ||
            (aleoType == AleoType.SwapDoble  && (pos == 1 || pos == 4)) 
        ) {
             gameM.rotateCurrentPiece((RorateEnum)movement);

        } else if (
            (aleoType == AleoType.Simple     && (pos == 1)) ||
            (aleoType == AleoType.Double     && (pos == 1 || pos == 2)) ||
            (aleoType == AleoType.SwapSimple && (pos == 2)) ||
            (aleoType == AleoType.SwapDoble  && (pos == 2 || pos == 3))
        ) {
            DirectionEnum direction = (movement >= 0 ) ? DirectionEnum.RIGHT : DirectionEnum.LEFT;
            for (int i = 0; i < Math.Abs(movement); i++) {
                gameM.moveCurrentPieceSide(direction);
            }
        }


        // After the movement, move the piece down for the doubles
        if( (pos == 1 && aleoType == AleoType.Double) || (pos == 2 && aleoType == AleoType.SwapDoble)) {
            gameM.moveCurrentPieceBootom();
        }

    }
}
