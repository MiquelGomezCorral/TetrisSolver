using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] int maxGenerations = 3000;
    [SerializeField] int maxPatience = 400;
    [SerializeField] float timeDelay = 0.0f;
    [SerializeField] int showingIndex = 0;
    [SerializeField] int showEvery = 25;


    [Header("Scoring Parameters")]
    [SerializeField] float softMaxTemp = 100;
    [SerializeField] float softMaxTempInitialTemp = 100;
    [SerializeField] float penalizationFactor = 1.0f;
    [SerializeField] float gameScoreFactor = 2.5f;
    [SerializeField] float generalHeuristicFactor = 1.0f;

    [Header("Heuristic Parameters")]
    [SerializeField] float BlocksHFactor = -1.0f;
    [SerializeField] float WeightedBlocksHFactor = -0.75f;
    [SerializeField] float ClearableLineHFactor = 1.0f;
    [SerializeField] float RoughnessHFactor = -1.0f;
    [SerializeField] float ColHolesHFactor = -5.0f;
    [SerializeField] float ConnectedHolesHFactor = -2.0f;
    [SerializeField] float BlockAboveHolesHFactor = -2.0f;
    [SerializeField] float PitHolePercentHFactor = -1.0f;
    [SerializeField] float DeepestWellHFactor = -1.0f;
    

    // ============= GA population =============
    public Genotype[] poblation;
    public float[] scores;
    public int[] sortedIdxs;
    public int generationI;
    public int patienceI = 0;
    public float lastBest = float.MinValue;
    Queue<TetriminoEnum> bagQueueSaved;
    TetriminoEnum[,] currentState;

    // ============= SA integration =============
    SimulatedAnneling saManager;
    public Genotype bestGenotype;
    // ============= Logging =============
    FileLogger fileLogger;

    // ============= EXPERIMENTS =============
    public int experimentI = 0;
    public AleoType[] aleoTypes = new AleoType[] {
        AleoType.Simple,
        AleoType.Double,
        AleoType.SwapSimple,
        AleoType.SwapDoble
    };
    public int[] poblationSizes = new int[] {
        10000, 20000, 30000
    };
    public float[] mutationChances = new float[] {
        0.05f, 0.15f, 0.25f
    };
    public int[] nPiecesOptions = new int[] {
        10, 20, 30
    };

    // ============= For single-threaded execution =============
    private bool simulating = false;
    private bool optimizingSA = false;
    private GameManager gameManager = new GameManager();

    // ============= Visualizaton and random =============
    private GridViewer gridV; // Do to look for it every time
    private System.Random rnd = new System.Random(TetriminoSettings.seed);

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
        startComputation();
    }

    
    void startComputation(){
        // ============= Initialize Configuration =============
        selectParametersFromList();

        // ============= Initialize logger =============
        initLogger();

        // ============= Initialize GA variables ============= 
        startPoblation();
        patienceI = 0;
        generationI = 0;
        lastBest = float.MinValue;

        // ============= START ============= 
        // Evaluate the first half of the genotypes to have some scores
        // Rest will be evaluated in the processNextGeneration  
        EvaluatePopulation(0, initialPoblation / 2);
    }

    void selectParametersFromList(){
        if (experimentI >= aleoTypes.Length * poblationSizes.Length * mutationChances.Length * nPiecesOptions.Length){
            logGA("All experiments completed.");
            return;
        }

        int totalCombinations = poblationSizes.Length * mutationChances.Length * nPiecesOptions.Length;
        int aleoTypeIndex = experimentI / totalCombinations;
        int remainder = experimentI % totalCombinations;

        int poblationSizeIndex = remainder / (mutationChances.Length * nPiecesOptions.Length);
        remainder = remainder % (mutationChances.Length * nPiecesOptions.Length);

        int mutationChanceIndex = remainder / nPiecesOptions.Length;
        int nPiecesIndex = remainder % nPiecesOptions.Length;

        // Set parameters for the next experiment
        aleoType = aleoTypes[aleoTypeIndex];
        initialPoblation = poblationSizes[poblationSizeIndex];
        mutationChance = mutationChances[mutationChanceIndex];
        nPieces = nPiecesOptions[nPiecesIndex];

        logGA($"Starting experiment {experimentI + 1}/{aleoTypes.Length * poblationSizes.Length * mutationChances.Length * nPiecesOptions.Length}: AleoType={aleoType}, PoblationSize={initialPoblation}, MutationChance={mutationChance}, NPieces={nPieces}");

        experimentI++;
    }
    
    void initLogger() {
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
        // =========================== LOCAL OPTIMIZATION ========================
        if (optimizingSA){
            if (saManager.finished){
                optimizingSA = false;
                bestGenotype = saManager.bestGenotype.DeepCopy();
                logGA($"SA finished after {saManager.generationI} generations. Best score {saManager.score} Genotype:\n{bestGenotype}");
                // Replace the worst genotype with the best from SA (use DeepCopy to avoid shared reference)
                poblation[sortedIdxs[sortedIdxs.Length-1]] = bestGenotype;
                Destroy(saManager);

                // Resort population insert
                SortPopulation();
            }
        }
        // =========================== Cooking... ========================
        if (simulating || optimizingSA) return;


        // =========================== GA NEXT EXPERIMENT ========================
        if (generationI >= maxGenerations) {
            logGA($"================== FINISHED ===================\n Score {scores[sortedIdxs[0]]}:");
            // currentState = getPlayedState(poblation[sortedIdxs[0]]);
            startComputation();

            return;
        }

        // =========================== GA NEXT STEP ========================
        GAStep();

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
    }

   
    private void GAStep() {
        // =========================== EVALUATE ========================
        EvaluatePopulation(initialPoblation / 2, initialPoblation);

        // =========================== SORT BEST ========================
        SortPopulation();

        // =========================== EARLY STOPPING ========================
        if (scores[sortedIdxs[0]] > lastBest) {
            lastBest = scores[sortedIdxs[0]];
            patienceI = 0;
        } else {
            patienceI++;
            if (patienceI >= maxPatience) {
                logGA($"Early stopping at generation {generationI} due to no improvement in {maxPatience} generations.");
                // currentState = getPlayedState(poblation[sortedIdxs[0]]);
                generationI = maxGenerations; // to trigger next experiment
            }
        }
        // =========================== UPDATE GENERATION ========================
        updateGeneration();
        generationI++;

        // =========================== UPDATE TEMP ========================
        softMaxTemp = Mathf.Max(0.1f, softMaxTempInitialTemp / Mathf.Log(generationI + 2));
        // softMaxTemp = Mathf.Max(0.1f, softMaxTempInitialTemp / generationI);


    }

    // ========================================================
    //                          EVALUATION
    // ========================================================
    void EvaluatePopulation(int startIdx, int endIdx) {
        for (int genIdx = startIdx; genIdx < endIdx; genIdx++) {
            scores[genIdx] = EvaluateGenotype(poblation[genIdx], gameManager);
        }
    }

    // ========================================================
    //                          EVALUATE
    // ========================================================
    float EvaluateGenotype(Genotype genotype, GameManager gameM, bool logHeuristics = false) {
        // Reset with the saved queue
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
        logGA($"Initial poblation size: {initialPoblation}");

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
            newScores[i] = 0f;

            if (i+1 < initialPoblation) { // not exeding array
                newPoblation[i+ 1] = parent2.reproduce(parent1, mutationChance);
                newScores[i + 1] = 0f;
            }
        }

        poblation = newPoblation;
        scores = newScores;
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
