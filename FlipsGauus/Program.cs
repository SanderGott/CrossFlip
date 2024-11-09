using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class Board
{
    private int[,] board;
    public int level;
    public List<Tuple<int, int>> path; // To track the flip path

    public Board(string boardInput, int l)
    {
        InitializeBoard(boardInput);
        level = l;
        path = new List<Tuple<int, int>>();
    }

    private void InitializeBoard(string boardInput)
    {
        // Split the input string by commas to get rows
        string[] rows = boardInput.Split(',');

        // Get the number of rows and columns based on the input
        int rowCount = rows.Length;
        int columnCount = rows[0].Length;

        // Initialize the 2D array with the determined size
        board = new int[rowCount, columnCount];

        // Populate the 2D array
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                // Parse each character as an integer
                board[i, j] = rows[i][j] - '0';
            }
        }
    }

    private int GetBinPos(int row, int column)
    {
        return row * board.GetLength(1) + column;
    }



    private bool IsWall(int row, int column)
    {
        return row < 0 || row >= board.GetLength(0) || column < 0 || column >= board.GetLength(1) || board[row, column] == 2;
    }

    public bool[] GetVector(int row, int column)
    {
        bool[] vector = new bool[board.GetLength(0) * board.GetLength(1)];
        
        if (board[row, column] == 2)
        {
            return vector;
        }

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = false;
        }
        
        // up
        int curr = row - 1;
        while (!IsWall(curr, column))
        {
            vector[GetBinPos(curr, column)] = true;
            curr--;
        }

        // down
        curr = row + 1;
        while (!IsWall(curr, column))
        {
            vector[GetBinPos(curr, column)] = true;
            curr++;
        }

        // left
        curr = column - 1;
        while (!IsWall(row, curr))
        {
            vector[GetBinPos(row, curr)] = true;
            curr--;
        }

        // right
        curr = column + 1;
        while (!IsWall(row, curr))
        {
            vector[GetBinPos(row, curr)] = true;
            curr++;
        }

        vector[GetBinPos(row, column)] = true;

        return vector;
    }

    public bool[,] GetMatrix()
    {
        int n = board.GetLength(0) * board.GetLength(1);
        bool[,] matrix = new bool[n, n];

        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] == 2)
                {
                    continue;
                }

                bool[] vector = GetVector(i, j);

                for (int k = 0; k < vector.Length; k++)
                {
                    matrix[GetBinPos(i, j), k] = vector[k];
                }
            }
        }

        return matrix;
    }

    public bool[] GetRHS()
    {
        int n = board.GetLength(0) * board.GetLength(1);
        bool[] rhs = new bool[n];

        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                
                if (board[i, j] == 2)
                {
                    rhs[GetBinPos(i, j)] = false;
                }
                else if(board[i, j] == 1)
                {
                    rhs[GetBinPos(i, j)] = true;
                }
                else
                {
                    rhs[GetBinPos(i, j)] = false;
                }
            }
        }

        return rhs;
    }

}

class Program
{
    static void Main()
    {
        string filePath = "/../../../../../puzzle.json";

        string executablePath = Assembly.GetExecutingAssembly().Location;

        filePath = executablePath + filePath;


        try
        {
            // Load the JSON file
            if (!File.Exists(filePath))
            {
                Console.WriteLine("The file puzzle.json was not found.");
                return;
            }

            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<int, Dictionary<string, object>>>(json);

            if (data == null || data.Count == 0)
            {
                Console.WriteLine("The JSON file puzzle.json contains no levels.");
                return;
            }

            // Find the latest level (highest ID)
            int latestLevel = data.Keys.Max();
            //int latestLevel = 135;
            var puzzleData = data[latestLevel];

            if (!puzzleData.TryGetValue("boardStr", out object boardStrObj) || boardStrObj == null)
            {
                Console.WriteLine($"Level {latestLevel} is missing the boardStr key.");
                return;
            }

            string boardStr = boardStrObj.ToString();
            Console.WriteLine($"Starting level {latestLevel}");
            //Console.WriteLine(boardStr);

            var board = new Board(boardStr, latestLevel);
            
            var vec = board.GetMatrix();

            /*
            for (int i = 0; i < vec.GetLength(0); i++)
            {
                for (int j = 0; j < vec.GetLength(1); j++)
                {
                    Console.Write(vec[i, j] + " ");
                }
                Console.WriteLine();
            }
            */

            var rhs = board.GetRHS();

            // start timer
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var augmentedMatrix = BuildAugmentedMatrix(vec, rhs);
            GaussJordanEliminationMod2(augmentedMatrix);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine($"Time taken: {elapsedMs} ms");

            // Display the result
            string solutionString = "";
            for (int i = 0; i < augmentedMatrix.GetLength(0); i++)
            {
                //solutionString += augmentedMatrix[i, augmentedMatrix.GetLength(1) - 1] ? "1" : "0";
                solutionString += augmentedMatrix[i, augmentedMatrix.GetLength(1) - 1] & 1UL;
            }
            //Console.WriteLine("\n\nSolution (in mod 2):\n");
            //Console.WriteLine(solutionString);

            puzzleData["solution"] = solutionString;

            // Serialize the updated data
            string updatedJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, updatedJson);

            Console.WriteLine("Solution saved successfully.");


        
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }

