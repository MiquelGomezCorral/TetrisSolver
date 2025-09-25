using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OptimizerManager : MonoBehaviour{
    [SerializeField] AleoType aleoType = AleoType.SwapDoble;
    [SerializeField] int initialPoblation = 200;
    [SerializeField] int nPieces = 10;
    [SerializeField] float mutationChance = 0.15f;

    [SerializeField] float timePerSearch = 1.0f;
    [SerializeField] float timeDelay = 0.1f;
    [SerializeField] public int showingIndex = 0;
    [SerializeField] public int showEvery = 100;

    public Genotype[] poblation;
    public int[] scores;
    public int[] sortedIdxs;
    public int generationI;
    Queue<TetriminoEnum> bagQueueSaved;

    public int threadCount;
    private GameManager[] gameMs;

    private GameViewer gameV; // Do to look for it every time

    // ========================================================
    //                          START
    // ========================================================
    void Start(){
        Debug.Log("Initial poblation size: " + initialPoblation);
        generationI = 0;
        scores = new int[initialPoblation];
        poblation = new Genotype[initialPoblation];
        for (int i = 0; i < initialPoblation; i++) {
            poblation[i] = new Genotype(aleoType, nPieces);
        }

        threadCount = Math.Max(Environment.ProcessorCount - 4, 1);
        gameMs = new GameManager[threadCount];
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    bool executed  = true;
    void Update(){
        if (executed) { return;  }
        executed = true;

        // Create a copy of the bag at that moment
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag());
        // Evaluate current Genotypes
        //StartCoroutine(EvaluateGenotypes());
        EvaluateGenotypes();

        // sort indices by scores
        sortedIdxs = Enumerable.Range(0, scores.Length).ToArray();
        Array.Sort(sortedIdxs, (a, b) => scores[b].CompareTo(scores[a]));

        if (generationI % showEvery == 0) {
            StartCoroutine(playGenotype(
                poblation[sortedIdxs[showingIndex]]
            ));
        } else {
            executed = false;
            updateGeneration();
        }

        Debug.Log("Generation : " + generationI + ", 1st score: " + scores[sortedIdxs[showingIndex]]);
        // Update generation
    }

    void EvaluateGenotypes() {
        gameMs[0].resetGame(new Queue<TetriminoEnum>(bagQueueSaved));
        for (int genI = 0; genI < poblation.Length; genI++) {
            Genotype genotype = poblation[genI];

            // For each piece
            for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
                // For each movement in that piece
                for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                    // Wait 0.2s before next movement
                    //yield return new WaitForSeconds(timeDelay);
                    playMovement(gameMs[0], genotype.movement[pieceI, moveJ], moveJ, aleoType);
                }
                gameMs[0].getNewRandomPiece();
            }
            scores[genI] = gameMs[0].getScore();
            Debug.Log("Genotype: " + genI + " scored: " + scores[genI]);

            // Create a copy of the bag saved
            gameMs[0].resetGame(bagQueueSaved);
        }
    }

    IEnumerator playGenotype(Genotype genotype) {
        // For each piece
        for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
            // For each movement in that piece
            for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                // Wait 0.2s before next movement
                yield return new WaitForSeconds(timeDelay);

                playMovement(gameMs[0], genotype.movement[pieceI, moveJ], moveJ, aleoType);
            }
            gameMs[0].getNewRandomPiece();
        }
        Debug.Log("Best genotype scored " + " scored: " + gameMs[0].getScore());

        // Create a copy of the bag saved
        gameMs[0].resetGame(new Queue<TetriminoEnum>(bagQueueSaved));

        executed = false;
        updateGeneration();
    }

    // ========================================================
    //                            GA
    // ========================================================
    void updateGeneration() {
        // Get the best half and first quarter of them (ignore the others)
        // Keep with the same values the first quarter 
        // Use the half to reproduce the other three quarters

        int[] half = sortedIdxs[..(initialPoblation / 2)];
        int[] quart = sortedIdxs[..(initialPoblation / 4)];
        float[] probs = computeSoftMax(
            half.Select(i => scores[i]).ToArray()
        );

        Genotype[] newPoblation = new Genotype[initialPoblation];

        for (int i = 0; i < quart.Length; i++) {
            newPoblation[i] = poblation[quart[i]];
        }

        // Keep the first quart of the best, reproduce the rest
        for (int i = quart.Length; i < initialPoblation; i++) {
            Genotype parent1 = getRandomGenotype(half, probs);
            Genotype parent2 = getRandomGenotype(half, probs);

            newPoblation[i] = parent1.reproduce(parent2, mutationChance);
        }

        poblation = newPoblation;
        generationI++;
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    private void playMovement(GameManager gameM, int movement, int pos, AleoType aleoType) {
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

    public float[] computeSoftMax(int[] scores) {
        float[] result = new float[scores.Length];

        float divisor = 0f;
        for (int i = 0;i < scores.Length;i++) {
            result[i] = Mathf.Exp(scores[i]);
            divisor += result[i];
        }

        for (int i = 0; i < scores.Length; i++) {
            result[i] /= divisor;
        }

        return result;
    }

    public Genotype getRandomGenotype(int[] indices, float[] probs) {
        float random = UnityEngine.Random.Range(0f, 1f);
        float sum = 0f;
        int i = -1;

        while(sum < random) {
            i++;
            sum += probs[i];
        }

        return poblation[indices[i]];
    }


}
