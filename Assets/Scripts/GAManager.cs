using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GAManager : MonoBehaviour{
    [Header("Algorithm Parameters")]
    [SerializeField] bool executeComputation = true;
    [SerializeField] bool optimizeWithSA = true;
    [SerializeField] bool logExecution = true;
    [SerializeField] AleoType aleoType = AleoType.SwapDoble;
    [SerializeField] int initialPoblation = 20000;
    [SerializeField] int nPieces = 10;
    [SerializeField] float mutationChance = 0.15f;

    [SerializeField] int maxGenerations = 200;
    [SerializeField] float timeDelay = 0.1f;
    [SerializeField] int showingIndex = 0;
    [SerializeField] int showEvery = 25;


    [Header("Scoring Parameters")]
    [SerializeField] float softMaxTemp = 100;
    [SerializeField] float softMaxTempInitialTemp = 100;
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
    [SerializeField] float BlockAboveHolesHFactor = -1.0f;
    [SerializeField] float PitHolePercentHFactor = -1.0f;
    [SerializeField] float DeepestWellHFactor = -1.0f;
    

    // ============= GA population =============
    public Genotype[] poblation;
    public float[] scores;
    public int[] sortedIdxs;
    public int generationI;
    Queue<TetriminoEnum> bagQueueSaved;
    TetriminoEnum[,] currentState;

    // ============= SA integration =============
    SimulatedAnneling saManager;
    public Genotype bestGenotype;
    // ============= Logging =============
    FileLogger fileLogger;

    // ============= For parallel processing =============
    // Batch size for work distribution
    private const int MIN_BATCH_SIZE = 50; // Minimum work per thread to avoid overhead
    public int threadCount;
    private bool executing = false;
    private bool simulating = false;
    private bool optimizingSA = false;

    private ThreadLocal<GameManager> threadLocalGameManager = new ThreadLocal<GameManager>(() => new GameManager());
    private ParallelOptions parallelOptions;

    // ============= Visualizaton and random =============
    private GridViewer gridV; // Do to look for it every time
    private System.Random rnd = new System.Random(TetriminoSettings.seed);

    // ========================================================
    //                          START
    // ========================================================
    void Start(){
        if (!executeComputation)
            return;
        if (logExecution){
            fileLogger = new FileLogger(
                $"GA_Log_{DateTime.Now:yyyyMMdd_HHmmss}" +
                $"-Aleo_{aleoType}" +
                $"-Pop_{initialPoblation}" +
                $"-Mut_{mutationChance}" +
                $"-Pieces_{nPieces}" +
                $"-Seed_{TetriminoSettings.seed}"
            );
        }


        logGA($"Initial poblation size: {initialPoblation}");
        // ============= Grid viewer to show results ============= 
        gridV = FindFirstObjectByType<GridViewer>();
        if (gridV == null) {
            Debug.LogWarning("OptimizerManager: No GridViewer found in scene. UI preview will be disabled.");
        }
        // ============= Initialize GA variables ============= 
       logGA("Initial poblation size: " + initialPoblation);
        startPoblation();

        // ============= Setup parallel processing ============= 
        // Use fewer threads if population is small
        int maxUsefulThreads = Math.Max(initialPoblation / MIN_BATCH_SIZE, 1);
        //threadCount = Math.Min(Math.Max(Environment.ProcessorCount - 4, 1), maxUsefulThreads);
        threadCount = 1;

       logGA($"Using {threadCount} threads for population of {initialPoblation}");

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
    void OnDestroy(){
        threadLocalGameManager?.Dispose();

    }
    
    void logGA(string message) {
        if (logExecution && fileLogger != null){
            fileLogger.Log(message);
        }
       Debug.Log(message);
    }
    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update(){
        if (optimizingSA){
            if (saManager.finished){
                optimizingSA = false;
                bestGenotype = saManager.bestGenotype;
                logGA($"SA finished after {saManager.generationI} generations. Best score {saManager.score} Genotype:\n{bestGenotype}");
                Destroy(saManager);
            }
        }
        if (executing || simulating || optimizingSA) return;
        // =========================== PLAY MOVEMENT ========================
        if (!simulating && generationI % showEvery == 0 && generationI != 0) {
           logGA($"================== PALYING ===================\n Score {scores[sortedIdxs[0]]}:");
            simulating = true;
            // EvaluateGenotype(poblation[sortedIdxs[showingIndex]], new GameManager(), true);
            StartCoroutine(playGenotype(poblation[sortedIdxs[showingIndex]]));

            if (optimizeWithSA){
                optimizingSA = true;
                saManager = gameObject.AddComponent<SimulatedAnneling>();
                saManager.Initialize(
                    maxGenerations,
                    poblation[sortedIdxs[showingIndex]],
                    aleoType
                );
            }
        }

        // =========================== GA ========================
        if (generationI >= maxGenerations) {
           logGA($"================== FINISHED ===================\n Score {scores[sortedIdxs[0]]}:");
            currentState = getPlayedState(poblation[sortedIdxs[0]]);
            startPoblation();

            // Re-evaluate the fisrt half of the genotypes to have some scores
            executing = true;
            Task.Run(() => {
                EvaluatePopulation(0, initialPoblation / 2);
                executing = false;
            });

            return;
        }

        executing = true;
        Task.Run(() => {
            GAStep();
            executing = false;
        });
       
        //threadCount = 1;
    }

   
    private void GAStep() {
        // =========================== EVALUATE ========================
        EvaluatePopulation(initialPoblation / 2, initialPoblation);

        // =========================== SORT BEST ========================
        SortPopulation();

        // =========================== UPDATE GENERATION ========================
        updateGeneration();

        // =========================== UPDATE TEMP ========================
        softMaxTemp = Mathf.Max(0.1f, softMaxTempInitialTemp / Mathf.Log(generationI + 2));
        // softMaxTemp = Mathf.Max(0.1f, softMaxTempInitialTemp / generationI);
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
    float EvaluateGenotype(Genotype genotype, GameManager gameM, bool logHeuristics = false) {
        // Reset with pre-copied queue
        gameM.resetGame(bagQueueSaved, currentState);

        int penalization = 0;

        // For each piece
        for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++){
            // For each movement in that piece
            for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++){
                penalization += playMovement(gameM, genotype.movement[pieceI, moveJ], moveJ, aleoType);
            }
            gameM.lockPiece();
        }
        



        if (logHeuristics){
           logGA("penalization: " + penalization + " * " + penalizationFactor + " = " + (penalization * penalizationFactor));
           logGA("Score: " + gameM.getScore() + " * " + gameScoreFactor + " = " + (gameM.getScore() * gameScoreFactor));
           logGA("Blocks: " + TetriminoSettings.computeBlocks(gameM.getGrid()) + " * " + BlocksHFactor + " = " + (TetriminoSettings.computeBlocks(gameM.getGrid()) * BlocksHFactor));
           logGA("WeightedBlocks: " + TetriminoSettings.computeWeightedBlocks(gameM.getGrid()) + " * " + WeightedBlocksHFactor + " = " + (TetriminoSettings.computeWeightedBlocks(gameM.getGrid()) * WeightedBlocksHFactor));
           logGA("ClearableLines: " + TetriminoSettings.computeClearableLine(gameM.getGrid()) + " * " + ClearableLineHFactor + " = " + (TetriminoSettings.computeClearableLine(gameM.getGrid()) * ClearableLineHFactor));
           logGA("Roughness: " + TetriminoSettings.computeRoughness(gameM.getGrid()) + " * " + RoughnessHFactor + " = " + (TetriminoSettings.computeRoughness(gameM.getGrid()) * RoughnessHFactor));
           logGA("ColHoles: " + TetriminoSettings.computeColHoles(gameM.getGrid()) + " * " + ColHolesHFactor + " = " + (TetriminoSettings.computeColHoles(gameM.getGrid()) * ColHolesHFactor));
           logGA("ConnectedHoles: " + TetriminoSettings.computeConnectedHoles(gameM.getGrid()) + " * " + ConnectedHolesHFactor + " = " + (TetriminoSettings.computeConnectedHoles(gameM.getGrid()) * ConnectedHolesHFactor));
           logGA("BlockAboveHoles: " + TetriminoSettings.computeBlockAboveHoles(gameM.getGrid()) + " * " + BlockAboveHolesHFactor + " = " + (TetriminoSettings.computeBlockAboveHoles(gameM.getGrid()) * BlockAboveHolesHFactor));
           logGA("PitHolePercent: " + TetriminoSettings.computePitHolePercent(gameM.getGrid()) + " * " + PitHolePercentHFactor + " = " + (TetriminoSettings.computePitHolePercent(gameM.getGrid()) * PitHolePercentHFactor));
           logGA("DeepestWell: " + TetriminoSettings.computeDeepestWell(gameM.getGrid()) + " * " + DeepestWellHFactor + " = " + (TetriminoSettings.computeDeepestWell(gameM.getGrid()) * DeepestWellHFactor));
           logGA("getHeuristicScore: " + (
                gameM.getHeuristicScore(
                    BlocksHFactor,
                    WeightedBlocksHFactor,
                    ClearableLineHFactor,
                    RoughnessHFactor,
                    ColHolesHFactor,
                    ConnectedHolesHFactor,
                    BlockAboveHolesHFactor,
                    PitHolePercentHFactor,
                    DeepestWellHFactor
                ) + " * " + generalHeuristicFactor + " = " + (gameM.getHeuristicScore(
                    BlocksHFactor,
                    WeightedBlocksHFactor,
                    ClearableLineHFactor,
                    RoughnessHFactor,
                    ColHolesHFactor,
                    ConnectedHolesHFactor,
                    BlockAboveHolesHFactor,
                    PitHolePercentHFactor,
                    DeepestWellHFactor
                ) * generalHeuristicFactor
            )));
           logGA("Total: " + (
                penalization * penalizationFactor +
                gameM.getScore() * gameScoreFactor +
                gameM.getHeuristicScore(
                    BlocksHFactor,
                    WeightedBlocksHFactor,
                    ClearableLineHFactor,
                    RoughnessHFactor,
                    ColHolesHFactor,
                    ConnectedHolesHFactor,
                    BlockAboveHolesHFactor,
                    PitHolePercentHFactor,
                    DeepestWellHFactor
                ) * generalHeuristicFactor
            ));
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
                BlockAboveHolesHFactor,
                PitHolePercentHFactor,
                DeepestWellHFactor
            ) * generalHeuristicFactor
        );
    }

    TetriminoEnum[,] getPlayedState(Genotype genotype) {
        GameManager gameM = new GameManager();
        for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
            // For each movement in that piece
            for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                playMovement(gameM, genotype.movement[pieceI, moveJ], moveJ, aleoType);
            }
            gameM.lockPiece();
        }

        return gameM.getGrid();
    }


    // ========================================================
    //                      PLAY VISUALY
    // ========================================================
    IEnumerator playGenotype(Genotype genotype) {
        GameManager gameM = new GameManager();
        gameM.resetGame(bagQueueSaved, currentState);
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
    void startPoblation() {
        generationI = 0;
        scores = new float[initialPoblation];
        poblation = new Genotype[initialPoblation];
        for (int i = 0; i < initialPoblation; i++) {
            poblation[i] = new Genotype(aleoType, nPieces);
        }
        generatePlayingBags();

    }
    void updateGeneration() {
        // Reproduce new poblation with all the members, yet replace the worst half
        float[] probs = computeSoftMax(
            sortedIdxs.Select(i => scores[i]).ToArray()
        );
        int[] half = sortedIdxs[..(initialPoblation / 2)];

        Genotype[] newPoblation = new Genotype[initialPoblation];
        float[] newScores = new float[initialPoblation];

        // Keep the first half of the best (copy individuals and their scores)
        for (int i = 0; i < half.Length; i++) {
            newPoblation[i] = poblation[half[i]];
            newScores[i] = scores[half[i]];
        }

        for (int i = half.Length; i < initialPoblation; i+=2) {
        // for (int i = half.Length; i < initialPoblation; i++) {
            Genotype parent1 = getRandomGenotype(sortedIdxs, probs);
            Genotype parent2 = getRandomGenotype(sortedIdxs, probs);
            newPoblation[i] = parent1.reproduce(parent2, mutationChance);
            newPoblation[i] = parent1.reproduce(null, mutationChance);
            newScores[i] = 0f;

            if (i+1 < initialPoblation) // not exeding array
                newPoblation[i+ 1] = parent2.reproduce(parent1, mutationChance);
                newScores[i + 1] = 0f;
        }

        poblation = newPoblation;
        scores = newScores;
        generationI++;
    }

    void SortPopulation() {
        sortedIdxs = Enumerable.Range(0, scores.Length).ToArray();
        Array.Sort(sortedIdxs, (a, b) => scores[b].CompareTo(scores[a]));

       logGA($"Generation: {generationI}. Score: {scores[sortedIdxs[showingIndex]]}");
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    private void generatePlayingBags() {
        // Generate enough bags to ensure we never run out during evaluation
        // Each piece potentially needs multiple movements, so be generous
        int bagsNeeded = Math.Max(3, (nPieces + 6) / 7); // At least 3 bags, or enough for nPieces
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag(bagsNeeded));

        // Bag pieces log
        string bagPieces = "Bag pieces: ";
        foreach (var piece in bagQueueSaved) {
            bagPieces += piece.ToString() + " ";
        }
        logGA(bagPieces);
    }
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
