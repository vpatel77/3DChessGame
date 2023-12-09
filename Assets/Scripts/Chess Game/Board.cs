using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Requires a SquareSelectorCreator component on the same GameObject
[RequireComponent(typeof(SquareSelectorCreator))]
public class Board : MonoBehaviour
{
    public const int BOARD_SIZE = 8; // Constant for the size of the chess board

    [SerializeField] private Transform bottomLeftSquareTransform; // Transform of the bottom left square of the board
    [SerializeField] private float squareSize; // Size of each square on the chess board

    private Piece[,] grid; // 2D array representing the chess board grid
    private Piece selectedPiece; // Currently selected chess piece
    private ChessGameController chessController; // Reference to the chess game controller
    private SquareSelectorCreator squareSelector; // Reference to the square selector creator

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        squareSelector = GetComponent<SquareSelectorCreator>(); // Getting the SquareSelectorCreator component
        CreateGrid(); // Initializing the chess board grid
    }

    // Sets the dependencies for the board
    public void SetDependencies(ChessGameController chessController)
    {
        this.chessController = chessController; // Setting the chess game controller reference
    }

    // Creates the grid for the chess board
    private void CreateGrid()
    {
        grid = new Piece[BOARD_SIZE, BOARD_SIZE]; // Initializing the 2D array for the board
    }

    // Calculates the world position from board coordinates
    public Vector3 CalculatePositionFromCoords(Vector2Int coords)
    {
        // Converting board coordinates to world position
        return bottomLeftSquareTransform.position + new Vector3(coords.x * squareSize, 0f, coords.y * squareSize);
    }

    // Calculates the board coordinates from a world position
    private Vector2Int CalculateCoordsFromPosition(Vector3 inputPosition)
    {
        // Converting world position to board coordinates
        int x = Mathf.FloorToInt(transform.InverseTransformPoint(inputPosition).x / squareSize) + BOARD_SIZE / 2;
        int y = Mathf.FloorToInt(transform.InverseTransformPoint(inputPosition).z / squareSize) + BOARD_SIZE / 2;
        return new Vector2Int(x, y);
    }

    internal void OnGameRestarted()
    {
        selectedPiece = null;
        CreateGrid();
    }

    // Called when a square on the board is selected
    public void OnSquareSelected(Vector3 inputPosition)
    {
        if (!chessController.IsGameInProgress())
            return;

        Vector2Int coords = CalculateCoordsFromPosition(inputPosition); // Getting board coordinates from position
        Piece piece = GetPieceOnSquare(coords); // Getting the piece on the selected square
        // Logic to handle piece selection and movement
        if (selectedPiece)
        {
            if (piece != null && selectedPiece == piece)
                DeselectPiece();
            else if (piece != null && selectedPiece != piece && chessController.IsTeamTurnActive(piece.team))
                SelectPiece(piece);
            else if (selectedPiece.CanMoveTo(coords))
                OnSelectedPieceMoved(coords, selectedPiece);
        }
        else
        {
            if (piece != null && chessController.IsTeamTurnActive(piece.team))
                SelectPiece(piece);
        }
    }

    public void PromotePiece(Piece piece)
    {
        TakePiece(piece);
        chessController.CreatePieceAndInitialize(piece.occupiedSquare, piece.team, typeof(Queen));
    }

    // Selects a chess piece
    private void SelectPiece(Piece piece)
    {
        chessController.RemoveMovesEnablingAttackOnPieceType<King>(piece);
        selectedPiece = piece; // Setting the selected piece
        List<Vector2Int> selection = selectedPiece.avaliableMoves; // Getting available moves for the piece
        ShowSelectionSquares(selection); // Showing selection squares for the available moves
    }

    // Shows the selection squares on the board
    private void ShowSelectionSquares(List<Vector2Int> selection)
    {
        Dictionary<Vector3, bool> squaresData = new Dictionary<Vector3, bool>();
        for (int i = 0; i < selection.Count; i++)
        {
            Vector3 position = CalculatePositionFromCoords(selection[i]); // Calculating position for each selection
            bool isSquareFree = GetPieceOnSquare(selection[i]) == null; // Checking if the square is free
            squaresData.Add(position, isSquareFree); // Adding to the squares data
        }
        squareSelector.ShowSelection(squaresData); // Displaying the selection squares
    }

    // Deselects the currently selected chess piece
    private void DeselectPiece()
    {
        selectedPiece = null; // Clearing the selected piece
        squareSelector.ClearSelection(); // Clearing the selection squares
    }

    // Handles the movement of the selected chess piece
    private void OnSelectedPieceMoved(Vector2Int coords, Piece piece)
    {
        TryToTakeOppositePiece(coords);
        UpdateBoardOnPieceMove(coords, piece.occupiedSquare, piece, null); // Updating the board state
        selectedPiece.MovePiece(coords); // Moving the piece
        DeselectPiece(); // Deselecting the piece
        EndTurn(); // Ending the turn
    }

    private void TryToTakeOppositePiece(Vector2Int coords)
    {
        Piece piece = GetPieceOnSquare(coords);
        if (piece != null && !selectedPiece.IsFromSameTeam(piece))
            TakePiece(piece);
    }

    private void TakePiece(Piece piece)
    {
        if (piece)
        {
            grid[piece.occupiedSquare.x, piece.occupiedSquare.y] = null;
            chessController.OnPieceRemoved(piece);
        }
    }

    // Ends the current turn
    private void EndTurn()
    {
        chessController.EndTurn(); // Calling the end turn method on the chess controller
    }

    // Updates the board state when a piece is moved
    public void UpdateBoardOnPieceMove(Vector2Int newCoords, Vector2Int oldCoords, Piece newPiece, Piece oldPiece)
    {
        grid[oldCoords.x, oldCoords.y] = oldPiece; // Setting the old piece position to null or captured piece
        grid[newCoords.x, newCoords.y] = newPiece; // Placing the new piece on the new position
    }

    // Gets the piece on a specific square
    public Piece GetPieceOnSquare(Vector2Int coords)
    {
        if (CheckIfCoordinatesAreOnBoard(coords))
            return grid[coords.x, coords.y]; // Returning the piece at the specified coordinates
        return null;
    }

    // Checks if the coordinates are within the board boundaries
    public bool CheckIfCoordinatesAreOnBoard(Vector2Int coords)
    {
        // Checking if the coordinates are outside the board
        if (coords.x < 0 || coords.y < 0 || coords.x >= BOARD_SIZE || coords.y >= BOARD_SIZE)
            return false;
        return true;
    }

    // Checks if the board contains a specific piece
    public bool HasPiece(Piece piece)
    {
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                if (grid[i, j] == piece)
                    return true; // Returning true if the piece is found on the board
            }
        }
        return false;
    }

    // Places a piece on the board at specified coordinates
    public void SetPieceOnBoard(Vector2Int coords, Piece piece)
    {
        if (CheckIfCoordinatesAreOnBoard(coords))
            grid[coords.x, coords.y] = piece; // Setting the piece on the board
    }

}
