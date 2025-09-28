using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class OptimizerManager : MonoBehaviour{
    [Header("Algorithm Parameters")]
    [SerializeField] bool executeComputation = true;
    [SerializeField] AleoType aleoType = AleoType.SwapDoble;
    [SerializeField] int initialPoblation = 200;
    [SerializeField] int nPieces = 10;
    [SerializeField] float mutationChance = 0.15f;

    [SerializeField] float timePerSearch = 1.0f;
    [SerializeField] float timeDelay = 0.1f;
    [SerializeField] int showingIndex = 0;
    [SerializeField] int showEvery = 100;


    [Header("Scoring Parameters")]
    [SerializeField] float softMaxTemp = 1.0f;
    [SerializeField] float penalizationFactor = 1.0f;
    [SerializeField] float gameScoreFactor = 1.0f;
    [SerializeField] float generalHeuristicFactor = 1.0f;

    [Header("Heuristic Parameters")]
    [SerializeField] float BlocksHFactor = -1.0f;
    [SerializeField] float WeightedBlocksHFactor = -1.0f;
    [SerializeField] float ClearableLineHFactor = 1.0f;
    [SerializeField] float RoughnessHFactor = -1.0f;
    [SerializeField] float ColHolesHFactor = -1.0f;
    [SerializeField] float ConnectedHolesHFactor = -1.0f;
    [SerializeField] float PitHolePercentHFactor = -1.0f;
    [SerializeField] float DeepestWellHFactor = -1.0f;

    // ============= GA population =============
    public Genotype[] poblation;
    public float[] scores;
    public int[] sortedIdxs;
    public int generationI;
    Queue<TetriminoEnum> bagQueueSaved;

    // ============= For parallel processing =============
    // Batch size for work distribution
    private const int MIN_BATCH_SIZE = 50; // Minimum work per thread to avoid overhead
    public int threadCount;
    private bool executing = false;
    private bool simulating = false;

    private class ThreadLocalData {
        public GameManager gameManager;
    }
    private ThreadLocal<GameManager> threadLocalGameManager = new ThreadLocal<GameManager>(() => new GameManager());
    private ParallelOptions parallelOptions;

    // ============= Visualizaton and random =============
    private GridViewer gridV; // Do to look for it every time
    private System.Random rnd = new System.Random();
    private ThreadLocal<System.Random> threadRandom = new ThreadLocal<System.Random>(() =>
        new System.Random(Guid.NewGuid().GetHashCode())
    );

    // ========================================================
    //                          START
    // ========================================================
    void Start(){
        if (!executeComputation) 
            return;
        // ============= Grid viewer to show results ============= 
        gridV = FindFirstObjectByType<GridViewer>();
        if (gridV == null) {
            Debug.LogWarning("OptimizerManager: No GridViewer found in scene. UI preview will be disabled.");
        }
        // ============= Initialize GA variables ============= 
        Debug.Log("Initial poblation size: " + initialPoblation);
        generationI = 0;
        scores = new float[initialPoblation];
        poblation = new Genotype[initialPoblation];
        for (int i = 0; i < initialPoblation; i++) {
            poblation[i] = new Genotype(aleoType, nPieces);
        }
        // Get the bags for all the pieces
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag((nPieces + 1) / 7 ));

        // ============= Setup parallel processing ============= 
        // Use fewer threads if population is small
        int maxUsefulThreads = Math.Max(initialPoblation / MIN_BATCH_SIZE, 1);
        threadCount = Math.Min(Math.Max(Environment.ProcessorCount - 4, 1), maxUsefulThreads);

        Debug.Log($"Using {threadCount} threads for population of {initialPoblation}");

        // Setup parallel options
        parallelOptions = new ParallelOptions {
            MaxDegreeOfParallelism = threadCount
        };


        // ============= START ============= 
        // Evaluate the fisrt half of the genotypes to have some scores
        // Rest will be evaluated in the processNextGeneration  
        executing = true;
        Task.Run(() => {
            EvaluatePopulation(0, initialPoblation / 2);
            executing = false;
        });
    }


    // remove threads on destroy
    void OnDestroy() {
        threadRandom?.Dispose();
        threadLocalGameManager?.Dispose();

    }
    // ========================================================
    //                          UPDATE
    // ========================================================

    void Update(){
        // =========================== GA ========================
        if (executing) return;

        executing = true;
        Task.Run(() => {
            GAStep();
            executing = false;
        });
       
        // =========================== PLAY MOVEMENT ========================
        if (!simulating && generationI % showEvery == 0 && generationI != 0) {
            simulating = true;
            StartCoroutine(playGenotype(poblation[sortedIdxs[showingIndex]]));
        }
    }

   
    private void GAStep() {
        // =========================== EVALUATE ========================
        EvaluatePopulation(initialPoblation / 2, initialPoblation);

        // =========================== SORT BEST ========================
        SortPopulation();

        // =========================== UPDATE GENERATION ========================
        updateGeneration();
    }

    // ========================================================
    //                          THREDING
    // ========================================================
    void EvaluatePopulation(int startIdx, int endIdx) {
        int totalWork = endIdx - startIdx;
        int batchSize = Math.Max(MIN_BATCH_SIZE, totalWork / (threadCount * 4)); // smaller batches for better load balancing

        // Use Parallel.ForEach with partitioner for better load balancing
        var partitioner = Partitioner.Create(startIdx, endIdx, batchSize);

        try {
            Parallel.ForEach(partitioner, parallelOptions, (range) => {
                try {
                    var localGameM = threadLocalGameManager.Value;

                    for (int genIdx = range.Item1; genIdx < range.Item2; genIdx++) {
                        scores[genIdx] = EvaluateGenotype(
                            poblation[genIdx],
                            localGameM
                        );
                    }
                } catch (Exception e) {
                    Debug.LogError("Error in parallel thread: " + e.Message + "\n" + e.StackTrace);
                    throw; // Re-throw to stop the parallel execution
                }
            });
        } catch (Exception e) {
            Debug.LogError("Parallel.ForEach failed: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ========================================================
    //                          EVALUATE
    // ========================================================
    float EvaluateGenotype(Genotype genotype, GameManager gameM) {
        // Reset with pre-copied queue
        gameM.resetGame(bagQueueSaved);

        int penalization = 0;

        // For each piece
        for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
            // For each movement in that piece
            for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                penalization += playMovement(gameM, genotype.movement[pieceI, moveJ], moveJ, aleoType);
            }
            gameM.lockPiece();
        }

        return (
            penalization * penalizationFactor +
            gameM.getScore() * gameScoreFactor +
            gameM.getHeuristicScore(
                BlocksHFactor,
                WeightedBlocksHFactor,
                ClearableLineHFactor,
                RoughnessHFactor,
                ColHolesHFactor,
                ConnectedHolesHFactor,
                PitHolePercentHFactor,
                DeepestWellHFactor
            ) * generalHeuristicFactor
        );
    }



    // ========================================================
    //                      PLAY VISUALY
    // ========================================================
    IEnumerator playGenotype(Genotype genotype) {
        GameManager gameM = new GameManager();
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

            updateGridViewer(gameM);
        }

        simulating = false;
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

        Debug.Log("Generation : " + generationI + " scored " + scores[sortedIdxs[showingIndex]]);
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    private int playMovement(GameManager gameM, int movement, int pos, AleoType aleoType) {
        // Penalize 1 point per each invalid movement
        int penalization = 0;
        if (pos == 0 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) {
            if(movement == 1) // no penalization for swap
                gameM.swapCurrentPiece();

        } else if (
            (aleoType == AleoType.Simple     && (pos == 0)) ||
            (aleoType == AleoType.Double     && (pos == 0 || pos == 3)) ||
            (aleoType == AleoType.SwapSimple && (pos == 1)) ||
            (aleoType == AleoType.SwapDoble  && (pos == 1 || pos == 4)) 
        ) {
            // if the rotation is not possible, penalize
            penalization = gameM.rotateCurrentPiece((RorateEnum)movement) ? 0 : -1;

        } else if (
            (aleoType == AleoType.Simple     && (pos == 1)) ||
            (aleoType == AleoType.Double     && (pos == 1 || pos == 2)) ||
            (aleoType == AleoType.SwapSimple && (pos == 2)) ||
            (aleoType == AleoType.SwapDoble  && (pos == 2 || pos == 3))
        ) {
            DirectionEnum direction = (movement >= 0 ) ? DirectionEnum.RIGHT : DirectionEnum.LEFT;
            int movementsLeft = Math.Abs(movement);
            while (movementsLeft > 0 && gameM.moveCurrentPieceSide(direction)) {
                movementsLeft--;
            }
            penalization = -movementsLeft; //penalize movements not done
        }


        // After the movement, move the piece down for the doubles
        if( (pos == 1 && aleoType == AleoType.Double) || (pos == 2 && aleoType == AleoType.SwapDoble)) {
            gameM.moveCurrentPieceBootom();
        }

        return penalization;
    }

    public float[] computeSoftMax(float[] scores) {
        float[] result = new float[scores.Length];

        float divisor = 0f;
        for (int i = 0;i < scores.Length;i++) {
            result[i] = Mathf.Exp(scores[i] / softMaxTemp);
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

    public void updateGridViewer(GameManager gameM) {
        gridV.updateGrid(gameM.getGrid());
        gridV.updateGridPositions(
            gameM.getPiecePositions(),
            gameM.getPieceType()
        );
        gridV.updateSwapPiece(gameM.getSwapPieceType());
    }
}
