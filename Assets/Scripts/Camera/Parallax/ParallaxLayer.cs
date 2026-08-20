using System;
using UnityEngine;

[Serializable]
public class ParallaxLayer
{
    [Header("Layer")]
    public Transform target;


    [Header("Horizontal Parallax")]

    [Tooltip(
        "0 = very far away / almost follows the camera.\n" +
        "1 = much stronger parallax movement."
    )]
    [Range(0f, 1f)]
    public float horizontalStrength = 0.1f;


    [Header("Vertical Parallax")]

    public bool useVerticalParallax = true;


    [Range(0f, 1f)]
    public float verticalStrength = 0.03f;


    [Header("Optional Pixel Snap")]

    [Tooltip(
        "Leave OFF when Pixel Perfect Camera is enabled."
    )]
    public bool snapTransformToPixelGrid = false;
}