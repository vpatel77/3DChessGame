using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// The ChessGameController class is responsible for managing the flow of a chess game.
[RequireComponent(typeof(PiecesCreator))]
public class ChessGameController : MonoBehaviour
{

    private enum GameState { Init, Play, Finished };

    // Serialized fields for setting up the game via the Unity Editor.
    [SerializeField] private BoardLayout startingBoardLayout; // The initial configuration of the chessboard.
    [SerializeField] private Board board; // The chessboard itself.
    [SerializeField] private ChessUIManager uiManager;

    // Reference to the PiecesCreator component for creating piece instances.
    private PiecesCreator pieceCreator;
    // Player references.
    private ChessPlayer whitePlayer;
    private ChessPlayer blackPlayer;
    private ChessPlayer activePlayer; // The player who is currently taking their turn.
    private GameState state;

    // Awake is called when the script instance is being loaded.
    private void Awake(){
        SetDependencies(); // Set up component references.
        CreatePlayers(); // Initialize player instances.
    }

    // Sets up references to required components.
    private void SetDependencies(){
        pieceCreator = GetComponent<PiecesCreator>(); // Get the PiecesCreator component.
    }

    // Creates player instances and assigns them their respective team colors and board.
    private void CreatePlayers(){
        whitePlayer = new ChessPlayer(TeamColor.White, board);
        blackPlayer = new ChessPlayer(TeamColor.Black, board);
    }

    // Start is called before the first frame update.
    private void Start(){
        StartNewGame(); // Begin a new game.
    }

    // Initializes a new game by setting up the board and pieces.
    private void StartNewGame()
    {
        uiManager.HideUI();
        SetGameState(GameState.Init);
        board.SetDependencies(this); // Provide the board a reference to this controller.
        CreatePiecesFromLayout(startingBoardLayout); // Set up the pieces on the board.
        activePlayer = whitePlayer; // White typically starts in chess.
        GenerateAllPossiblePlayerMoves(activePlayer); // Calculate possible moves for the starting player.
        SetGameState(GameState.Play);
    }

    public void RestartGame()
    {
        DestroyPieces();
        board.OnGameRestarted();
        whitePlayer.OnGameRestarted();
        blackPlayer.OnGameRestarted();
        StartNewGame();
    }

    private void DestroyPieces()
    {
        whitePlayer.activePieces.ForEach(p => Destroy(p.gameObject));
        blackPlayer.activePieces.ForEach(p => Destroy(p.gameObject));
    }

    private void SetGameState(GameState state)
    {
        this.state = state;
    }

    public bool IsGameInProgress()
    {
        return state == GameState.Play;
    }

    // Creates the chess pieces on the board based on the provided layout.
    private void CreatePiecesFromLayout(BoardLayout layout){
        for (int i = 0; i < layout.GetPiecesCount(); i++){
            Vector2Int squareCoords = layout.GetSquareCoordsAtIndex(i);
            TeamColor team = layout.GetSquareTeamColorAtIndex(i);
            string typeName = layout.GetSquarePieceNameAtIndex(i);

            Type type = Type.GetType(typeName); // Dynamically get the type from the piece name.
            CreatePieceAndInitialize(squareCoords, team, type); // Instantiate and initialize the piece.
        }
    }

    // Instantiates a piece and initializes it on the board.
    public void CreatePieceAndInitialize(Vector2Int squareCoords, TeamColor team, Type type){
        Piece newPiece = pieceCreator.CreatePiece(type).GetComponent<Piece>(); // Create the piece game object.
        newPiece.SetData(squareCoords, team, board); // Set the piece's data.

        Material teamMaterial = pieceCreator.GetTeamMaterial(team); // Get the correct team material.
        newPiece.SetMaterial(teamMaterial); // Set the piece's material.

        board.SetPieceOnBoard(squareCoords, newPiece); // Place the piece on the board.

        ChessPlayer currentPlayer = team == TeamColor.White ? whitePlayer : blackPlayer; // Determine the correct player.
        currentPlayer.AddPiece(newPiece); // Add the piece to the player's active list.
    }

    // Generates all possible moves for a given player.
    private void GenerateAllPossiblePlayerMoves(ChessPlayer player){
        player.GenerateAllPossibleMoves(); // Ask the player to calculate possible moves.
    }

    // Checks if it is currently the given team's turn.
    public bool IsTeamTurnActive(TeamColor team){
        return activePlayer.team == team; // Compare the active player's team to the given team.
    }

    // Ends the current turn and switches to the next player.
    public void EndTurn(){
        GenerateAllPossiblePlayerMoves(activePlayer); // Recalculate moves for the current player.
        GenerateAllPossiblePlayerMoves(GetOpponentToPlayer(activePlayer)); // Calculate moves for the opponent.
        if (CheckIfGameIsFinished())
            EndGame();
        else
            ChangeActiveTeam();
    }

    private bool CheckIfGameIsFinished()
    {
        Piece[] kingAttackingPieces = activePlayer.GetPiecesAttackingOppositePieceOfType<King>();
        if (kingAttackingPieces.Length > 0)
        {
            ChessPlayer oppositePlayer = GetOpponentToPlayer(activePlayer);
            Piece attackedKing = oppositePlayer.GetPiecesOfType<King>().FirstOrDefault();
            oppositePlayer.RemoveMovesEnablingAttackOnPiece<King>(activePlayer, attackedKing);

            int availableKingMoves = attackedKing.avaliableMoves.Count;
            if (availableKingMoves == 0)
            {
                bool canCoverKing = oppositePlayer.CanHidePieceFromAttack<King>(activePlayer);
                if (!canCoverKing)
                    return true;
            }
        }
        return false;
    }

    public void OnPieceRemoved(Piece piece)
    {
        ChessPlayer pieceOwner = (piece.team == TeamColor.White) ? whitePlayer : blackPlayer;
        pieceOwner.RemovePiece(piece);
        Destroy(piece.gameObject);
    }

    private void EndGame()
    {
        uiManager.OnGameFinished(activePlayer.team.ToString());
        SetGameState(GameState.Finished);
    }

    // Switches the active player.
    private void ChangeActiveTeam(){
        activePlayer = activePlayer == whitePlayer ? blackPlayer : whitePlayer; // Toggle between white and black players.
    }

    // Retrieves the opponent of the given player.
    private ChessPlayer GetOpponentToPlayer(ChessPlayer player){
        return player == whitePlayer ? blackPlayer : whitePlayer; // Return the opposite player.
    }

    public void RemoveMovesEnablingAttackOnPieceType<T>(Piece piece) where T : Piece
    {
        activePlayer.RemoveMovesEnablingAttackOnPiece<T>(GetOpponentToPlayer(activePlayer), piece);
    }
}
