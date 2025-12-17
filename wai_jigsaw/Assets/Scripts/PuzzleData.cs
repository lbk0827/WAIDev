using UnityEngine;

[CreateAssetMenu(fileName = "New Puzzle Data", menuName = "Jigsaw/Puzzle Data")]
public class PuzzleData : ScriptableObject
{
    public string imageId;       // 이미지 ID (예: "Photo_01")
    public Texture2D sourceImage; // ★ 원본 이미지 (Sprite 말고 Texture2D로 받습니다)
}