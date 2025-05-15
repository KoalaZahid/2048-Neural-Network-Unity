using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    private Vector2[] allDirections = {Vector2.up, Vector2.left, Vector2.down, Vector2.right};
    public static int score = 0;
    [SerializeField] private GameObject _gameBoardContainer;
    [SerializeField] private TextMeshProUGUI _movesDisplay;
    [SerializeField] private GameObject _startGameButton;
    public event EventHandler OnBoardUpdate;
    public event EventHandler OnStartGame;
    public event EventHandler OnGameFail;
    public event EventHandler OnIllegalMove;
    [SerializeField] private GameObject NeuralNetwork;
    private NEATNetwork networkScript;
    [SerializeField] private GameObject _resetGameButton;
    [SerializeField] private TextMeshProUGUI _scoreDisplay;
    [SerializeField] private TextMeshProUGUI _highScoreDisplay;
    [SerializeField] private TextMeshProUGUI _frameRateCounter;

    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = 0.2f;
    [SerializeField] private int _winCondition = 2048;

    [SerializeField] private GameObject _winScreen, _lostScreen;

    public List<Node> _nodes;
    public static List<Block> _blocks;
    private GameState _state;
    public int _round;

    private bool moved = true;

    private AudioSource audioSource;
    public AudioClip _blockSlideSound;
    public AudioClip _blockMergeSound;
    [SerializeField] private bool _debug = false;
    public static bool _debugEnabled = false;
    public bool speed = false;

    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);

    public void Start() {
        _debugEnabled = _debug;
        _startGameButton.SetActive(true);
        _resetGameButton.SetActive(false);
        ChangeState(GameState.GenerateLevel);
        _movesDisplay.text = "Moves: 0";
        _scoreDisplay.text = "0"; 
        _highScoreDisplay.text = PlayerPrefs.GetInt("HighScore",0).ToString();
        audioSource = transform.GetComponent<AudioSource>();
        networkScript = NeuralNetwork.GetComponent<NEATNetwork>();

        // events
        networkScript.UpInput += UpPressed;
        networkScript.DownInput += DownPressed;
        networkScript.LeftInput += LeftPressed;
        networkScript.RightInput += RightPressed;
    }

    public int getHighScore() {
        return PlayerPrefs.GetInt("HighScore",0);
    }

    public void Reset() {
        moved = true;
        _startGameButton.SetActive(true);
        _resetGameButton.SetActive(false);
        _round = 0;
        foreach (var block in _blocks) {
            Destroy(block.gameObject);
        }
        _blocks.Clear();
        _movesDisplay.text = "Moves: 0";
        _scoreDisplay.text = "0";
        _lostScreen.SetActive(false);
        _winScreen.SetActive(false);
    }

    public void StartGame() {
        OnStartGame?.Invoke(this,EventArgs.Empty);
        Reset();
        moved = true;
        score = 0;
        ChangeState(GameState.SpawningBlocks);
        _startGameButton.SetActive(true);
        UpdateMovesDisplay();
        UpdateScoreDisplay();
    }

    public void QuitGame() {
        Application.Quit();
    }

    private void UpdateMovesDisplay() {
        _movesDisplay.text = "Moves: " + (_round - 1);
    }
    private void UpdateScoreDisplay() {
        _scoreDisplay.text = score.ToString();
        if (score > PlayerPrefs.GetInt("HighScore",0)) {
            PlayerPrefs.SetInt("HighScore",score);
        }
        _highScoreDisplay.text = PlayerPrefs.GetInt("HighScore",0).ToString();
    }

    private void ChangeState(GameState newState) {
        _state = newState;

        switch (newState) {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:

                if (moved) {
                    SpawnBlocks(_round++ == 0 ? 2 : 1);
                    OnBoardUpdate?.Invoke(this, EventArgs.Empty);
                } else {
                    // Debug.Log("Illegal move");
                    OnIllegalMove?.Invoke(this, EventArgs.Empty);
                    ChangeState(GameState.WaitingInput);
                }

                UpdateMovesDisplay();
                UpdateScoreDisplay();
                break;
            case GameState.WaitingInput:
                break;
            case GameState.Moving: 
                break;
            case GameState.Win:
                if(_winScreen != null) _winScreen.SetActive(true);
                _resetGameButton.SetActive(true);
                break;
            case GameState.Lose:
                if (_lostScreen != null) _lostScreen.SetActive(true);
                _resetGameButton.SetActive(true);
                OnGameFail?.Invoke(this,EventArgs.Empty);
                break;
            case GameState.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    private bool inputDown = false;
    void Update() {
        _debugEnabled = _debug;
        var current = (int)(1f / Time.unscaledDeltaTime);
        _frameRateCounter.text = "FPS: " + current.ToString();
        if (_state != GameState.WaitingInput) return;

        // input part here
        if (Input.anyKeyDown && !inputDown) {
            inputDown = true;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { 
                ShiftBlocks(Vector2.left);
            } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                ShiftBlocks(Vector2.up);
            } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                ShiftBlocks(Vector2.down);
            } else if (Input.GetKeyDown(KeyCode.RightArrow)) ShiftBlocks(Vector2.right);
        } else {
            inputDown = false;
        }
    }
    
    void UpPressed(object sender, EventArgs e)
    {
        if (_state != GameState.WaitingInput) return;
        // Debug.Log("Computer Tried going UP!");
        ShiftBlocks(Vector2.up);
    }
    void DownPressed(object sender, EventArgs e)
    {
        if (_state != GameState.WaitingInput) return;
        // Debug.Log("Computer Tried going DOWN!");
        ShiftBlocks(Vector2.down);
    }
    void LeftPressed(object sender, EventArgs e)
    {
        if (_state != GameState.WaitingInput) return;
        // Debug.Log("Computer Tried going LEFT!");
        ShiftBlocks(Vector2.left);
    }
    void RightPressed(object sender, EventArgs e)
    {
        if (_state != GameState.WaitingInput) return;
        // Debug.Log("Computer Tried going RIGHT!");
        ShiftBlocks(Vector2.right);
    }

    void GenerateGrid() {
        _round = 0;
        _blocks = new List<Block>();
        _nodes = new List<Node>();
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                var node = Instantiate(_nodePrefab, new Vector2(x,y), Quaternion.identity);
                node.transform.parent = _gameBoardContainer.transform;
                _nodes.Add(node);
            }
        }

        var center = new Vector2((float) _width/2 - 0.5f, (float) _height/2 - 0.5f);
        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width,_height);
        board.transform.parent = _gameBoardContainer.transform;

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);
    }

    void SpawnBlocks(int amount) {
        moved = false;
        var freeNodes = _nodes.Where(n=>n.OccupiedBlock == null).OrderBy(b=>Random.value);

        foreach (var node in freeNodes.Take(amount)) {
            SpawnBlockAtNode(node, Random.value > 0.9f ? 2 : 1 );
        }  

        if (freeNodes.Count() == 0) {
            if (!CheckForMergableNodes()) {
                ChangeState(GameState.Lose);
                return;
            }
        }
        ChangeState(_blocks.Any(b => b.Value == _winCondition) ? GameState.Win : GameState.WaitingInput);
    }

    IEnumerator SwitchToWaitingInput(float time) {
        yield return new WaitForSeconds(time);
        ChangeState(GameState.WaitingInput);
    }

    bool CheckForMergableNodes() {
        bool canMerge = false;
        foreach (var block in _blocks){
            foreach (var direction in allDirections){
                var possibleNode = GetNodeAtPosition(block.Node.Pos + direction);
                if (possibleNode != null && possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value)) {
                    canMerge = true;
                    break;
                }
            }
        }
        return canMerge;
    }

    void SpawnBlockAtNode(Node node, int value) {
        var block = Instantiate(_blockPrefab,node.Pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        block.transform.parent = _gameBoardContainer.transform;
        _blocks.Add(block);
    }

    void ShiftBlocks(Vector2 dir) {
        ChangeState(GameState.Moving);
        // Debug.Log( NeuralNetwork.GetComponent<NEATNetwork>().Fitness() );
        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y);
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks) {
            var next = block.Node;
            var numberOfMoves = 0;
            do {
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition( next.Pos + dir );
                if (possibleNode != null) {
                    // Node present
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value)) 
                      {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                        numberOfMoves++;
                    } else if (possibleNode.OccupiedBlock == null) {
                        next = possibleNode;
                        numberOfMoves++;
                    }
                }

            } while (next != block.Node);
            if (numberOfMoves > 0) moved = true;
        }

        if (moved) {

            if (speed) {

                foreach (var block in orderedBlocks) {
                    var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
                    block.transform.SetPositionAndRotation(movePoint, Quaternion.identity);
                }
                foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null)) {
                    MergeBlocks(block.MergingBlock, block);
                }
                ChangeState(GameState.SpawningBlocks);

            } else {
                var sequence = DOTween.Sequence();

                foreach (var block in orderedBlocks) {
                    var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
                    sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
                }
                audioSource.PlayOneShot(_blockSlideSound);

                sequence.OnComplete(() => {
                    foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null)) {
                        MergeBlocks(block.MergingBlock, block);
                    }

                    ChangeState(GameState.SpawningBlocks);
                });
            }

        } else {
            ChangeState(GameState.SpawningBlocks);
        }
        
    }

    void MergeBlocks(Block baseBlock, Block mergingBlock) {
        var newValue = baseBlock.Value + 1;
        score+= (int) Mathf.Pow(2f, (float) newValue); //2^newValue;
        audioSource.PlayOneShot(_blockMergeSound);
        SpawnBlockAtNode(baseBlock.Node, newValue);

        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    void RemoveBlock(Block block) {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    public List<Block> getCurrentBlocksOnBoard() {
        return _blocks;
    }

    Node GetNodeAtPosition(Vector2 pos) {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

    public List<Node> GetBoard() {
        return _nodes;
    }
}

[Serializable]
public struct BlockType {
    public int Value;
    public Color color;
    public Color FontColor;
}

public enum GameState {
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose,
    Reset
}