using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
public class MovementPlayer : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] bool executeMovements = true;
    [SerializeField] int movementIndex = 0;
    [SerializeField] float timeDelay = 0.0f;

    AleoType[] aleoTypes = {
        AleoType.SwapDoble
    };
    Queue<TetriminoEnum>[] allPieces = {
        new Queue<TetriminoEnum>(new [] {
            TetriminoEnum.T,
            TetriminoEnum.I,
            TetriminoEnum.L,
            TetriminoEnum.Z,
            TetriminoEnum.J,
            TetriminoEnum.O,
            TetriminoEnum.S,
            TetriminoEnum.T,
            TetriminoEnum.Z,
            TetriminoEnum.L,
            TetriminoEnum.I,
            TetriminoEnum.O,
            TetriminoEnum.S,
            TetriminoEnum.J,
            TetriminoEnum.L,
            TetriminoEnum.O,
            TetriminoEnum.Z,
            TetriminoEnum.T,
            TetriminoEnum.I,
            TetriminoEnum.S,
            TetriminoEnum.J
        }),
    };
    int[][][] movements = {
        new int[][] {
            new int[] { 1, 2, -1, 3, 1 },
            new int[] { 1, 2, 2, -6, 1 },
            new int[] { 0, 3, 3, -4, 3 },
            new int[] { 0, 2, 4, 0, 2 },
            new int[] { 0, 1, -4, 8, 0 },
            new int[] { 0, 3, 2, 0, 0 },
            new int[] { 0, 0, 3, -3, 1 },
            new int[] { 1, 1, 5, -3, 1 },
            new int[] { 0, 1, 0, -3, 1 },
            new int[] { 0, 3, -3, 0, 0 },
        }
    };

    Queue<TetriminoEnum> bagQueueSaved;
    TetriminoEnum[,] currentState;
    private GridViewer gridV; // Do to look for it every time
    public bool simulating = false;
    void Start() {
        gridV = FindFirstObjectByType<GridViewer>();
        if (gridV == null) {
            Debug.LogWarning("OptimizerManager: No GridViewer found in scene. UI preview will be disabled.");
        }
    }

    // Update is called once per frame
    void Update() {
        if (!executeMovements || simulating) return;
        simulating = true;

        int[][] selectedMovement = movements[movementIndex % movements.Length];
        int[,] movement2D = ConvertToMultidimensional(selectedMovement);
        Genotype genotype = new Genotype(
            aleoTypes[movementIndex % aleoTypes.Length],
            selectedMovement.Length,
            movement2D
        );
        bagQueueSaved = allPieces[movementIndex % allPieces.Length];
        StartCoroutine(playGenotype(genotype));

    }

    int[,] ConvertToMultidimensional(int[][] jaggedArray) {
        int rows = jaggedArray.Length;
        int cols = jaggedArray[0].Length;
        int[,] result = new int[rows, cols];
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {
                result[i, j] = jaggedArray[i][j];
            }
        }
        return result;
    }
    private void generatePlayingBags(int nPieces) {
        // Generate enough bags to ensure we never run out during evaluation
        // Each piece potentially needs multiple movements, so be generous
        int bagsNeeded = Math.Max(3, (nPieces + 6) / 7); // At least 3 bags, or enough for nPieces
        bagQueueSaved = new Queue<TetriminoEnum>(TetriminoSettings.produceRandomBag(bagsNeeded));
    }

    IEnumerator playGenotype(Genotype genotype) {
        GameManager gameM = new GameManager();
        gameM.resetGame(new Queue<TetriminoEnum>(bagQueueSaved), currentState);
        gridV.resetGrid();

        // For each piece
        for (int pieceI = 0; pieceI < genotype.movement.GetLength(0); pieceI++) {
            // For each movement in that piece
            for (int moveJ = 0; moveJ < genotype.movement.GetLength(1); moveJ++) {
                yield return new WaitForSeconds(timeDelay);
                playMovement(gameM, genotype.movement[pieceI, moveJ], moveJ, genotype.aleoType);
            }
            gameM.lockPiece();

            updateGridViewer(gameM);
        }

        simulating = false;
        movementIndex++;
    }

    private int playMovement(GameManager gameM, int movement, int pos, AleoType aleoType) {
        // Penalize 1 point per each invalid movement
        int penalization = 0;
        if (pos == 0 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble)) {
            if (movement == 1) // no penalization for swap
                gameM.swapCurrentPiece();

        } else if (
            (aleoType == AleoType.Simple && (pos == 0)) ||
            (aleoType == AleoType.Double && (pos == 0 || pos == 3)) ||
            (aleoType == AleoType.SwapSimple && (pos == 1)) ||
            (aleoType == AleoType.SwapDoble && (pos == 1 || pos == 4))
        ) {
            // if the rotation is not possible, penalize
            penalization = gameM.rotateCurrentPiece((RorateEnum)movement) ? 0 : -1;

        } else if (
            (aleoType == AleoType.Simple && (pos == 1)) ||
            (aleoType == AleoType.Double && (pos == 1 || pos == 2)) ||
            (aleoType == AleoType.SwapSimple && (pos == 2)) ||
            (aleoType == AleoType.SwapDoble && (pos == 2 || pos == 3))
        ) {
            DirectionEnum direction = (movement >= 0) ? DirectionEnum.RIGHT : DirectionEnum.LEFT;
            int movementsLeft = Math.Abs(movement);
            while (movementsLeft > 0 && gameM.moveCurrentPieceSide(direction)) {
                movementsLeft--;
            }
            penalization = -movementsLeft; //penalize movements not done
        }


        // After the movement, move the piece down for the doubles
        if ((pos == 1 && aleoType == AleoType.Double) || (pos == 2 && aleoType == AleoType.SwapDoble)) {
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
