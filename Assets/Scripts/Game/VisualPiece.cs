using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

public class VisualPiece : MonoBehaviour
{
    public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
    public static event VisualPieceMovedAction VisualPieceMoved;

    public Side PieceColor;

    public Square CurrentSquare => StringToSquare(transform.parent.name);

    private const float SquareCollisionRadius = 9f;
    private Camera boardCamera;
    private Vector3 piecePositionSS;
    private List<GameObject> potentialLandingSquares = new List<GameObject>();
    private Transform thisTransform;

    private void Start()
    {
        boardCamera = Camera.main;
        thisTransform = transform;
    }

    public void OnMouseDown()
    {
        if (enabled)
        {
            piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
        }
    }

    private void OnMouseDrag()
    {
        if (enabled)
        {
            Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
            thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
        }
    }

    public void OnMouseUp()
    {
        if (enabled)
        {
            potentialLandingSquares.Clear();
            BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);
            if (potentialLandingSquares.Count == 0)
            {
                thisTransform.position = transform.parent.position;
                return;
            }
            Transform closestSquareTransform = potentialLandingSquares[0].transform;
            float shortestDistanceSquared = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;
            for (int i = 1; i < potentialLandingSquares.Count; i++)
            {
                GameObject candidate = potentialLandingSquares[i];
                float distSq = (candidate.transform.position - thisTransform.position).sqrMagnitude;
                if (distSq < shortestDistanceSquared)
                {
                    shortestDistanceSquared = distSq;
                    closestSquareTransform = candidate.transform;
                }
            }
            if (GameManager.Instance.IsMultiplayer)
            {
                // In multiplayer, send the move to the server
                GameManager.Instance.SubmitMoveServerRpc(CurrentSquare.ToString(), closestSquareTransform.name);
            }
            else
            {
                VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
            }
        }
    }
}
