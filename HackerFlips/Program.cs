// See https://aka.ms/new-console-template for more information


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

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

    private bool IsWall(int row, int column)
    {
        return row < 0 || row >= board.GetLength(0) || column < 0 || column >= board.GetLength(1) || board[row, column] == 2;
    }

    private void FlipBlock(int row, int column)
    {
        if(board[row, column] == 0)
        {
            board[row, column] = 1;
        }
        else if(board[row, column] == 1)
        {
            board[row, column] = 0;
        }
    }

    private void FlipRow(int row, int column)
    {
        // Flip to left
        int curr = column;
        while(!IsWall(row, curr))
        {
            FlipBlock(row, curr);
            curr--;
        }

        // Flip to right
        curr = column + 1;
        while(!IsWall(row, curr))
        {
            FlipBlock(row, curr);
            curr++;
        }
    }

    private void FlipColumn(int row, int column)
    {
        // Flip up
        int curr = row;
        while(!IsWall(curr, column))
        {
            FlipBlock(curr, column);
            curr--;
        }

        // Flip down
        curr = row + 1;
        while(!IsWall(curr, column))
        {
            FlipBlock(curr, column);
            curr++;
        }
    }

    public void Flip(int row, int column)
    {
        if(board[row, column] == 2)
        {
            return;
        }
        FlipRow(row, column);
        FlipColumn(row, column);
        FlipBlock(row, column);

    }

    public void PrintBoard()
    {
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                // Change color based on the value
                switch (board[i, j])
                {
                    case 0:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("■ ");
                        break;
                    case 1:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("■ ");
                        break;
                    case 2:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("■ ");
                        break;
                }
            }
            Console.WriteLine();
        }

        // Reset color back to default
        Console.ResetColor();
        Console.WriteLine("---------------------");
    }

    public bool Solve()
    {
        return Solve(0, 0);
    }

    private bool Solve(int row, int col)
    {
        if (IsSolved())
        {
            PrintPath();
            return true;
        }

        if (row >= board.GetLength(0))
            return false;

        int nextRow = (col + 1 >= board.GetLength(1)) ? row + 1 : row;
        int nextCol = (col + 1) % board.GetLength(1);

        // Option 1: Don't flip this position, move to the next
        if (Solve(nextRow, nextCol))
            return true;

        // Option 2: Flip this position, track it in the path, then attempt to solve
        Flip(row, col);
        path.Add(new Tuple<int, int>(row, col)); // Add to path

        if (Solve(nextRow, nextCol))
            return true;

        // Backtrack: Undo the flip and remove from path
        Flip(row, col);
        path.RemoveAt(path.Count - 1);

        return false;
    }

    public void PrintPath()
    {
        Console.WriteLine("Path to solve:");
        foreach (var step in path)
        {
            Console.WriteLine($"Flip at row {step.Item1}, column {step.Item2}");
        }
    }

    public bool IsSolved()
    {
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if(board[i, j] == 1)
                {
                    return false;
                }
            }
        }
        return true;
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
            var puzzleData = data[latestLevel];

            if (!puzzleData.TryGetValue("boardStr", out object boardStrObj) || boardStrObj == null)
            {
                Console.WriteLine($"Level {latestLevel} is missing the boardStr key.");
                return;
            }

            string boardStr = boardStrObj.ToString();
            Console.WriteLine($"Starting level {latestLevel}");

            // Initialize the board with boardStr
            Board board = new Board(boardStr, latestLevel);
            //board.PrintBoard();
            board.Solve();
            //board.PrintBoard();
            //board.PrintPath();

            // Ensure "solution" exists and is a string
            if (!puzzleData.TryGetValue("solution", out object solutionObj) || solutionObj == null)
            {
                puzzleData["solution"] = string.Empty;
            }

            string[] rows = boardStr.Split(',');

            // Get the number of rows and columns based on the input
            int rowCount = rows.Length;
            int columnCount = rows[0].Length;

            // Create a character array filled with '0' for each cell in the grid
            char[] solutionArray = new char[rowCount * columnCount];
            for (int i = 0; i < solutionArray.Length; i++)
            {
                solutionArray[i] = '0';
            }

            // Update cells based on the path in `board.path`
            foreach (var step in board.path)
            {
                int pos = step.Item1 * columnCount + step.Item2;
                solutionArray[pos] = '1';
            }

            // Convert the character array to a string and update the solution in the data dictionary
            puzzleData["solution"] = new string(solutionArray);



            // Write the updated JSON data back to the file
            string updatedJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, updatedJson);

            Console.WriteLine("Solution saved successfully.");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("The file puzzle.json was not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}