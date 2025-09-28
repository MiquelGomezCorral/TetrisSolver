using System.Collections.Generic;

public class GameManager {
    // ========================================================
    //                          Managers
    // ========================================================
    private GridManager gridM; // Do to look for it every time

    // ========================================================
    //                          PIECES
    // ========================================================
    private Tetrimino currentPiece;
    private TetriminoEnum currentPieceType = TetriminoEnum.X;
    private TetriminoEnum swapPieceType = TetriminoEnum.X;

    private int score;

    // is okey to be empty, fist call will add 7, then take 1 so 6 and in the
    // next call will add 7 more so 13. No need to initialize here.
    public Queue<TetriminoEnum> bagQueue = new Queue<TetriminoEnum>();
    // ========================================================
    //                          CONSTRUCTOR
    // ========================================================
    public GameManager() {
        gridM = new GridManager(TetriminoSettings.width, TetriminoSettings.height);

        // Spawn piece
        currentPieceType = getNewRandomPiece(false);
        currentPiece = new Tetrimino(gridM, currentPieceType);
    }

    // ========================================================
    //                          MOVEMENTS
    // ========================================================
    public bool moveCurrentPieceSide(DirectionEnum direction) {
        return currentPiece.movePieze(direction);
    }
    public bool rotateCurrentPiece(RorateEnum direction) {
        return currentPiece.rotatePiece(direction);
    }
    public bool moveCurrentPieceBootom() {
        return currentPiece.movePieceBootom();
    }

    public void lockPiece() {
        // Put piece down
        moveCurrentPieceBootom();
        // Then lock it
        score += gridM.lockPiece(getPiecePositions(), currentPieceType, currentPiece.lastAction);
        getNewRandomPiece();
    }


    // Maybe more than one movement
    public void moveLeft() {
        moveCurrentPieceSide(DirectionEnum.LEFT);
    }
    public void moveUp() {
        moveCurrentPieceSide(DirectionEnum.UP);
    }
    public void moveRight() {
        moveCurrentPieceSide(DirectionEnum.RIGHT);
    }
    public void moveDown() {
        moveCurrentPieceSide(DirectionEnum.DOWN);
    }


    public void rotateAClock() {
        rotateCurrentPiece(RorateEnum.ACLOCK);
    }
    public void rotateClock() {
        rotateCurrentPiece(RorateEnum.CLOCK);
    }
    public void rotateR180() {
        rotateCurrentPiece(RorateEnum.R180);
    }

    // ========================================================
    //                          METHODS
    // ========================================================
    public void resetGame(Queue<TetriminoEnum> bagQueueSaved = null, TetriminoEnum[,] newGridState = null) {
        // Reset bag
        if (bagQueueSaved != null)
            bagQueue = new Queue<TetriminoEnum>(bagQueueSaved);
        else
            bagQueue.Clear();


        // Get get new piece
        getNewRandomPiece();
        swapPieceType = TetriminoEnum.X;

        // Reset grid
        gridM.resetGrid(newGridState);

        // Reset socre
        score = 0;
    }

    // ========================================================
    //                 BAGS AND PIECE MANAGEMENT
    // ========================================================
    public void swapCurrentPiece() {
        if (currentPieceType == TetriminoEnum.X && swapPieceType == TetriminoEnum.X)
            return;

        // If no piece get a new one
        if (swapPieceType == TetriminoEnum.X) {
            swapPieceType = getNewRandomPiece(false);
        }

        // Swap types
        TetriminoEnum auxPieceType = currentPieceType;
        currentPieceType = swapPieceType;
        swapPieceType = auxPieceType;

        currentPiece.resetPeace(currentPieceType);
    }


    public void fillRandomBag() {
        TetriminoEnum[] newBag = TetriminoSettings.produceRandomBag();

        // Enque new pieces
        foreach (TetriminoEnum newPiece in newBag)
            bagQueue.Enqueue(newPiece);
    }
    public TetriminoEnum getNewRandomPiece(bool assignToCurrent = true) {
        if (bagQueue.Count < 7)
            fillRandomBag();

        if (assignToCurrent) {
            currentPieceType = bagQueue.Dequeue();
            currentPiece.resetPeace(currentPieceType);
            return currentPieceType;
        } else {
            return bagQueue.Dequeue();
        }
    }
    public TetriminoEnum[] PeekNext(int n) {
        TetriminoEnum[] nextPieces = new TetriminoEnum[n];
        int i = 0;
        foreach (var piece in bagQueue) {
            if (i >= n) break;
            nextPieces[i] = piece;
            i++;
        }
        return nextPieces;
    }

    // ========================================================
    //                      VALUE ACCESS
    // ========================================================
    public TetriminoEnum[,] getGrid() {
        return gridM.getGrid();
    }
    public TetriminoEnum getSwapPieceType() {
        return swapPieceType;
    }
    public GridPos[] getPiecePositions() {
        return currentPiece.getAbsPositions();
    }
    public TetriminoEnum getPieceType() {
        return currentPieceType;
    }
    public int getScore() {
        return score;
    }
    // ========================================================
    //                      VALUE ACCESS
    // ========================================================
    public float getHeuristicScore(
        float BlocksHFactor = 1.0f,
        float WeightedBlocksHFactor = 1.0f,
        float ClearableLineHFactor = 1.0f,
        float RoughnessHFactor = 1.0f,
        float ColHolesHFactor = 1.0f,
        float ConnectedHolesHFactor = 1.0f,
        float BlockAboveHolesHFactor = 1.0f,
        float PitHolePercentHFactor = 1.0f,
        float DeepestWellHFactor = 1.0f
    ) {
        TetriminoEnum[,] grid = getGrid();
        return (
            TetriminoSettings.computeBlocks(grid) * BlocksHFactor +
            TetriminoSettings.computeWeightedBlocks(grid) * WeightedBlocksHFactor +
            TetriminoSettings.computeClearableLine(grid) * ClearableLineHFactor +
            TetriminoSettings.computeRoughness(grid) * RoughnessHFactor +
            TetriminoSettings.computeColHoles(grid) * ColHolesHFactor +
            TetriminoSettings.computeConnectedHoles(grid) * ConnectedHolesHFactor +
            TetriminoSettings.computeBlockAboveHoles(grid) * BlockAboveHolesHFactor +
            TetriminoSettings.computePitHolePercent(grid) * PitHolePercentHFactor +
            TetriminoSettings.computeDeepestWell(grid) * DeepestWellHFactor
        );
    }
}
