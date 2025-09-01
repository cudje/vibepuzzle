using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Piece3DManager : MonoBehaviour
{
    public GameObject[] allPieces;

    // 가장 가까운 piece 반환 (Pick용)
    public GameObject GetNearestAvailablePiece(Vector3 position, float radius = 1.0f)
    {
        foreach (GameObject piece in allPieces)
        {
            if (piece == null) continue;

            float dist = Vector3.Distance(position, piece.transform.position);
            if (dist < radius)
            {
                return piece;
            }
        }

        return null;
    }

}