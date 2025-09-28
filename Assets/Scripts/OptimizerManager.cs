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
    public int threadCount;
    private int activeThreads = 0;
    private GameManager[] gameMs;

    private Thread[] workerThreads;
    private BlockingCollection<WorkItem> workQueue;
    private bool shutdownRequested = false;

    // Work item structure
    private struct WorkItem {
        public int startIdx;
        public int endIdx;
        public int threadIdx;
    }

    bool executing = false;
    bool simulating = false;
    // ============= Visualizaton and random =============
    private GridViewer gridV; // Do to look for it every time
    private System.Random rnd = new System.Random();

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

        // For parallel processing
        threadCount = Math.Max(Environment.ProcessorCount - 4, 1);
        //threadCount = 8;
        gameMs = new GameManager[threadCount];
        workQueue = new BlockingCollection<WorkItem>();
        workerThreads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++) {
            gameMs[i] = new GameManager();

            // Create persistent worker thread
            int localThreadIdx = i; // capture for closure
            workerThreads[i] = new Thread(() => WorkerThreadLoop(localThreadIdx));
            workerThreads[i].IsBackground = true;
            workerThreads[i].Start();
        }

        // Get the bags for all the pieces
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag((nPieces + 1) / 7 )); 

        // ============= START ============= 
        // Evaluate the fisrt half of the genotypes to have some scores
        // Rest will be evaluated in the processNextGeneration  
        executing = true;
        StartEvaluationThread(0, initialPoblation / 2);
        executing = false;
    }


    // remove threads on destroy
    void OnDestroy() {
        shutdownRequested = true;
        workQueue?.CompleteAdding();
        workQueue?.Dispose();
    }
    // ========================================================
    //                          UPDATE
    // ========================================================

    void Update(){
        // =========================== GA ========================
        if (executing) return;
        executing = true;
        new Thread(GAStep).Start();
       
        // =========================== PLAY MOVEMENT ========================
        if (!simulating && generationI % showEvery == 0 && generationI != 0) {
            simulating = true;
            StartCoroutine(playGenotype(poblation[sortedIdxs[showingIndex]]));
        }
    }

   
    private void GAStep() {
        // =========================== EVALUATE ========================
        StartEvaluationThread(initialPoblation / 2, initialPoblation);

        // =========================== SORT BEST ========================
        SortPopulation();

        // =========================== UPDATE GENERATION ========================
        updateGeneration();

        executing = false;
    }
    // ========================================================
    //                          THREDING
    // ========================================================
    void StartEvaluationThread(int startIdx, int endIdx) {
        // Process from startIdx to endIdx (not included)
        int threadSliceLenght = (endIdx - startIdx) / threadCount;
        Interlocked.Exchange(ref activeThreads, 0);

        for (int threadIdx = 0; threadIdx < threadCount; threadIdx++) {
            // capture locals to avoid closure issues
            int localThreadIdx = threadIdx;
            int fromIdx = startIdx + threadIdx * threadSliceLenght;
            // Compute next + give the reminder to the last thread
            int toIdx = (threadIdx == threadCount - 1) ? endIdx : startIdx + (threadIdx + 1) * threadSliceLenght;


            if (fromIdx >= toIdx) continue; // nothing to do for this thread

            Interlocked.Increment(ref activeThreads);
            workQueue.Add(new WorkItem { startIdx = fromIdx, endIdx = toIdx, threadIdx = threadIdx });
        }

        while (Volatile.Read(ref activeThreads) > 0) {
            Thread.Sleep(8); // 8ms sleep for responsiveness and not to waste cpu
        }
    }

    private void WorkerThreadLoop(int threadIdx) {
        try {
            while (!shutdownRequested) {
                WorkItem workItem;
                if (workQueue.TryTake(out workItem, 8)) { // 8ms timeout
                    evaluateGenotypes(workItem.startIdx, workItem.endIdx, workItem.threadIdx);
                }
            }
        } catch (InvalidOperationException) {
            // Queue was disposed, thread should exit
        }
    }


    // ========================================================
    //                          EVALUATE
    // ========================================================
    void evaluateGenotypes(int startIdx, int endIdx, int threadIdx) {
        // Process from startIdx to endIdx (not included) with gameManager threadIdx
        gameMs[threadIdx].resetGame(bagQueueSaved);

        for (int genIdx = startIdx; genIdx < endIdx; genIdx++) {
            Genotype genotype = poblation[genIdx];
            int penalization = 0;

            // For each piece
            for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
                // For each movement in that piece
                for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                    // apply penalization for each invalid movement
                    penalization += playMovement(gameMs[threadIdx], genotype.movement[pieceI, moveJ], moveJ, aleoType);
                }
                gameMs[threadIdx].lockPiece();
            }

            scores[genIdx] = (
                penalization * penalizationFactor + 
                gameMs[threadIdx].getScore() * gameScoreFactor + 
                gameMs[threadIdx].getHeuristicScore(
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

            // Create a copy of the bag saved
            gameMs[threadIdx].resetGame(bagQueueSaved);
        }

        // signal thread finished
        Interlocked.Decrement(ref activeThreads);
    }

    IEnumerator playGenotype(Genotype genotype) {
        GameManager gameM = gameMs[1];
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
