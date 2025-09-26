using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    private GridViewer gridV; // Do to look for it every time
    private System.Random rnd = new System.Random();

    // ========================================================
    //                          START
    // ========================================================
    void Start(){
        // ============= Grid viewer to show results ============= 
        gridV = FindFirstObjectByType<GridViewer>();
        if (gridV == null) {
            Debug.LogWarning("OptimizerManager: No GridViewer found in scene. UI preview will be disabled.");
        }
        

        // ============= Initialize GA variables ============= 
        Debug.Log("Initial poblation size: " + initialPoblation);
        generationI = 0;
        scores = new int[initialPoblation];
        poblation = new Genotype[initialPoblation];
        for (int i = 0; i < initialPoblation; i++) {
            poblation[i] = new Genotype(aleoType, nPieces);
        }

        // For parallel processing
        threadCount = Math.Max(Environment.ProcessorCount - 4, 1);
        gameMs = new GameManager[threadCount];
        for(int i = 0; i < threadCount; i++) {
            gameMs[i] = new GameManager();
        }

        // Create a copy of the bag at that moment
        // If TetriminoSettings instance is not present, produceRandomBag will still work (static data)
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag(2));

        // ============= START ============= 
        // Evaluate the fisrt half of the genotypes to have some scores
        // Rest will be evaluated in the processNextGeneration  
        Debug.Log("FIRST evlauation population " + generationI);
        StartEvaluationThread(poblation[..(initialPoblation / 2)], 0);
    }

    // ========================================================
    //                          UPDATE
    // ========================================================
    //Thread mainComputationThread;
    bool executed = true;
    bool simulating = false;
    void Update(){
        //if (mainComputationThread == null || mainComputationThread.IsAlive) return;
        if (executed || simulating) return;

        // =========================== EVALUATE ========================
        StartEvaluationThread(poblation[(initialPoblation / 2)..], initialPoblation / 2);

        // =========================== SORT BEST ========================
        SortPopulation();

        // =========================== PLAY MOVEMENT ========================
        if (generationI % showEvery == 0) {
            simulating = true;
            StartCoroutine(playGenotype(poblation[sortedIdxs[showingIndex]]));
        }

        // =========================== UPDATE GENERATION ========================
        updateGeneration();

    }
    // ========================================================
    //                          THREDING
    // ========================================================
    void StartEvaluationThread(Genotype[] slice, int startIdx) {
        //mainComputationThread = new Thread(() => evaluateGenotypes(slice));
        //mainComputationThread.Start();
        executed = true;

        evaluateGenotypes(slice, startIdx);
        executed = false;
    }


    // ========================================================
    //                          EVALUATE
    // ========================================================

    void evaluateGenotypes(Genotype[] toEvaluatePoblation, int startIdx) {
        Debug.Log("Initial bagQueueSaved: " + string.Join(", ", bagQueueSaved));

        gameMs[0].resetGame(bagQueueSaved);
        for (int genIdx = 0; genIdx < toEvaluatePoblation.Length; genIdx++) {
            Genotype genotype = toEvaluatePoblation[genIdx];

            // For each piece
            for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
                // For each movement in that piece
                for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                    playMovement(gameMs[0], genotype.movement[pieceI, moveJ], moveJ, aleoType);
                }
                gameMs[0].lockPiece();
                gameMs[0].getNewRandomPiece();
            }
            scores[startIdx+genIdx] = gameMs[0].getScore();


            // Create a copy of the bag saved
            gameMs[0].resetGame(bagQueueSaved);
        }
    }

    IEnumerator playGenotype(Genotype genotype) {
        GameManager gameM = gameMs[0];
        Debug.Log("Playing bagQueueSaved: " + string.Join(", ", bagQueueSaved));
        gameM.resetGame(bagQueueSaved);
        gridV.resetGrid();

        // For each piece
        for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
            // For each movement in that piece
            for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                yield return new WaitForSeconds(timeDelay);
                playMovement(gameM, genotype.movement[pieceI, moveJ], moveJ, aleoType);
            }
            gameM.lockPiece();
            gameM.getNewRandomPiece();

            updateGridViewer(gameM);
        }

        simulating = false;
    }


    public void updateGridViewer(GameManager gameM) {
        gridV.updateGrid(gameM.getGrid());
        gridV.updateGridPositions(
            gameM.getPiecePositions(),
            gameM.getPieceType()
        );
        gridV.updateSwapPiece(gameM.getSwapPieceType());
    }

    // ========================================================
    //                            GA
    // ========================================================
    void updateGeneration() {
        // Reproduce new poblation with all the members, yet replace the worst half
        float[] probs = computeSoftMax(
            sortedIdxs.Select(i => scores[i]).ToArray()
        );
        int[] half = sortedIdxs[..(initialPoblation / 2)];

        Genotype[] newPoblation = new Genotype[initialPoblation];

        // Keep the first half of the best, reproduce the rest
        for (int i = 0; i < half.Length; i++) {
            newPoblation[i] = poblation[half[i]];
        }

        for (int i = half.Length; i < initialPoblation; i+=2) {
            Genotype parent1 = getRandomGenotype(sortedIdxs, probs);
            Genotype parent2 = getRandomGenotype(sortedIdxs, probs);
            newPoblation[i] = parent1.reproduce(parent2, mutationChance);
            if(i+1 < initialPoblation) // not exeding array
                newPoblation[i+1] = parent2.reproduce(parent1, mutationChance);
        }

        poblation = newPoblation;
        generationI++;
    }

    void SortPopulation() {
        sortedIdxs = Enumerable.Range(0, scores.Length).ToArray();
        Array.Sort(sortedIdxs, (a, b) => scores[b].CompareTo(scores[a]));

        Debug.Log("Generation : " + generationI + ", 1st score: " + scores[sortedIdxs[showingIndex]]);
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
        float random = (float)rnd.NextDouble();
        float sum = 0f;
        int i = -1;

        while(sum < random && i < indices.Length - 1) {
            i++;
            sum += probs[i];
        }

        return poblation[indices[i]];
    }


}
