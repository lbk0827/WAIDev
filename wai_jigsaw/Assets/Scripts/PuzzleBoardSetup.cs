using UnityEngine;
using System.Collections.Generic; // 리스트(List)를 쓰기 위해 필요합니다.

public class PuzzleBoardSetup : MonoBehaviour
{
    [Header("퍼즐 설정")]
    // Inspector 창에서 기획자님이 자른 20개의 이미지를 여기에 넣을 것입니다.
    public List<Sprite> puzzleSprites; 

    [Header("격자 크기")]
    public int columns = 4; // 가로 4칸
    public int rows = 5;    // 세로 5줄

    void Start()
    {
        CreatePuzzlePieces();
    }

    void CreatePuzzlePieces()
    {
        // 1. 첫 번째 조각의 사이즈를 측정해서 간격을 정합니다.
        if (puzzleSprites.Count == 0) return;
        
        // 이미지 하나의 너비와 높이를 가져옵니다.
        float pieceWidth = puzzleSprites[0].bounds.size.x;
        float pieceHeight = puzzleSprites[0].bounds.size.y;

        // 2. 전체 퍼즐판이 화면 중앙에 오도록 시작 위치를 계산합니다.
        // (수학적인 부분이라 이해만 하시면 됩니다: 전체 길이의 절반만큼 왼쪽/위로 이동)
        float startX = -((columns * pieceWidth) / 2) + (pieceWidth / 2);
        float startY = ((rows * pieceHeight) / 2) - (pieceHeight / 2);

        // 3. 반복문을 통해 20개의 조각을 하나씩 생성합니다.
        for (int i = 0; i < puzzleSprites.Count; i++)
        {
            // --- A. 새로운 빈 게임 오브젝트 생성 ---
            GameObject newPiece = new GameObject($"Piece_{i}");

            // --- B. 이미지(Sprite Renderer) 붙이기 ---
            SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
            sr.sprite = puzzleSprites[i]; // 리스트에 있는 이미지를 순서대로 입힘

            // --- C. 충돌체(Box Collider 2D) 붙이기 ---
            newPiece.AddComponent<BoxCollider2D>();

            // --- D. 우리가 만든 드래그 기능(DragController) 붙이기 ---
            newPiece.AddComponent<DragController>();

            // --- E. 위치 잡기 (격자 배치 로직) ---
            // i번째 조각이 몇 번째 줄(row), 몇 번째 칸(col)인지 계산
            int col = i % columns; // 나눈 나머지 (0, 1, 2, 3)
            int row = i / columns; // 몫 (0, 1, 2, 3, 4)

            // 계산된 위치로 이동 (Y는 아래로 내려가야 하므로 빼줍니다)
            float posX = startX + (col * pieceWidth);
            float posY = startY - (row * pieceHeight);

            newPiece.transform.position = new Vector3(posX, posY, 0);

            // 정리: 생성된 조각을 이 스크립트가 붙은 오브젝트의 자식으로 둡니다. (Hierarchy 창 정리용)
            newPiece.transform.parent = this.transform;
        }
    }
}