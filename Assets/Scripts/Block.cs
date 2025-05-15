using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Block : MonoBehaviour {
    private static bool debugMode;
    public int Value;

    private Color _color;
    private Color _fontColor;

    public Node Node;
    public Block MergingBlock;
    public bool Merging;
    public bool Moved = false;

    private static Color red = new Color(1,0,0,1);
    private static Color green = new Color(0,1,0,1);

    public Vector2 Pos => transform.position;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private TextMeshPro _text;

    public void Start() {
        debugMode = GameManager._debugEnabled;
        // Debug.Log(debugMode);
        // board = gameManager.getCurrentBlocksOnBoard();
    }

    public void Init(BlockType type) {
        Value = type.Value;
        _color = type.color;
        var newValue = debugMode ? Value : Mathf.Pow(2f,(float) Value);
        _fontColor = (newValue > 4) ? new Color(0.9764706f,0.9490197f,0.9215687f,1f) : type.FontColor;
        if (debugMode) {
            int highest = getHighestValueInBoard();
            _renderer.color = Color.Lerp(red,green, (float)Value/highest);
        } else {
            _renderer.color = _color;
        }
        // _renderer.color = !debugMode ? type.color : Color.white;
        _text.color = debugMode? Color.white : type.FontColor;
        if (newValue > 4 && !debugMode) { // 4
            _text.color = new Color(0.9764706f,0.9490197f,0.9215687f,1f);
        }
        string valueDisplay = "";
        if (debugMode) {
            valueDisplay = newValue.ToString()+" / "+ getHighestValueInBoard().ToString();
        } else {
            valueDisplay = newValue.ToString();
        }
        _text.text = valueDisplay;
        // Debug.Log(debugMode.ToString()+" "+ newValue.ToString());
    }

    void Update() {
        debugMode = GameManager._debugEnabled;
        if (debugMode) {
            int highest = getHighestValueInBoard();
             _renderer.color = Color.Lerp(red,green, (float)Value/highest);
             _text.text = this.Value.ToString()+" / "+highest.ToString();
             _text.color = Color.white;
        } else {
            var value = Mathf.Pow(2f,(float) this.Value);
            if (value > 4) _text.color = new Color(0.9764706f,0.9490197f,0.9215687f,1f);
            _renderer.color = _color;
            _text.color = _fontColor;
            _text.text = value.ToString();
        }
    }


    public void SetBlock(Node node) {
        if (Node != null) Node.OccupiedBlock = null;
        Node = node;
        Node.OccupiedBlock = this;
    }

    public void MergeBlock(Block blockToMergeWith) {
        if (blockToMergeWith.Merging || Merging) return;
        MergingBlock = blockToMergeWith;

        Node.OccupiedBlock = null;

        blockToMergeWith.Merging = true;
    }

    private int getHighestValueInBoard() {
        List<Block> board = GameManager._blocks;
        int highest = 0;
        foreach (Block block in board)
        {
            if (block.Value > highest) highest = block.Value;
        }
        return highest;
    }

    public bool CanMerge(int value) => value == Value && !Merging && MergingBlock == null;
}
