using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Cell : MonoBehaviour {
    public TetriminoEnum pieceType = TetriminoEnum.X;

    [SerializeField] public Sprite texture = null;

    // Start is called before the first frame update
    void Start() {
        this.changeType(this.pieceType);
    }

    // Update is called once per frame
    void Update() {

    }

    public void changeType(TetriminoEnum piecetype) {
        this.pieceType = piecetype;

        this.texture = TetriminoClass.getTetriminoTexture(this.pieceType);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = this.texture;
    }
}
