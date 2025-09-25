
public enum AleoType {
    Simple, Double, SwapSimple, SwapDoble
}


public class Genotype {
    public static int[] movementInitialRagnes = { -5, 5 };
    public static int[] movementRagnes = { -9, 9 };
    public static int[] rotateRanges = { 0, 3 };
    public static int rotateRangesModulo = rotateRanges[1] + 1;
    public static int[] swapRanges = { 0, 1 };

    public int[,] movement;
    AleoType aleoType;
    int nPieces;

    // ========================================================
    //                       CONSTRUCTOR
    // ========================================================
    public Genotype(AleoType aleoTypeArg, int nPiecesArg, int[,] movementArg = null) {
        aleoType = aleoTypeArg;
        nPieces = nPiecesArg;

        if (movementArg != null) {
            movement = movementArg;
        } else {
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
    }

    public Genotype DeepCopy() {
        int[,] movementCpy = new int[movement.GetLength(0), movement.GetLength(1)];

        for (int r = 0; r < movement.GetLength(0); r++) {
            for (int c = 0; c < movement.GetLength(1); c++) {
                movementCpy[r, c] = movement[r, c];
            }
        }

        return new Genotype(aleoType, nPieces, movementCpy);
    }
    // ========================================================
    //                       METHODS
    // ========================================================
    public int getRandomMovementInitial() {
        return UnityEngine.Random.Range(movementInitialRagnes[0], movementInitialRagnes[1] + 1);
    }
    public int getRandomMovement() {
        return UnityEngine.Random.Range(movementRagnes[0], movementRagnes[1] + 1);
    }
    public int getRandomRotate() {
        return UnityEngine.Random.Range(rotateRanges[0], rotateRanges[1] + 1);
    }
    public int getRandomSwap() {
        return UnityEngine.Random.Range(swapRanges[0], swapRanges[1] + 1);
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
    // ========================================================
    //                         GSA
    // ========================================================
    public Genotype reproduce(Genotype parent, float mutateChance) {
        Genotype kid = DeepCopy();

        if (parent != null) {
            // Copy half of the pieces movement alternating
            for (int i = 0; i < movement.GetLength(0); i += 2) {
                for (int j = 0; j < movement.GetLength(1); j++) {
                    kid.movement[i, j] = parent.movement[i, j];
                }
            }
        }// if no parent just kid mutated

        kid.mutate(mutateChance);
        return kid;
    }

    public void mutate(float chance) {
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

                    // JUST ONE LEFT OR RIGHT
                    //if (movement[i, pos] <= movementInitialRagnes[0]) {
                    //    movement[i, pos] =  movementInitialRagnes[0] + 1;  // move up from lower limit
                    //} else if (movement[i, pos] >= movementInitialRagnes[1]) {
                    //    movement[i, pos] = movementInitialRagnes[1] - 1;  // move down from upper limit
                    //} else {
                    //    movement[i, pos] += (UnityEngine.Random.value > 0.5f ? 1 : -1);
                    //}

                    // GET ANY RANDOM VALUE
                    movement[i, pos] = UnityEngine.Random.Range(movementInitialRagnes[0], movementInitialRagnes[1] + 1);

                } else if (
                    (aleoType == AleoType.Double && (pos == 2)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 3))
                ) { // Move middle

                    // JUST ONE LEFT OR RIGHT
                    //if (movement[i, pos] <= movementRagnes[0]) {
                    //    movement[i, pos] = movementRagnes[0] + 1;  // move up from lower limit
                    //} else if (movement[i, pos] >= movementRagnes[1]) {
                    //    movement[i, pos] = movementRagnes[1] - 1;  // move down from upper limit
                    //} else {
                    //    movement[i, pos] += (UnityEngine.Random.value > 0.5f ? 1 : -1);
                    //}

                    // GET ANY RANDOM VALUE
                    movement[i, pos] = UnityEngine.Random.Range(movementRagnes[0], movementRagnes[1] + 1);
                }
            }
        }
    }

}
