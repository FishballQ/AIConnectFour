using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Board
{
  // Set up piece types
  enum Piece
  {
    Empty = 0,
    Blue = 1,
    Red = 2
  }

  private int numRows; // The number of rows of the game board

  public int NumRows {
    get { return numRows; }
  }

  private int numColumns; // The number of columns of the game board

  public int NumColumns {
    get { return numColumns; }
  }

  private int numPiecesToWin; // Game rules

  private bool allowDiagonally = true; // Allow win from the diagonal line

  protected int[,] board; // piece position on the board

  private bool isPlayersTurn; // Whose turn

  public bool IsPlayersTurn {
    get { return isPlayersTurn; }
  }

  private int piecesNumber = 0; // Game progress

  public int PiecesNumber {
    get { return piecesNumber; }
  }

  // Last move
  private int dropColumn;
  private int dropRow;

  public Awake ()
  {
    // Random first player
    isPlayersTurn = System.Convert.ToBoolean (UnityEngine.Random.Range (0, 2));

    board = new int[numColumns, numRows];
    for (int x = 0; x < numColumns; x++) {
      for (int y = 0; y < numRows; y++) {
        board [x, y] = (int)Piece.Empty;
      }
    }

    dropColumn = 0;
    dropRow = 0;
  }
  
  // Initialise board constructor and rules
  public Board (int numRows, int numColumns, int numPiecesToWin, bool allowDiagonally, 
                bool isPlayersTurn, int piecesNumber, int[,] board)
  {
    this.numRows = numRows;
    this.numColumns = numColumns;
    this.numPiecesToWin = numPiecesToWin;
    this.allowDiagonally = allowDiagonally;
    this.isPlayersTurn = isPlayersTurn;
    this.piecesNumber = piecesNumber;
    // Add all positions in the board
    this.board = new int[numColumns, numRows];
    for (int x = 0; x < numColumns; x++) {
      for (int y = 0; y < numRows; y++) {
        this.board [x, y] = board [x, y];
      }
    }
  }
  
  // Go through all available positions
  public Dictionary<int, int> GetPossibleCells ()
  {
    Dictionary<int, int> possibleCells = new Dictionary<int, int> ();
    for (int x = 0; x < numColumns; x++) {
      for (int y = numRows - 1; y >= 0; y--) {
        if (board [x, y] == (int)Piece.Empty) {
          possibleCells.Add (x, y);
          break;
        }
      }
    }
    return possibleCells;
  }

  // Go through current avalable drop column
  public List<int> GetPossibleDrops ()
  {
    List<int> possibleDrops = new List<int> ();
    for (int x = 0; x < numColumns; x++) {
      for (int y = numRows - 1; y >= 0; y--) {
        if (board [x, y] == (int)Piece.Empty) {
          possibleDrops.Add (x);
          break;
        }
      }
    }
    return possibleDrops;
  }
    
  // Get a random move among all possible moves
  public int GetRandomMove ()
  {
    List<int> moves = GetPossibleDrops (System.Random r);

    if (moves.Count > 0) {
      System.Random r = new System.Random ();
      return moves [r.Next(0, moves.Count)];
    }
    return -1;
  }

  // Drop a piece in column i, and get its row info to get the drop cell
  public int DropInColumn (int col)
  {
    for (int i = numRows - 1; i >= 0; i--) {
      if (board [col, i] == 0) {
        board [col, i] = isPlayersTurn ? (int)Piece.Blue : (int)Piece.Red;
        piecesNumber += 1;
        dropColumn = col;
        dropRow = i;
        return i;
      }
    }
    return -1;
  }

  // Change turn
  public void SwitchPlayer ()
  {
    isPlayersTurn = !isPlayersTurn;
  }

  // Who won
  public bool CheckForWinner ()
  {
    for (int x = 0; x < numColumns; x++) {
      for (int y = 0; y < numRows; y++) {
        // Player turn only include layermask Blue, AI turn is Red
        int layermask = isPlayersTurn ? (1 << 8) : (1 << 9);

        // If it is player turn, ignore red as first piece
        if (board [x, y] != (isPlayersTurn ? (int)Piece.Blue : (int)Piece.Red)) {
          continue;
        }
        // shoot a ray of length 'numPiecesToWin - 1' to the right to test horizontally
        RaycastHit[] hitsHorz = Physics.RaycastAll (new Vector3 (x, y * -1, 0), Vector3.right, 
                                                    numPiecesToWin - 1, layermask);
        // return true (won) if enough hits
        if (hitsHorz.Length == numPiecesToWin - 1) {
          return true;
        }
        // shoot a ray up to test vertically
        RaycastHit[] hitsVert = Physics.RaycastAll (new Vector3 (x, y * -1, 0), Vector3.up, 
                                                    numPiecesToWin - 1, layermask);
        if (hitsVert.Length == numPiecesToWin - 1) {
          return true;
        }

        // test diagonally
        if (allowDiagonally) {
          // calculate the length of the ray to shoot diagonally
          float length = Vector2.Distance (new Vector2 (0, 0), new Vector2 (numPiecesToWin - 1, 
                                           numPiecesToWin - 1));
          RaycastHit[] hitsDiaLeft = Physics.RaycastAll (new Vector3 (x, y * -1, 0), 
                                                         new Vector3 (-1, 1), length, layermask);

          if (hitsDiaLeft.Length == numPiecesToWin - 1) {
            return true;
          }
          RaycastHit[] hitsDiaRight = Physics.RaycastAll (new Vector3 (x, y * -1, 0), 
                                                          new Vector3 (1, 1), length, layermask);
          if (hitsDiaRight.Length == numPiecesToWin - 1) {
            return true;
          }
        }
      }
    }
    return false;
  }

  // Check if game over
  public bool CheckForVictory ()
  {
    int colour = board [dropColumn, dropRow];
    if (colour == 0) {
      return false;
    }
      
    bool bottomDirection = true;
    int currentAlignment = 1; //count current Piece

    //check vertical alignment
    for(int i = 1; i <= numPiecesToWin; i++) {
      if (bottomDirection && dropRow + i < NumRows) {
        if (board [dropColumn, dropRow + i] == colour)
          currentAlignment++;
        else
          bottomDirection = false;
      }

      if (currentAlignment >= numPiecesToWin)
        return true;
    }

    //reset check condition
    bool rightDirection = true;
    bool leftDirection = true;
    currentAlignment = 1;

    //check horizontal alignment
    for(int i = 1; i <= numPiecesToWin; i++) {
      if (rightDirection && dropColumn + i < numColumns) {
        if (board [dropColumn + i, dropRow] == colour)
          currentAlignment++;
        else
          rightDirection = false;
      }

      if (leftDirection && dropColumn - i >= 0) {
        if (board [dropColumn - i, dropRow] == colour)
          currentAlignment++;
        else
          leftDirection = false;
      }

      if (currentAlignment >= numPiecesToWin)
        return true;
    }

    //check diagonal alignment
    if (allowDiagonally) {

      bool upRightDirection = true;
      bool bottomLeftDirection = true;
      currentAlignment = 1;
      //check up right direction alignment
      for(int i = 1; i <= numPiecesToWin; i++) {
        if (upRightDirection && dropColumn + i < numColumns && dropRow + i < numRows) {
          if (board [dropColumn + i, dropRow + i] == colour)
            currentAlignment++;
          else
            upRightDirection = false;
        }
        //check bottom left direction alignment
        if (bottomLeftDirection && dropColumn - i >= 0 && dropRow - i >= 0 ) {
          if (board [dropColumn - i, dropRow - i] == colour)
            currentAlignment++;
          else
            bottomLeftDirection = false;
        }

        if (currentAlignment >= numPiecesToWin)
          return true;
      }

      bool upLeftDirection = true;
      bool bottomRightDirection = true;
      currentAlignment = 1;
      //check up left direction alignment
      for(int i = 1; i <= numPiecesToWin; i++) {
        if (upLeftDirection && dropColumn + i < numColumns && dropRow - i >= 0) {
          if (board [dropColumn + i, dropRow - i] == colour)
            currentAlignment++;
          else
            upLeftDirection = false;
        }
        //check bottom right direction alignment
        if (bottomRightDirection && dropColumn - i >= 0 && dropRow + i < numRows ) {
          if (board [dropColumn - i, dropRow + i] == colour)
            currentAlignment++;
          else
            bottomRightDirection = false;
        }

        if (currentAlignment >= numPiecesToWin)
          return true;
      }
    }

    return false;
  }

  // Check if there have available cells
  public bool ContainsEmptyCell ()
  {
    return (piecesNumber < numRows * numColumns);
  }

  // Clone the game current state
  public Board Clone ()
  {
    return new Board (numRows, numColumns, numPiecesToWin, allowDiagonally, isPlayersTurn, piecesNumber, board);
  }

}
