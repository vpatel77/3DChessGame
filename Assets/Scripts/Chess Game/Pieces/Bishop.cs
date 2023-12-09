using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class representing a Bishop chess piece, inheriting from the Piece class.
public class Bishop : Piece
{
    // Array of directions in which a bishop can move: diagonally in all four directions.
    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 1),   // Up-right diagonal
        new Vector2Int(1, -1),  // Down-right diagonal
        new Vector2Int(-1, 1),  // Up-left diagonal
        new Vector2Int(-1,- 1), // Down-left diagonal
    };

    // Override the method to select available squares for the bishop to move.
    public override List<Vector2Int> SelectAvaliableSquares()
    {
        avaliableMoves.Clear(); // Clearing the list of available moves.

        // Define the range of the board. It's set to the board size.
        float range = Board.BOARD_SIZE;

        // Iterate through each diagonal direction.
        foreach (var direction in directions)
        {
            // Check each square in the direction up to the board range.
            for (int i = 1; i <= range; i++)
            {
                // Calculate the next square in the direction.
                Vector2Int nextCoords = occupiedSquare + direction * i;

                // Get the piece on the next square, if any.
                Piece piece = board.GetPieceOnSquare(nextCoords);

                // Break if the next coordinates are outside the board boundaries.
                if (!board.CheckIfCoordinatesAreOnBoard(nextCoords))
                    break;

                // If there is no piece on the square, add it to the available moves.
                if (piece == null)
                    TryToAddMove(nextCoords);
                else if (!piece.IsFromSameTeam(this)) // If there is a piece of the opposite team,
                {
                    TryToAddMove(nextCoords); // add the move and break, as bishops cannot jump over pieces.
                    break;
                }
                else if (piece.IsFromSameTeam(this)) // If there's a piece of the same team,
                    break; // do not add the move and break the loop.
            }
        }
        return avaliableMoves; // Return the list of available moves.
    }
}
