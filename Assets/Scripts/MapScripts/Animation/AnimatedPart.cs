using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class AnimatedPart
{
    [Tooltip("Cell position on the Tilemap.")]
    public Vector3Int cell;

    [Tooltip("Animated Tile used for this statue part.")]
    public TileBase animatedTile;

    [Tooltip(
        "Static Tile representing the LAST frame of this part. " +
        "Only required when End Behavior is Freeze On Last Frame."
    )]
    public TileBase lastFrameTile;
}