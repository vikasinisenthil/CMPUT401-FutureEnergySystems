using UnityEngine;

public class PlayerAnimator : MonoBehaviour {
    private SpriteRenderer spriteRenderer;
    public int playerIndex = 0;
    private CharacterSprites characterSprites;
    private int currentWalkFrame = 0;
    private float animationTimer = 0f;
    public float frameRate = 0.2f;
    private bool isWalking = false;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (GameManager.Instance != null && 
            GameManager.Instance.SelectedHeroes != null && 
            playerIndex < GameManager.Instance.SelectedHeroes.Length)
        {
            HeroType heroType = GameManager.Instance.SelectedHeroes[playerIndex];
            characterSprites = GameManager.Instance.GetCharacterSprites(heroType, playerIndex);
            UpdateIdleSprite();
        }
        else
        {
            Debug.LogWarning($"No hero selected for player {playerIndex + 1}");
        }
    }

    public void Initialize(CharacterSprites sprites) {
        if (spriteRenderer == null) {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        characterSprites = sprites;
        UpdateIdleSprite();
    }
    
    public void SetWalking(bool walking) {
        isWalking = walking;
        
        if (!walking) {
            UpdateIdleSprite();
            currentWalkFrame = 0;
        }
    }

    private void UpdateIdleSprite() {
        if (characterSprites == null || spriteRenderer == null) return;

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.GetScore() : 0;
        if (score >= 7) {
            spriteRenderer.sprite = characterSprites.sickSprite;
        } else {
            spriteRenderer.sprite = characterSprites.idleSprite;
        }
    }
    
    public void ShowSick() {
        if (characterSprites != null && spriteRenderer != null) {
            spriteRenderer.sprite = characterSprites.sickSprite;
        }
    }
    
    void Update() {
        if (isWalking && characterSprites != null && characterSprites.walkSprites.Length > 0) {
            animationTimer += Time.deltaTime;
            
            if (animationTimer >= frameRate) {
                animationTimer = 0f;
                currentWalkFrame = (currentWalkFrame + 1) % characterSprites.walkSprites.Length;
                spriteRenderer.sprite = characterSprites.walkSprites[currentWalkFrame];
            }
        }
    }
}