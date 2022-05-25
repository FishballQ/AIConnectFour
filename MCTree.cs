using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


public class NodeEvaluator
{
  static double explorationParameter = Math.Sqrt (2);

  static double Evaluate (Node node)
  {
    return 0;
  }
}

public class Node
{
  public int wins { get; set; }           // win times from this node
  public int plays { get; set; }          // playing times from this node
  bool isPlayersTurn { get; set; }        // it's true when the player at the node

  Node parent { get; set; }               //parent node
  public Dictionary<Node, int> children;  //children node and its state

  // Initializes a new instance of the class.
  public Node (bool isPlayerTurn = false, Node parentNode = null)
  {
    wins = 0;
    plays = 0;
    this.isPlayersTurn = isPlayerTurn;
    children = new Dictionary<Node, int> ();
    parent = parentNode;
  }

  // Selects the best child from this node with UCB equation
  public Node BestChild (int nbSimulation)
  {
    double maxValue = -1;  // maximum one in all UCB values
    Node bestNode = null;  // best chird node
    float confident = 1.96;     // factor constant in the UCB equation

    foreach (Node child in children.Keys) {
      double nodeValue = (double)child.wins / (double)child.plays + Math.Sqrt (confident * 
                          Math.Log ((double)nbSimulation) / (double)child.plays);
      
      if (maxValue < nodeValue) {
        maxValue = nodeValue;
        bestNode = child;
      }
    }
    return bestNode;
  }

  // Select a node to expand in the current MCTS
  public Node SelectNodeToExpand (int nbSimulation, Board simulatedBoard)
  {
    //check for end game
    if (!simulatedBoard.ContainsEmptyCell () || simulatedBoard.CheckForVictory ()) {
      return this;
    }

    // If not all plays have been tried
    if (children.Keys.Count != simulatedBoard.GetPossibleDrops ().Count)
      return this;
    
    Node bestNode = null;
    bestNode = BestChild (nbSimulation);
    simulatedBoard.DropInColumn (children [bestNode]);
    simulatedBoard.SwitchPlayer ();
    return bestNode.SelectNodeToExpand (nbSimulation, simulatedBoard);
  }

  // Instantiate a child below the selected node and attach it to the tree.
  public Node Expand (Board simulatedBoard, System.Random r)
  {
    // If selected node is a leaf, return
    if (!simulatedBoard.ContainsEmptyCell () || simulatedBoard.CheckForVictory ())
      return this;
    // Copy of the possible plays list
    List<int> drops = new List<int> (simulatedBoard.GetPossibleDrops ());
    // For each available plays, remove the ones that have already been played.
    foreach (int column in children.Values) {
      if (drops.Contains (column))
        drops.Remove (column);
    }
    // Gets a line to play on.
    int colToPlay = drops [r.Next(0, drops.Count)];
    Node n = new Node (simulatedBoard.IsPlayersTurn, this);
    // Adds the child to the tree
    AddChild (n, colToPlay);
    simulatedBoard.DropInColumn (colToPlay);
    simulatedBoard.SwitchPlayer ();
    return n;
  }

  // Adds a child to the node.
  public void AddChild (Node node, int line)
  {
    children.Add (node, line);
  }

  // Gets the child action number.
  public int GetChildAction (Node node)
  {
    return children [node];
  }

  // Gets the children number.
  public int GetChildrenNumber ()
  {
    return children.Count;
  }

  // Simulate a game play based on the specified node selected by UCB.
  public bool Simulate (Board simulatedBoard, System.Random r)
  {
    if (simulatedBoard.CheckForVictory ()) {
      return !simulatedBoard.IsPlayersTurn;
    }
    while (simulatedBoard.ContainsEmptyCell ()) {
      int column = simulatedBoard.GetRandomMove (r);
      simulatedBoard.DropInColumn (column);

      if (simulatedBoard.CheckForVictory ()) {
        return simulatedBoard.IsPlayersTurn;
      }
      simulatedBoard.SwitchPlayer ();
    }
    return true;
  }

  // back propagation from this simulated game to the root
  public void BackPropagate (bool playersVictory)
  {
    plays++;  // current simulation plus 1
    if (isPlayersTurn == playersVictory) {
      wins++; // current victory plus 1
    }
    if (parent != null) {
      parent.BackPropagate (playersVictory);
    }
  }

  // Select the best node to move. The selected move column number
  public int BestMove ()
  {
    double maxValue = -1;
    int bestMove = -1;
    foreach (var child in children) {
      if ((double)child.Key.wins/(double)child.Key.plays > maxValue) {
        bestMove = child.Value;
        maxValue = (double)child.Key.wins/(double)child.Key.plays;
      }
    }
    return bestMove;
  }
}

// for call directly
public class MonteCarloSearchTree
{
  // correspond current state of the game
  public readonly Board currentStateBoard;
  // game state for simulation
  public Board simulatedStateBoard;
  public int nbIteration;
  //threadpool attribute (needed for parallel processing)
  public ManualResetEvent doneEvent;
  public Node rootNode;

  public MonteCarloSearchTree(Board board, ManualResetEvent _doneEvent, int _nbIteration)
  {
    doneEvent = _doneEvent;
    currentStateBoard = board;
    this.nbIteration = _nbIteration;
  }
}

