using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public double[] GetVector(int row, int column)
    {
        double[] vector = new double[board.GetLength(0) * board.GetLength(1)];
        
        if (board[row, column] == 2)
        {
            return vector;
        }
        
        // up
        int curr = row - 1;
        while (!IsWall(curr, column))
        {
            vector[GetBinPos(curr, column)] = 1;
            curr--;
        }

        // down
        curr = row + 1;
        while (!IsWall(curr, column))
        {
            vector[GetBinPos(curr, column)] = 1;
            curr++;
        }

        // left
        curr = column - 1;
        while (!IsWall(row, curr))
        {
            vector[GetBinPos(row, curr)] = 1;
            curr--;
        }

        // right
        curr = column + 1;
        while (!IsWall(row, curr))
        {
            vector[GetBinPos(row, curr)] = 1;
            curr++;
        }

        vector[GetBinPos(row, column)] = 1;

        return vector;
    }

    public double[,] GetMatrix()
    {
        int n = board.GetLength(0) * board.GetLength(1);
        double[,] matrix = new double[n, n];

        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] == 2)
                {
                    continue;
                }

                double[] vector = GetVector(i, j);

                for (int k = 0; k < vector.Length; k++)
                {
                    matrix[GetBinPos(i, j), k] = vector[k];
                }
            }
        }

        return matrix;
    }

    public double[] GetRHS()
    {
        int n = board.GetLength(0) * board.GetLength(1);
        double[] rhs = new double[n];

        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                
                if (board[i, j] == 2)
                {
                    rhs[GetBinPos(i, j)] = 0;
                }
                else if(board[i, j] == 1)
                {
                    rhs[GetBinPos(i, j)] = 1;
                }
                else
                {
                    rhs[GetBinPos(i, j)] = 0;
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
            //int latestLevel = 75;
            var puzzleData = data[latestLevel];

            if (!puzzleData.TryGetValue("boardStr", out object boardStrObj) || boardStrObj == null)
            {
                Console.WriteLine($"Level {latestLevel} is missing the boardStr key.");
                return;
            }

            string boardStr = boardStrObj.ToString();
            Console.WriteLine($"Starting level {latestLevel}");
            Console.WriteLine(boardStr);

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


            var augmentedMatrix = BuildAugmentedMatrix(vec, rhs);
            GaussJordanEliminationMod2(augmentedMatrix);

            // Display the result
            string solutionString = "";
            for (int i = 0; i < augmentedMatrix.RowCount; i++)
            {
                solutionString += augmentedMatrix[i, augmentedMatrix.ColumnCount - 1];
            }
            Console.WriteLine("\n\nSolution (in mod 2):\n");
            Console.WriteLine(solutionString);

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

        static Matrix<double> BuildAugmentedMatrix(double[,] matrix, double[] rhs)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1) + 1;
            var augmentedMatrix = Matrix<double>.Build.Dense(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    augmentedMatrix[i, j] = matrix[i, j] % 2; // Ensure mod 2
                }
                augmentedMatrix[i, cols - 1] = rhs[i] % 2; // Add RHS as last column
            }

            return augmentedMatrix;
        }

        static void GaussJordanEliminationMod2(Matrix<double> matrix)
        {
            int rows = matrix.RowCount;
            int cols = matrix.ColumnCount;

            for (int i = 0; i < rows; i++)
            {
                // Make sure the pivot is 1 (find a row to swap if necessary)
                if (matrix[i, i] == 0)
                {
                    for (int k = i + 1; k < rows; k++)
                    {
                        if (matrix[k, i] == 1)
                        {
                            matrix.SetRow(i, matrix.Row(k) + matrix.Row(i));
                            break;
                        }
                    }
                }

                // Normalize row to ensure the pivot is 1
                matrix[i, i] %= 2;

                // Make all elements in the current column except the pivot zero
                for (int k = 0; k < rows; k++)
                {
                    if (k != i && matrix[k, i] == 1)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            matrix[k, j] = (matrix[k, j] + matrix[i, j]) % 2; // Add rows mod 2
                        }
                    }
                }
            }
        }       
    }
