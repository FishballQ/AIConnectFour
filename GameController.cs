using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{

  [Range (3, 8)]
  public int numRows = 4;
  [Range (3, 8)]
  public int numColumns = 4;
  [Range (1, 8)]
  public int parallelProcesses = 2;
  [Range (7, 10000)]
  public int MCTS_Iterations = 1000;

  public int numPiecesToWin = 4;      // How many pieces have to be connected to win
  public bool allowDiagonally = true; // If allow diagonally connected Pieces
  public float dropTime = 4f;         // Piece dropped duration

  public GameObject pieceRed;
  public GameObject pieceBlue;
  public GameObject pieceBoard;

  public GameObject winningText;
  public GameObject winningBackground;
  public string playerWonText = "You Won!";
  public string playerLoseText = "You Lose!";
  public string drawText = "Draw!";

  public GameObject btnPlayAgain;
  public GameObject btn;
  bool btnPlayAgainTouching = false;
  Color btnPlayAgainOrigColor;
  Color btnPlayAgainHoverColor = new Color (255, 143, 4);
  Color btnHoverColor = new Color(212,196,183,0.5f);
  Color btnOrigColor = new Color(100,100,100,0.1f);

  GameObject gameObjectBoard;
  // temporary gameobject, holds the piece at mouse position until the mouse has clicked
  GameObject gameObjectTurn;
  // Game board: 0 = Empty, 1 = Blue, 2 = Red
  Board board;

  bool isLoading = true;
  bool isDropping = false;
  bool mouseButtonPressed = false;

  bool gameOver = false;
  bool isCheckingForWinner = false;

  // Use this for initialization
  void Start ()
  {
    int max = Mathf.Max (numRows, numColumns);

    if (numPiecesToWin > max)
      numPiecesToWin = max;

    CreateBoard ();

    btnPlayAgainOrigColor = btnPlayAgain.GetComponent<MeshRenderer> ().material.color;
  }

  // Creates the board.
  void CreateBoard ()
  {
    winningText.SetActive (false);
    winningBackground.SetActive (false);
    btnPlayAgain.SetActive (false);
    btn.SetActive (false);

    isLoading = true;

    gameObjectBoard = GameObject.Find ("Board");
    if (gameObjectBoard != null) {
      DestroyImmediate (gameObjectBoard);
    }
    gameObjectBoard = new GameObject ("Board");

    // create an empty board and instantiate the cells
    board = new Board (numRows, numColumns, numPiecesToWin, allowDiagonally);

    for (int x = 0; x < numColumns; x++) {
      for (int y = 0; y < numRows; y++) {
        GameObject g = Instantiate (pieceBoard, new Vector3 (x, y * -1, -1), Quaternion.identity) as GameObject;
        g.transform.parent = gameObjectBoard.transform;
      }
    }

    isLoading = false;
    gameOver = false;

    // center camera
    Camera.main.transform.position = new Vector3 (
      (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f), Camera.main.transform.position.z);

    winningText.transform.position = new Vector3 (
      (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f) + 1, winningText.transform.position.z);

    btnPlayAgain.transform.position = new Vector3 (
      (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f) - 1, btnPlayAgain.transform.position.z);
    btn.transform.position = new Vector3 (
      (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f) - 1, btnPlayAgain.transform.position.z);
  }

  // Spawns a piece at mouse position above the first row
  GameObject SpawnPiece ()
  {
    Vector3 spawnPos = Camera.main.ScreenToWorldPoint (Input.mousePosition);

    if (!board.IsPlayersTurn) {
      int column;
      // Inutile de lancer MCST le premier tour
      if (board.PiecesNumber != 0) {
        // One event is used for each MCTS.
        ManualResetEvent[] doneEvents = new ManualResetEvent[parallelProcesses];
        MonteCarloSearchTree[] trees = new MonteCarloSearchTree[parallelProcesses];

        for (int i = 0; i < parallelProcesses; i++) {
          doneEvents [i] = new ManualResetEvent (false);
          trees[i] = new MonteCarloSearchTree (board, doneEvents [i], MCTS_Iterations);
          ThreadPool.QueueUserWorkItem( new WaitCallback(ExpandTree), trees [i]);
        }
      
        WaitHandle.WaitAll(doneEvents);

        //regrouping all results
        Node rootNode = new Node ();

        for (int i = 0; i < parallelProcesses; i++) {

          var sortedChildren = (List<KeyValuePair<Node, int>>)trees [i].rootNode.children.ToList ();
          sortedChildren.Sort((pair1,pair2) => pair1.Value.CompareTo(pair2.Value));

          foreach (var child in sortedChildren) {

            if (!rootNode.children.ContainsValue (child.Value)) {
              Node rootChild = new Node ();
              rootChild.wins = child.Key.wins;
              rootChild.plays = child.Key.plays;
              rootNode.children.Add (rootChild, child.Value);
            } else {
              Node rootChild = rootNode.children.First( p => p.Value == child.Value ).Key;
              rootChild.wins += child.Key.wins;
              rootChild.plays += child.Key.plays;
            }
          }
        }

        column = rootNode.BestMove ();
      }
      else
        column = board.GetRandomMove ();
        
      spawnPos = new Vector3 (column, 0, 0);
    }

    GameObject g = Instantiate (
                  board.IsPlayersTurn ? pieceBlue : pieceRed, // is players turn = spawn blue, else spawn red
                  new Vector3 (
                    Mathf.Clamp (spawnPos.x, 0, numColumns - 1), 
                    gameObjectBoard.transform.position.y + 1, 0), // spawn it above the first row
                  Quaternion.identity) as GameObject;

    return g;
  }

  // Expands the tree, from root node
  public static void ExpandTree (System.Object t)
  {
    var tree = (MonteCarloSearchTree) t;
    tree.simulatedStateBoard = tree.currentStateBoard.Clone ();
    tree.rootNode = new Node (tree.simulatedStateBoard.IsPlayersTurn);

    Node selectedNode;
    Node expandedNode;
    System.Random r = new System.Random (System.Guid.NewGuid().GetHashCode());

    for (int i = 0; i < tree.nbIteration; i++) {
      // copie profonde
      tree.simulatedStateBoard = tree.currentStateBoard.Clone ();

      selectedNode = tree.rootNode.SelectNodeToExpand (tree.rootNode.plays, tree.simulatedStateBoard);
      expandedNode = selectedNode.Expand (tree.simulatedStateBoard, r);
      expandedNode.BackPropagate (expandedNode.Simulate (tree.simulatedStateBoard, r));
    }

    tree.doneEvent.Set ();
  }

  void BackButton ()
  {
    RaycastHit hit;
    //ray hitting out of the camera from where the mouse is
    Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
    
    if (Physics.Raycast (ray, out hit) && hit.collider.name == btnPlayAgain.name) {
      btnPlayAgain.GetComponent<MeshRenderer> ().material.color = btnPlayAgainHoverColor;
      btn.GetComponent<SpriteRenderer> ().color = btnHoverColor;
      //check if the left mouse has been pressed on this frame
      if (Input.GetMouseButtonDown (0) || Input.touchCount > 0 && btnPlayAgainTouching == false) {
        btnPlayAgainTouching = true;
        SceneManager.LoadScene(0);
      }
    } else {
      btnPlayAgain.GetComponent<MeshRenderer> ().material.color = btnPlayAgainOrigColor;
      btn.GetComponent<SpriteRenderer> ().color = btnOrigColor;
    }

    if (Input.touchCount == 0) {
      btnPlayAgainTouching = false;
    }
  }

  void Update ()
  {
    if (isLoading)
      return;

    if (isCheckingForWinner)
      return;

    if (gameOver) {
      winningText.SetActive (true);
      winningBackground.SetActive (true);
      btnPlayAgain.SetActive (true);
      btn.SetActive (true);
      BackButton ();

      return;
    }

    if (board.IsPlayersTurn) {
      if (gameObjectTurn == null) {
        gameObjectTurn = SpawnPiece ();
      } else {
        // update the objects position
        Vector3 pos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
        gameObjectTurn.transform.position = new Vector3 (
          Mathf.Clamp (pos.x, 0, numColumns - 1), 
          gameObjectBoard.transform.position.y + 1, 0);

        // click the left mouse button to drop the piece into the selected column
        if (Input.GetMouseButtonDown (0) && !mouseButtonPressed && !isDropping) {
          mouseButtonPressed = true;

          StartCoroutine (dropPiece (gameObjectTurn));
        } else {
          mouseButtonPressed = false;
        }
      }
    } else {
      if (gameObjectTurn == null) {
        gameObjectTurn = SpawnPiece ();
      } else {
        if (!isDropping)
          StartCoroutine (dropPiece (gameObjectTurn));
      }
    }
  }

  // Searching for a empty cell and let the object fall down into this cell
  IEnumerator dropPiece (GameObject gObject)
  {
    isDropping = true;

    Vector3 startPosition = gObject.transform.position;
    Vector3 endPosition = new Vector3 ();

    // round to a grid cell
    int x = Mathf.RoundToInt (startPosition.x);
    startPosition = new Vector3 (x, startPosition.y, startPosition.z);

    int y = board.DropInColumn (x);

    if (y != -1) {
      endPosition = new Vector3 (x, y * -1, startPosition.z);

      // Instantiate a new Piece, disable the temporary
      GameObject g = Instantiate (gObject) as GameObject;
      gameObjectTurn.GetComponent<Renderer> ().enabled = false;

      float distance = Vector3.Distance (startPosition, endPosition);

      float t = 0;
      while (t < 1) {
        t += Time.deltaTime * dropTime * ((numRows - distance) + 1);

        g.transform.position = Vector3.Lerp (startPosition, endPosition, t);
        yield return null;
      }

      g.transform.parent = gameObjectBoard.transform;

      // remove the temporary gameobject
      DestroyImmediate (gameObjectTurn);
      // run coroutine to check if someone has won
      StartCoroutine (Won ());
      // wait until winning check is done
      while (isCheckingForWinner)
        yield return null;

      board.SwitchPlayer ();
    }

    isDropping = false;

    yield return 0;
  }

  // Check for Winner
  IEnumerator Won ()
  {
    isCheckingForWinner = true;

    gameOver = board.CheckForWinner ();
    // if Game Over update the winning text to show who has won
    if (gameOver == true) {
      winningText.GetComponent<TextMesh> ().text = board.IsPlayersTurn ? playerWonText : playerLoseText;
    } else {
      // check if there are any empty cells left, if not set game over and update text to show a draw
      if (!board.ContainsEmptyCell ()) {
        gameOver = true;
        winningText.GetComponent<TextMesh> ().text = drawText;
      }
    }

    isCheckingForWinner = false;

    yield return 0;
  }
}
