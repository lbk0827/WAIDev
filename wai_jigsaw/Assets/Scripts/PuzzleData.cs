using UnityEngine;
using System.Collections.Generic;

// 난이도 종류를 정의합니다. (드롭다운 메뉴로 선택할 수 있게 됩니다)
public enum PuzzleDifficulty
{
    Easy,   // 24조각
    Normal, // 48조각
    Hard    // 96조각
}

[CreateAssetMenu(fileName = "New Puzzle Data", menuName = "Jigsaw/Puzzle Data")]
public class PuzzleData : ScriptableObject
{
    [Header("기본 정보")]
    public string puzzleId; // 예: Nature_01
    public Sprite originalImage; // 완성된 그림 (참고용/고스트 힌트용)

    [Header("★ 난이도별 조각 데이터")]
    // 기획자님이 Sprite Editor로 자른 조각들을 여기에 담을 겁니다.
    
    // 1. Easy (4x6 = 24)
    public List<Sprite> easyPieces; 
    public int easyRows = 4;
    public int easyCols = 6;

    // 2. Normal (6x8 = 48)
    public List<Sprite> normalPieces;
    public int normalRows = 6;
    public int normalCols = 8;

    // 3. Hard (8x12 = 96)
    public List<Sprite> hardPieces;
    public int hardRows = 8;
    public int hardCols = 12;
}