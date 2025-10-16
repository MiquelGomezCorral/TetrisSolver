
using System.Threading;

public enum AleoType {
    Simple, Double, SwapSimple, SwapDoble, None
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

    private static ThreadLocal<System.Random> rng = new ThreadLocal<System.Random>(() => new System.Random(TetriminoSettings.seed));
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
        return rng.Value.Next(movementInitialRagnes[0], movementInitialRagnes[1] + 1);
    }
    public int getRandomMovement() {
        return rng.Value.Next(movementRagnes[0], movementRagnes[1] + 1);
    }
    public int getRandomRotate() {
        return rng.Value.Next(rotateRanges[0], rotateRanges[1] + 1);
    }
    public int getRandomSwap() {
        return rng.Value.Next(swapRanges[0], swapRanges[1] + 1);
    }

    public bool[,] GetRandomBooleans(int n, int m, float probabilityYes = 0.5f){
        bool[,] matrix = new bool[n, m];
        for (int i = 0; i < n; i++)
        {
            // For each element, apply the probability
            // If selected, get a random index and mutate it
            if (rng.Value.NextDouble() < probabilityYes)
            {
                matrix[i, rng.Value.Next(0, m)] = true;
            }
        }
        return matrix;
    }

    public int[] getRangeList(int start, int end) {
        int[] res = new int[end - start + 1];
        for (int i = start; i <= end; i++) {
            res[i - start] = i;
        }
        return res;
    }
    
    public int[][] generateAllMovements(){
        int[][] allMovements = new int[movement.GetLength(1)][];
        int[] initMoves = getRangeList(movementInitialRagnes[0], movementInitialRagnes[1]);
        int[] moves     = getRangeList(movementRagnes[0], movementRagnes[1]);
        int[] rotations = getRangeList(rotateRanges[0], rotateRanges[1]);
        int[] swaps     = getRangeList(swapRanges[0], swapRanges[1]);
        switch (aleoType){
            case AleoType.Simple:
                allMovements[0] = initMoves;
                allMovements[1] = rotations;
                break;

            case AleoType.Double:
                allMovements[0] = initMoves;
                allMovements[1] = rotations;
                allMovements[2] = moves;
                allMovements[3] = rotations;
                break;

            case AleoType.SwapSimple:
                allMovements[0] = swaps;
                allMovements[1] = initMoves;
                allMovements[2] = rotations;
                break;
            case AleoType.SwapDoble:
                allMovements[0] = swaps;
                allMovements[1] = initMoves;
                allMovements[2] = rotations;
                allMovements[3] = moves;
                allMovements[4] = rotations;
                break;
        }
        
        return allMovements;
    }
    // ========================================================
    //                         GSA
    // ========================================================
    public Genotype reproduce(Genotype parent, float mutateChance) {
        Genotype kid = DeepCopy();

        if (parent != null) {
            // Copy half of the pieces movement alternating
            for (int i = movement.GetLength(0) * 4 / 5; i < movement.GetLength(0); i++) {
                for (int j = 0; j < movement.GetLength(1); j++) {
                    kid.movement[i, j] = parent.movement[i, j];
                }
            }
        }// if no parent just kid mutated

        kid.mutate(mutateChance);
        return kid;
    }

    public void mutate(float chance)
    {
        bool[,] mutates = GetRandomBooleans(movement.GetLength(0), movement.GetLength(1), chance);
        for (int i = 0; i < movement.GetLength(0); i++)
        {
            for (int pos = 0; pos < movement.GetLength(1); pos++)
            {
                if (!mutates[i, pos]) continue;

                if (pos == 0 && (aleoType == AleoType.SwapSimple || aleoType == AleoType.SwapDoble))
                {
                    // Swap: XOR -> stay the same if mutates is true, change if is false.
                    // Since we use a 1 it will always change
                    movement[i, pos] ^= 1;

                }
                else if (
                    (aleoType == AleoType.Simple && (pos == 0)) ||
                    (aleoType == AleoType.Double && (pos == 0 || pos == 3)) ||
                    (aleoType == AleoType.SwapSimple && (pos == 1)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 1 || pos == 4))
                )
                { // Rotatae: -1 or +1 if mutates, always in modulo
                    movement[i, pos] = (
                        movement[i, pos] +
                        (rng.Value.NextDouble() > 0.5f ? 1 : -1) +
                        rotateRangesModulo
                    ) % rotateRangesModulo;

                }
                else if (
                    (aleoType == AleoType.Simple && (pos == 1)) ||
                    (aleoType == AleoType.Double && (pos == 1)) ||
                    (aleoType == AleoType.SwapSimple && (pos == 2)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 2))
                )
                { // Move intitial

                    // JUST ONE LEFT OR RIGHT
                    //if (movement[i, pos] <= movementInitialRagnes[0]) {
                    //    movement[i, pos] =  movementInitialRagnes[0] + 1;  // move up from lower limit
                    //} else if (movement[i, pos] >= movementInitialRagnes[1]) {
                    //    movement[i, pos] = movementInitialRagnes[1] - 1;  // move down from upper limit
                    //} else {
                    //    movement[i, pos] += (rng.Value.NextDouble() > 0.5f ? 1 : -1);
                    //}

                    // GET ANY RANDOM VALUE
                    movement[i, pos] = rng.Value.Next(movementInitialRagnes[0], movementInitialRagnes[1] + 1);

                }
                else if (
                    (aleoType == AleoType.Double && (pos == 2)) ||
                    (aleoType == AleoType.SwapDoble && (pos == 3))
                )
                { // Move middle

                    // JUST ONE LEFT OR RIGHT
                    //if (movement[i, pos] <= movementRagnes[0]) {
                    //    movement[i, pos] = movementRagnes[0] + 1;  // move up from lower limit
                    //} else if (movement[i, pos] >= movementRagnes[1]) {
                    //    movement[i, pos] = movementRagnes[1] - 1;  // move down from upper limit
                    //} else {
                    //    movement[i, pos] += (rng.Value.NextDouble() > 0.5f ? 1 : -1);
                    //}

                    // GET ANY RANDOM VALUE
                    movement[i, pos] = rng.Value.Next(movementRagnes[0], movementRagnes[1] + 1);
                }
            }
        }
    }

    public Genotype mutateAtCopy(int pieceIndex, int moveTypeIndex, int newValue){
        Genotype kid = DeepCopy();
        kid.movement[pieceIndex, moveTypeIndex] = newValue;
        return kid;
    }

    // ========================================================
    //                       EQUALITY
    // ========================================================
    public override bool Equals(object obj) {
        if (obj == null || GetType() != obj.GetType()) return false;
        
        Genotype other = (Genotype)obj;
        
        // Quick checks first
        if (aleoType != other.aleoType || nPieces != other.nPieces) return false;
        if (movement.GetLength(0) != other.movement.GetLength(0) || 
            movement.GetLength(1) != other.movement.GetLength(1)) return false;
        
        // Compare movement arrays
        for (int i = 0; i < movement.GetLength(0); i++) {
            for (int j = 0; j < movement.GetLength(1); j++) {
                if (movement[i, j] != other.movement[i, j]) return false;
            }
        }
        
        return true;
    }

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = hash * 23 + aleoType.GetHashCode();
            hash = hash * 23 + nPieces.GetHashCode();
            
            // Include movement array in hash
            for (int i = 0; i < movement.GetLength(0); i++) {
                for (int j = 0; j < movement.GetLength(1); j++) {
                    hash = hash * 23 + movement[i, j].GetHashCode();
                }
            }
            
            return hash;
        }
    }

    public static bool operator ==(Genotype left, Genotype right) {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Genotype left, Genotype right) {
        return !(left == right);
    }

    // ========================================================
    //                       STRING REPRESENTATION
    // ========================================================
    public override string ToString() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Header with genotype info
        sb.AppendLine($"Genotype [{aleoType}] - {nPieces} pieces:");
        sb.AppendLine($"Movement Matrix [{movement.GetLength(0)}x{movement.GetLength(1)}]:");
        
        // Column headers (movement types)
        sb.Append("Piece |");
        for (int j = 0; j < movement.GetLength(1); j++) {
            sb.Append($" {GetMovementTypeName(j),8} |");
        }
        sb.AppendLine();
        
        // Separator line
        sb.Append("------|");
        for (int j = 0; j < movement.GetLength(1); j++) {
            sb.Append("----------|");
        }
        sb.AppendLine();
        
        // Movement data rows
        for (int i = 0; i < movement.GetLength(0); i++) {
            sb.Append($" {i,4} |");
            for (int j = 0; j < movement.GetLength(1); j++) {
                sb.Append($" {movement[i, j],8} |");
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    private string GetMovementTypeName(int position) {
        switch (aleoType) {
            case AleoType.Simple:
                return position switch {
                    0 => "Rotate",
                    1 => "Move",
                    _ => $"Pos{position}"
                };
            
            case AleoType.Double:
                return position switch {
                    0 => "Rotate1",
                    1 => "Move1",
                    2 => "Move2",
                    3 => "Rotate2",
                    _ => $"Pos{position}"
                };
            
            case AleoType.SwapSimple:
                return position switch {
                    0 => "Swap",
                    1 => "Rotate",
                    2 => "Move",
                    _ => $"Pos{position}"
                };
            
            case AleoType.SwapDoble:
                return position switch {
                    0 => "Swap",
                    1 => "Rotate1",
                    2 => "Move1",
                    3 => "Move2",
                    4 => "Rotate2",
                    _ => $"Pos{position}"
                };
            
            default:
                return $"Pos{position}";
        }
    }

}
