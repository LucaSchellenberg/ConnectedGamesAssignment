using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityChess;
using UnityEngine;

public class GameManager : NetworkBehaviour  // Changed to NetworkBehaviour for RPCs
{
    public static GameManager Instance { get; private set; }

    public bool IsMultiplayer = true;

    public static event Action NewGameStartedEvent;
    public static event Action GameEndedEvent;
    public static event Action GameResetToHalfMoveEvent;
    public static event Action MoveExecutedEvent;

    private Game game;
    private Dictionary<GameSerializationType, IGameSerializer> serializersByType;
    private GameSerializationType selectedSerializationType = GameSerializationType.FEN;

    private readonly List<(Square, Piece)> currentPiecesBacking = new List<(Square, Piece)>();
    public List<(Square, Piece)> CurrentPieces
    {
        get
        {
            currentPiecesBacking.Clear();
            for (int file = 1; file <= 8; file++)
            {
                for (int rank = 1; rank <= 8; rank++)
                {
                    Piece piece = CurrentBoard[file, rank];
                    if (piece != null)
                        currentPiecesBacking.Add((new Square(file, rank), piece));
                }
            }
            return currentPiecesBacking;
        }
    }

    public bool HasLegalMoves(Piece piece)
    {
        return game.TryGetLegalMovesForPiece(piece, out _);
    }

