using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class SimulatedAnneling : MonoBehaviour{
    [Header("Algorithm Parameters")]
    [SerializeField] bool executeComputation = true;
    [SerializeField] bool logExecution = true;
    [SerializeField] AleoType aleoType = AleoType.SwapDoble;
    [SerializeField] int nPieces = 10;

    [SerializeField] int maxGenerations = 10000;
    [SerializeField] float timeDelay = 0.0f;
    [SerializeField] int showEvery = 1000;


    [Header("Scoring Parameters")]
    [SerializeField] float Temperature = 100;
    [SerializeField] float InitialTemperature = 100;
    [SerializeField] float updateTempFactor = 0.0005f;
    [SerializeField] int maxPatience = 50;
    [SerializeField] int patience = 0;
    [SerializeField] int tabuSize = 10000; 
    private HashSet<Genotype> tabuSet;
    private Queue<Genotype> tabuQueue; // To track insertion order for removal
    [SerializeField] float penalizationFactor = 1.0f;
    [SerializeField] float gameScoreFactor = 2.5f;
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
    

    // ============= SA algorithm state =============
    public Genotype bestGenotype;
    public float score = float.MinValue; // minus infinity
    public int[][] allMovements;
    public int movementIndex;
    public int possibleMovements;
    public int generationI;
    Queue<TetriminoEnum> bagQueueSaved;
    TetriminoEnum[,] currentState;
    // ============= Logging =============
    FileLogger fileLogger;

    // ============= For parallel processing =============
    // Batch size for work distribution
    private const int MIN_BATCH_SIZE = 50; // Minimum work per thread to avoid overhead
    public int threadCount;
    public bool finished = false;
    private bool executing = false;
    private bool simulating = false;

    private ThreadLocal<GameManager> threadLocalGameManager = new ThreadLocal<GameManager>(() => new GameManager());
    private ParallelOptions parallelOptions;

    // ============= Visualizaton and random =============
    private GridViewer gridV; // Do to look for it every time
    private System.Random rnd = new System.Random(TetriminoSettings.seed);

    // ========================================================
    //                          START
    // ========================================================
    void Start(){
        Initialize(maxGenerations);
    }

    public void Initialize(int maxGenerations, Genotype initialGenotype = null) {
        if (!executeComputation){
            return;
        }
        if (logExecution){
            fileLogger = new FileLogger(
                $"SA_Log_{DateTime.Now:yyyyMMdd_HHmmss}" +
                $"-Aleo_{aleoType}" +
                $"-Pieces_{nPieces}" +
                $"InitialTemp_{InitialTemperature}" +
                $"MaxTemp_{Temperature}" +
                $"-Patience_{maxPatience}" +
                $"-Tabu_{tabuSize}"
            );
        }
        this.maxGenerations = maxGenerations;

        logSA($"Starting Simulated Annealing with {nPieces} pieces of type {aleoType} for a max of {maxGenerations} generations");
        // ============= Grid viewer to show results ============= 
        gridV = FindFirstObjectByType<GridViewer>();
        if (gridV == null) {
            Debug.LogWarning("OptimizerManager: No GridViewer found in scene. UI preview will be disabled.");
        }
        // ============= Initialize GA variables ============= 
        logSA("Initial genotype");
        startPoblation(initialGenotype);

        // ============= Setup parallel processing ============= 
        // Use fewer threads if population is small
        // int maxUsefulThreads = Math.Max(initialPoblation / MIN_BATCH_SIZE, 1);
        //threadCount = Math.Min(Math.Max(Environment.ProcessorCount - 4, 1), maxUsefulThreads);
        // threadCount = 1;

        // logSA($"Using {threadCount} threads for population of {initialPoblation}");

        // Setup parallel options
        parallelOptions = new ParallelOptions {
            MaxDegreeOfParallelism = 1
        };


        // ============= START ============= 
        // Evaluate the initial genotype to have a starting score
        executing = true;
        Task.Run(() => {
            score = EvaluateSample(bestGenotype);
            executing = false;
        });
    }


    // remove threads on destroy
    void OnDestroy(){
        threadLocalGameManager?.Dispose();
        logSA("SA Manager destroyed.");
        logSA($"Best score {score} Genotype:\n{bestGenotype}");
    }
    void logSA(string message) {
        if (logExecution && fileLogger != null){
            fileLogger.Log(message);
        }
       Debug.Log(message);
    }
    
    // ========================================================
    //                          UPDATE
    // ========================================================
    void Update(){
        if (generationI >= maxGenerations){
            finished = true;
        }
        if (executing || simulating) return;
        // =========================== PLAY MOVEMENT ========================
        if (!simulating && generationI % showEvery == 0 && generationI != 0) {
            logSA($"================== PALYING ===================\n Score {score}:");
            simulating = true;

            // EvaluateGenotype(bestGenotype new GameManager(), true);
            StartCoroutine(playGenotype(bestGenotype));
        }

        // =========================== GA ========================
        if (generationI >= maxGenerations) {
            logSA($"================== FINISHED ===================\n Score {score}:");
            currentState = getPlayedState(bestGenotype);
            startPoblation();

            // Re-evaluate the initial genotype to have a starting score
            executing = true;
            Task.Run(() => {
                score = EvaluateSample(bestGenotype);
                executing = false;
            });

            return;
        }

        executing = true;
        Task.Run(() => {
            SAStep();
            executing = false;
        });
       
        //threadCount = 1;
    }

   
    private void SAStep() {
        // =========================== GET NEIGHBOR ========================
        Genotype neighbor = generateNeighbor(bestGenotype);
        
        // =========================== SORT BEST ========================
        float neighborScore = EvaluateSample(neighbor);

        // =========================== UPDATE GENERATION ========================
        bool update = updateGeneration(neighbor, neighborScore);

        // =========================== UPDATE TEMP ========================
        patience = update ? 0 : patience + 1;

        if (patience < maxPatience){ // If we have patience, keep going down
            Temperature = Mathf.Max(0.1f, Temperature * (1 - updateTempFactor));
        }else{
            Temperature = Mathf.Max(0.1f, Temperature * (1 + updateTempFactor)); 
        }
    }

    // ========================================================
    //                          THREDING
    // ========================================================
    float EvaluateSample(Genotype genotype) {
        try{
            var localGameM = threadLocalGameManager.Value;
            return EvaluateGenotype(genotype, localGameM);
        }
        catch (Exception e){
            Debug.LogError("Evaluation failed: " + e.Message + "\n" + e.StackTrace);
        }
        return float.MinValue;
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
            logSA("penalization: " + penalization + " * " + penalizationFactor + " = " + (penalization * penalizationFactor));
            logSA("Score: " + gameM.getScore() + " * " + gameScoreFactor + " = " + (gameM.getScore() * gameScoreFactor));
            logSA("Blocks: " + TetriminoSettings.computeBlocks(gameM.getGrid()) + " * " + BlocksHFactor + " = " + (TetriminoSettings.computeBlocks(gameM.getGrid()) * BlocksHFactor));
            logSA("WeightedBlocks: " + TetriminoSettings.computeWeightedBlocks(gameM.getGrid()) + " * " + WeightedBlocksHFactor + " = " + (TetriminoSettings.computeWeightedBlocks(gameM.getGrid()) * WeightedBlocksHFactor));
            logSA("ClearableLines: " + TetriminoSettings.computeClearableLine(gameM.getGrid()) + " * " + ClearableLineHFactor + " = " + (TetriminoSettings.computeClearableLine(gameM.getGrid()) * ClearableLineHFactor));
            logSA("Roughness: " + TetriminoSettings.computeRoughness(gameM.getGrid()) + " * " + RoughnessHFactor + " = " + (TetriminoSettings.computeRoughness(gameM.getGrid()) * RoughnessHFactor));
            logSA("ColHoles: " + TetriminoSettings.computeColHoles(gameM.getGrid()) + " * " + ColHolesHFactor + " = " + (TetriminoSettings.computeColHoles(gameM.getGrid()) * ColHolesHFactor));
            logSA("ConnectedHoles: " + TetriminoSettings.computeConnectedHoles(gameM.getGrid()) + " * " + ConnectedHolesHFactor + " = " + (TetriminoSettings.computeConnectedHoles(gameM.getGrid()) * ConnectedHolesHFactor));
            logSA("BlockAboveHoles: " + TetriminoSettings.computeBlockAboveHoles(gameM.getGrid()) + " * " + BlockAboveHolesHFactor + " = " + (TetriminoSettings.computeBlockAboveHoles(gameM.getGrid()) * BlockAboveHolesHFactor));
            logSA("PitHolePercent: " + TetriminoSettings.computePitHolePercent(gameM.getGrid()) + " * " + PitHolePercentHFactor + " = " + (TetriminoSettings.computePitHolePercent(gameM.getGrid()) * PitHolePercentHFactor));
            logSA("DeepestWell: " + TetriminoSettings.computeDeepestWell(gameM.getGrid()) + " * " + DeepestWellHFactor + " = " + (TetriminoSettings.computeDeepestWell(gameM.getGrid()) * DeepestWellHFactor));
            logSA("getHeuristicScore: " + (
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
            logSA("Total: " + (
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
    //                            SA
    // ========================================================
    void startPoblation(Genotype initialGenotype = null) {
        generationI = 0;
        bestGenotype = initialGenotype ?? new Genotype(aleoType, nPieces);
        

        // ================= LIST OF MOVEMENTS =================
        allMovements = bestGenotype.generateAllMovements();
        possibleMovements = 0;
        for (int i = 0; i < allMovements.Length; i++){
            possibleMovements += allMovements[i].Length;
        }
        possibleMovements *= nPieces; // each piece can use any movement 
        movementIndex = rnd.Next(possibleMovements);
        logSA($"Possible movements: {possibleMovements} ({allMovements.Length} types for each {nPieces} pieces)");

        maxPatience = possibleMovements;
        tabuSize = possibleMovements * 4;
        // ================= TABU LIST =================
        tabuSet = new HashSet<Genotype>();
        tabuQueue = new Queue<Genotype>();
        AddToTabuList(bestGenotype);

        // ================= BAG OF PIECES =================
        generatePlayingBags();
    }
    
    bool updateGeneration(Genotype neighbor, float neighborScore){
        float deltaFitness = neighborScore - score ;
        float prob = Mathf.Min(1, Mathf.Exp(deltaFitness / Temperature));
        float random = (float)rnd.NextDouble();
        bool update = random < prob && deltaFitness != 0; // To avoid getting the same score

        if(update){ // we have selected one, so we need to change the movement
            // Add NEW genotype to tabu list (prevent revisiting)
            logSA($"Gen: {generationI} - Updated: {score} (temp: {Temperature}, delta: {deltaFitness}, prob: {prob}, rand: {random})");
            AddToTabuList(neighbor);
            
            bestGenotype = neighbor;
            score = neighborScore;

            movementIndex = rnd.Next(possibleMovements);
        }else{ // keep trying with the next movement
            logSA($"Gen: {generationI} - Rejected: {score} | {neighborScore} (temp: {Temperature}, delta: {deltaFitness}, prob: {prob}, rand: {random})");
            movementIndex = (movementIndex + 1) % possibleMovements;
        }

        generationI++;
        return update;
    }

    Genotype generateNeighbor(Genotype genotype){
        Genotype neighbor = getGenotypeFromIndex(genotype, movementIndex);
        while (tabuSet.Contains(neighbor)){
            movementIndex = (movementIndex + 1) % possibleMovements;
            neighbor = getGenotypeFromIndex(genotype, movementIndex);
        }

        return neighbor;
    }

    Genotype getGenotypeFromIndex(Genotype baseGenotype, int idx) {
        // Create a neighbor by mutating the current best genotype
        int pieceIndex = idx % nPieces;
        int moveTypeIndex = (idx / nPieces) % allMovements.Length;
        int valueIndex = idx / (nPieces * allMovements.Length);
        
        // Ensure valueIndex is within bounds
        valueIndex = valueIndex % allMovements[moveTypeIndex].Length;

        return baseGenotype.mutateAtCopy(
            pieceIndex,
            moveTypeIndex,
            allMovements[moveTypeIndex][valueIndex]
        );
    }

    // ========================================================
    //                      TABU LIST MANAGEMENT
    // ========================================================
    private void AddToTabuList(Genotype genotype) {
        // Add to both set (for fast lookup) and queue (for order tracking)
        tabuSet.Add(genotype);
        tabuQueue.Enqueue(genotype);
        
        // Remove oldest if size exceeded
        while (tabuQueue.Count > tabuSize) {
            Genotype oldest = tabuQueue.Dequeue();
            tabuSet.Remove(oldest);
        }
        
        // logSA($"Tabu List Size: {tabuSet.Count}");
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    private void generatePlayingBags() {
        // Generate enough bags to ensure we never run out during evaluation
        // Each piece potentially needs multiple movements, so be generous
        int bagsNeeded = Math.Max(3, (nPieces + 6) / 7); // At least 3 bags, or enough for nPieces
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag(bagsNeeded));
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

    public void updateGridViewer(GameManager gameM) {
        gridV.updateGrid(gameM.getGrid());
        gridV.updateGridPositions(
            gameM.getPiecePositions(),
            gameM.getPieceType()
        );
        gridV.updateSwapPiece(gameM.getSwapPieceType());
    }
}