    static ulong[,] BuildAugmentedMatrix(bool[,] matrix, bool[] rhs)
    {
        int rows = matrix.GetLength(0);
        int boolCols = matrix.GetLength(1);
        int ulongCols = (boolCols + 64) / 64; // Number of `ulong`s needed per row
        
        // Augmented matrix where each row is represented by multiple `ulong`s
        ulong[,] augmentedMatrix = new ulong[rows, ulongCols + 1];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < boolCols; j++)
            {
                if (matrix[i, j])
                {
                    int ulongIndex = j / 64;       // Determine which `ulong` in the row
                    int bitPosition = j % 64;      // Determine bit position within the `ulong`
                    augmentedMatrix[i, ulongIndex] |= (1UL << bitPosition); // Set the bit
                }
            }

            // Add the RHS value as the last `ulong` in each row
            if (rhs[i])
            {
                augmentedMatrix[i, ulongCols] |= 1UL; // Set the first bit in the last `ulong`
            }
        }

        return augmentedMatrix;
    }

    static void GaussJordanEliminationMod2(ulong[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int ulongCols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            // Find the pivot column for row `i`
            int pivotUlongIndex = i / 64;  // Determine which `ulong` contains the pivot bit
            int pivotBitPosition = i % 64; // Determine the bit position within that `ulong`

            // Ensure the pivot is true (find a row to swap if necessary)
            if ((matrix[i, pivotUlongIndex] & (1UL << pivotBitPosition)) == 0)
            {
                for (int k = i + 1; k < rows; k++)
                {
                    if ((matrix[k, pivotUlongIndex] & (1UL << pivotBitPosition)) != 0)
                    {
                        // Swap rows using XOR (bitwise addition in Boolean space)
                        for (int j = 0; j < ulongCols; j++)
                        {
                            matrix[i, j] ^= matrix[k, j];
                        }
                        break;
                    }
                }
            }

            // Make all elements in the current column except the pivot zero
            for (int k = 0; k < rows; k++)
            {
                if (k != i)
                {
                    // Check if the k-th row has a `1` in the pivot column
                    if ((matrix[k, pivotUlongIndex] & (1UL << pivotBitPosition)) != 0)
                    {
                        // Eliminate the k-th row's pivot bit by XOR with the i-th row
                        for (int j = 0; j < ulongCols; j++)
                        {
                            matrix[k, j] ^= matrix[i, j];
                        }
                    }
                }
            }
        }
    }

    /*
    static bool[,] BuildAugmentedMatrix(bool[,] matrix, bool[] rhs)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1) + 1;
        var augmentedMatrix = new bool[rows, cols];


        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                augmentedMatrix[i, j] = matrix[i, j]; // Copy matrix elements
            }
            augmentedMatrix[i, cols - 1] = rhs[i]; // Add RHS as last column
        }

        return augmentedMatrix;
    }
    */

    /*
    static void GaussJordanEliminationMod2(bool[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            // Ensure the pivot is true (find a row to swap if necessary)
            if (!matrix[i, i])
            {
                for (int k = i + 1; k < rows; k++)
                {
                    if (matrix[k, i])
                    {
                        // Swap rows using XOR (bitwise addition in Boolean space)
                        for (int j = 0; j < cols; j++)
                        {
                            matrix[i, j] ^= matrix[k, j];
                        }
                        break;
                    }
                }
            }

            // Make all elements in the current column except the pivot zero
            for (int k = 0; k < rows; k++)
            {
                if (k != i && matrix[k, i])
                {
                    for (int j = 0; j < cols; j++)
                    {
                        matrix[k, j] ^= matrix[i, j]; // XOR to zero out non-pivot elements
                    }
                }
            }
        }
    }
    */
}