    private CancellationTokenSource promotionUITaskCancellationTokenSource;
    private ElectedPiece userPromotionChoice = ElectedPiece.None;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        serializersByType = new Dictionary<GameSerializationType, IGameSerializer>
        {
            [GameSerializationType.FEN] = new FENSerializer(),
            [GameSerializationType.PGN] = new PGNSerializer()
        };
    }

    private void Start()
    {
        // For single-player mode, subscribe to VisualPiece event.
        if (!IsMultiplayer)
            VisualPiece.VisualPieceMoved += OnPieceMoved;
        StartNewGame();
#if DEBUG_VIEW
        UnityChessDebug.Instance.gameObject.SetActive(true);
        UnityChessDebug.Instance.enabled = true;
#endif
    }

    #endregion

    #region Properties

    public Board CurrentBoard
    {
        get
        {
            game.BoardTimeline.TryGetCurrent(out Board currentBoard);
            return currentBoard;
        }
    }

    public Side SideToMove
    {
        get
        {
            game.ConditionsTimeline.TryGetCurrent(out GameConditions currentConditions);
            return currentConditions.SideToMove;
        }
    }

    public Side StartingSide => game.ConditionsTimeline[0].SideToMove;
    public Timeline<HalfMove> HalfMoveTimeline => game.HalfMoveTimeline;
    public int LatestHalfMoveIndex => game.HalfMoveTimeline.HeadIndex;
    public int FullMoveNumber => StartingSide switch
    {
        Side.White => LatestHalfMoveIndex / 2 + 1,
        Side.Black => (LatestHalfMoveIndex + 1) / 2 + 1,
        _ => -1
    };

    #endregion

    #region Game Setup & Serialization

    public async void StartNewGame()
    {
        game = new Game();
        NewGameStartedEvent?.Invoke();
    }

    public string SerializeGame()
    {
        return serializersByType.TryGetValue(selectedSerializationType, out IGameSerializer serializer)
            ? serializer.Serialize(game)
            : null;
    }

    public void LoadGame(string serializedGame)
    {
        game = serializersByType[selectedSerializationType].Deserialize(serializedGame);
        NewGameStartedEvent?.Invoke();
    }

    public void ResetGameToHalfMoveIndex(int halfMoveIndex)
    {
        if (!game.ResetGameToHalfMoveIndex(halfMoveIndex))
            return;
        UIManager.Instance.SetActivePromotionUI(false);
        promotionUITaskCancellationTokenSource?.Cancel();
        GameResetToHalfMoveEvent?.Invoke();
    }

    #endregion

    #region Move Execution & Special Moves

    private bool TryExecuteMove(Movement move)
    {
        if (!game.TryExecuteMove(move))
            return false;
        game.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);
        if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate)
        {
            BoardManager.Instance.SetActiveAllPieces(false);
            GameEndedEvent?.Invoke();
        }
        else
        {
            BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(SideToMove);
        }
        MoveExecutedEvent?.Invoke();
        return true;
    }

    private async Task<bool> TryHandleSpecialMoveBehaviourAsync(SpecialMove specialMove)
    {
        switch (specialMove)
        {
            case CastlingMove castlingMove:
                BoardManager.Instance.CastleRook(castlingMove.RookSquare, castlingMove.GetRookEndSquare());
                return true;
            case EnPassantMove enPassantMove:
                BoardManager.Instance.TryDestroyVisualPiece(enPassantMove.CapturedPawnSquare);
                return true;
            case PromotionMove { PromotionPiece: null } promotionMove:
                UIManager.Instance.SetActivePromotionUI(true);
                BoardManager.Instance.SetActiveAllPieces(false);
                promotionUITaskCancellationTokenSource?.Cancel();
                promotionUITaskCancellationTokenSource = new CancellationTokenSource();
                ElectedPiece choice = await Task.Run(GetUserPromotionPieceChoice, promotionUITaskCancellationTokenSource.Token);
                UIManager.Instance.SetActivePromotionUI(false);
                BoardManager.Instance.SetActiveAllPieces(true);
                if (promotionUITaskCancellationTokenSource == null || promotionUITaskCancellationTokenSource.Token.IsCancellationRequested)
                    return false;
                promotionMove.SetPromotionPiece(PromotionUtil.GeneratePromotionPiece(choice, SideToMove));
                BoardManager.Instance.TryDestroyVisualPiece(promotionMove.Start);
                BoardManager.Instance.TryDestroyVisualPiece(promotionMove.End);
                BoardManager.Instance.CreateAndPlacePieceGO(promotionMove.PromotionPiece, promotionMove.End);
                promotionUITaskCancellationTokenSource = null;
                return true;
            case PromotionMove promotionMove:
                BoardManager.Instance.TryDestroyVisualPiece(promotionMove.Start);
                BoardManager.Instance.TryDestroyVisualPiece(promotionMove.End);
                BoardManager.Instance.CreateAndPlacePieceGO(promotionMove.PromotionPiece, promotionMove.End);
                return true;
            default:
                return false;
        }
    }

    private ElectedPiece GetUserPromotionPieceChoice()
    {
        while (userPromotionChoice == ElectedPiece.None) { }
        ElectedPiece result = userPromotionChoice;
        userPromotionChoice = ElectedPiece.None;
        return result;
    }

    public void ElectPiece(ElectedPiece choice)
    {
        userPromotionChoice = choice;
    }

    #endregion

    #region Networked Move Submission

    // This RPC is used for multiplayer moves.
    [ServerRpc(RequireOwnership = false)]
    public void SubmitMoveServerRpc(string startSquare, string endSquare, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (!IsPlayerTurn(senderId))
        {
            Debug.LogWarning($"Client {senderId} attempted a move out of turn.");
            RejectMoveClientRpc();
            return;
        }
        Square from = new Square(startSquare);
        Square to = new Square(endSquare);
        if (!game.TryGetLegalMove(from, to, out Movement move))
        {
            Debug.LogWarning($"Invalid move from {startSquare} to {endSquare} by client {senderId}.");
            RejectMoveClientRpc();
            return;
        }
        bool canProcessSpecial = true;
        if (move is SpecialMove specialMove)
            canProcessSpecial = TryHandleSpecialMoveBehaviourAsync(specialMove).Result;
        if (canProcessSpecial && TryExecuteMove(move))
        {
            string currentState = SerializeGame();
            SyncBoardStateClientRpc(currentState);
        }
        else
        {
            RejectMoveClientRpc();
        }
    }

    // When a move is invalid, the server sends this RPC so that all clients re-sync.
    [ClientRpc]
    private void RejectMoveClientRpc()
    {
        string currentState = SerializeGame();
        GameManager.Instance.LoadGame(currentState);
        UIManager.Instance.UpdateGameStringInputField();
        BoardManager.Instance.OnGameResetToHalfMove();
    }

    // On a valid move, the server sends this RPC to sync all clients.
    [ClientRpc]
    private void SyncBoardStateClientRpc(string serializedGameState)
    {
        GameManager.Instance.LoadGame(serializedGameState);
        UIManager.Instance.UpdateGameStringInputField();
        BoardManager.Instance.OnGameResetToHalfMove();
    }

    private bool IsPlayerTurn(ulong clientId)
    {
        if (SideToMove == Side.White && clientId == 0)
            return true;
        if (SideToMove == Side.Black && clientId == 1)
            return true;
        return false;
    }

    #endregion

    #region Piece Movement Handler (Single-Player)
    // For single-player mode, the original event works as before.
    private async void OnPieceMoved(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null)
    {
        Square endSquare = new Square(closestBoardSquareTransform.name);
        if (!game.TryGetLegalMove(movedPieceInitialSquare, endSquare, out Movement move))
        {
#if DEBUG_VIEW
            Piece movedPiece = CurrentBoard[movedPieceInitialSquare];
            game.TryGetLegalMovesForPiece(movedPiece, out ICollection<Movement> legalMoves);
            UnityChessDebug.ShowLegalMovesInLog(legalMoves);
#endif
            movedPieceTransform.position = movedPieceTransform.parent.position;
            return;
        }
        if (move is PromotionMove promotionMove)
        {
            promotionMove.SetPromotionPiece(promotionPiece);
        }
        if ((move is not SpecialMove specialMove || await TryHandleSpecialMoveBehaviourAsync(specialMove))
            && TryExecuteMove(move))
        {
            if (move is not SpecialMove)
                BoardManager.Instance.TryDestroyVisualPiece(move.End);
            if (move is PromotionMove)
                movedPieceTransform = BoardManager.Instance.GetPieceGOAtPosition(move.End).transform;
            movedPieceTransform.parent = closestBoardSquareTransform;
            movedPieceTransform.position = closestBoardSquareTransform.position;
        }
    }
    #endregion

    public void ApplySkinToPieces(Texture2D newSkin, Side targetSide)
    {
        // Find all VisualPiece objects in the scene.
        VisualPiece[] pieces = GameObject.FindObjectsOfType<VisualPiece>();
        foreach (VisualPiece vp in pieces)
        {
            // Only update the pieces that match the target color.
            if (vp.PieceColor == targetSide)
            {
                // Try to update a Renderer (for 3D models).
                Renderer renderer = vp.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = newSkin;
                }
                else
                {
                    // If you are using 2D sprites, try a SpriteRenderer instead.
                    SpriteRenderer spriteRenderer = vp.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        // Create a new sprite from the texture.
                        spriteRenderer.sprite = Sprite.Create(newSkin,
                            new Rect(0, 0, newSkin.width, newSkin.height),
                            new Vector2(0.5f, 0.5f));
                    }
                }
            }
        }
    }
}
