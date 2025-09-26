using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecePlaceholder : MonoBehaviour {
    public TetriminoEnum pieceType = TetriminoEnum.X;
    [SerializeField] public Sprite texture = null;

    // Start is called before the first frame update
    void Start() {
        changeType(pieceType);
    }

    // Update is called once per frame
    void Update() {}

    public void changeType(TetriminoEnum piecetype) {
        pieceType = piecetype;

        texture = TetriminoSettings.getTetriminoPieceTexture(pieceType);
        if (texture == null) {
            Debug.LogWarning($"PiecePlaceholder: texture for {pieceType} is null. Make sure TetriminoSettings instance has piece textures assigned.");
            return;
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = texture;
    }
}
