using System;
using UnityEngine;


[Serializable]
public struct DesktopPetLayoutProfile
{
    [Header("Layout Mode")]
    public DesktopPetLayoutMode Mode;


    [Header("Window Logical Size")]
    public int WindowWidth;

    public int WindowHeight;


    [Header("Canvas Reference Resolution")]
    public Vector2 ReferenceResolution;


    [Header("Canvas Scaling")]
    [Range(0f, 1f)]
    public float MatchWidthOrHeight;


    public DesktopPetLayoutProfile(
        DesktopPetLayoutMode mode,
        int windowWidth,
        int windowHeight,
        Vector2 referenceResolution,
        float matchWidthOrHeight = 0.5f)
    {
        Mode =
            mode;

        WindowWidth =
            windowWidth;

        WindowHeight =
            windowHeight;

        ReferenceResolution =
            referenceResolution;

        MatchWidthOrHeight =
            matchWidthOrHeight;
    }
}