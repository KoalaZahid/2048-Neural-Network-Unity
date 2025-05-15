using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*

This algorithhm takes in a starting, user-defined neural network
and modifies it based on fitness every generation.

Using a sample size, the code chooses the best bunch, say top 30%,
and essentially combine the two networks into a new, mutated network
that should perform better.

Purpose of the NEAT (NeuroEvolution of Augmenting Topologies) algorithm
is to remove the need to define the amount of hidden layers, and instead
have the network, generation by generation, find the optimal hidden layers
with neurons, weights, and biases.

TODO:
attempt to implement NEAT
modify fitness to consider empty spaces and score
penalize the network with illegal moves.

*/

public class NEATNetwork : MonoBehaviour
{

    /*
    Networks: NetworkTest1, NetworkTest489, NetworkTest100
    */

    public bool _networkActive = true;
    public GameObject gameManagerObj;
    private GameManager gameManager;
    private NeuralNetwork test;

    [SerializeField] private TextMeshProUGUI _generationText;
    [SerializeField] private TextMeshProUGUI _populationText;

    System.Random rng = new System.Random();

    private int fails = 0;
    private int gen = 0;
    private int pop = 0;
    private bool gameStarted = false;
    private bool gameFailed = false;

    private int openSpaces = 14;
    private int answerIndex = 0;

    private float lastUpdate = 0;
    [Range(6,240)]
    public int _updateFrameRate = 30;

    public int _maxPopulation = 10;
    public int _topPercentile = 30;
    public int _maxFails = 10;
    public float _mutateRange = .5f;
    public int _stopAtGen = 489;

    public string _loadNetworkFromPath;
    private bool usingFileSave = false;

    private int prevHighest = 0;
    private int currentHighest = 0;
    private NeuralNetworkSave[] GenerationNetworks;
    private double[] networkFitness;
    private bool[] resetFromFails;

    public Activation.ActivationType activationType;

    public event EventHandler UpInput;
    public event EventHandler DownInput;
    public event EventHandler LeftInput;
    public event EventHandler RightInput;

    void Start() {
        Application.targetFrameRate = 240;
        gameManager = gameManagerObj.GetComponent<GameManager>();
        int Score = GameManager.score;
        int HighScore = gameManager.getHighScore();
        setUpNewNetwork();
        gameManager.OnBoardUpdate += OnBoardUpdate;
        gameManager.OnStartGame += OnGameStart;
        gameManager.OnGameFail += OnGameFail;
        gameManager.OnIllegalMove += OnIllegalMove;
        GenerationNetworks = new NeuralNetworkSave[ (_loadNetworkFromPath=="")? _maxPopulation : 1];
        networkFitness = new double[_maxPopulation];
    }

    private void OnGameFail(object sender, EventArgs e){
        gameFailed = true;
        if (_loadNetworkFromPath != "") gameStarted = false;
    }

    private void OnIllegalMove(object sender, EventArgs e) {
        fails++;
    }

    private int[] checkIndex = {-1,1,-4,4};
    public double Fitness() {
        int addition = 0;
        if( getHighestValueInBoard() > prevHighest ) {
            prevHighest = getHighestValueInBoard();
            addition = prevHighest;
        }
        var failedCheck = (fails > 0)? fails : 1;
        var board = BoardToArray();
        var highestBlockIndex = Array.IndexOf(board,1);
        var highestBlockNextToEachOther = false;
        
        for (int i = 0; i < checkIndex.Length; i++) {
            // within board bounds
            var checkBlock = highestBlockIndex+checkIndex[i];
            if ( Math.Clamp(checkBlock,0,15) == checkBlock ) {
                // if using the first 2 indexes, check if on the same row
                if (i < 2) {
                    int row = highestBlockIndex / 16;
                    if( row == checkBlock / 16 ) {
                        if ( board[checkBlock] == 1 ) {
                            highestBlockNextToEachOther = true;
                            break;
                        }
                    }
                } else { // comparing top and bottom
                    if( board[checkBlock] == 1 ) {
                        highestBlockNextToEachOther = true;
                        break;
                    }
                }
            }
        }
        double valuesNextAreGood = highestBlockNextToEachOther? 10 : .1;
        return (GameManager.score * board[4] * valuesNextAreGood * gameManager._round+1) * (openSpaces+1) / failedCheck + addition;
    }

    private void setUpNewNetwork() {
        test = new NeuralNetwork(17,17,12,53,3,4);
        test.SetActivationFunction( Activation.GetActivationFromType(activationType) );
    }

    private void OnBoardUpdate(object sender, EventArgs e) {
        int Score = GameManager.score;
        int HighScore = gameManager.getHighScore();
        answerIndex = 0;
    }

    private void OnGameStart(object sender, EventArgs e) {
        if (_loadNetworkFromPath != "" && !gameStarted) {
            NeuralNetworkSave network = NeuralNetwork.LoadFromFile(_loadNetworkFromPath);
            if (network != null) {
                usingFileSave = true;
                GenerationNetworks[0] = network;
                gameStarted = true;
                return;
            }
        }
        gameStarted = true;
        if (pop >= _maxPopulation) {
            fails = 0;
            gen++;
            pop = 1;
            // choose the best based on performance
            (NeuralNetworkSave[] bestNetworks, double bestFitness, double averageFitness) = GetBestNetworks();
            var bestNetworkId = bestNetworks[0].networkId;
            Debug.Log("Best Fitness of Generation "+ (gen-1).ToString()+": " + bestFitness.ToString()+
            ", Average Fitness of Generation " + (gen-1).ToString()+": " + averageFitness.ToString() + 
            ", Highest Block: "+Mathf.Pow(2,prevHighest).ToString()+
            ", Generation Highest: "+Mathf.Pow(2,currentHighest).ToString()+
            ", Best Network ID: "+bestNetworkId.ToString());
            if( gen == _stopAtGen+1) {
                gameStarted = false;
                NeuralNetwork.SaveToFile(bestNetworks[0],"NetworkTest100");
                Debug.Log("Saved NetworkTest100!");
                return;
            }
            currentHighest = 0;
            GenerationNetworks = new NeuralNetworkSave[_maxPopulation]; // clears the list
            for (int i = 0; i < bestNetworks.Length; i++) {
                GenerationNetworks[i] = bestNetworks[i]; // adds the best networks back to the list
            }
            // add additional networks similar to the best ones with adjustments
            var remainingSpaces = _maxPopulation - bestNetworks.Length;

            // make half merge weights between pairs from the best networks
            // mutate the child of the others
            for (int i = bestNetworks.Length; i < _maxPopulation; i++)
            {
                int randNet1 = rng.Next(0,bestNetworks.Length);
                int randNet2 = rng.Next(0,bestNetworks.Length);
                if (randNet1 == randNet2) {
                    do {
                        randNet2 = rng.Next(0,bestNetworks.Length);
                    } while(randNet1 == randNet2);
                }
                GenerationNetworks[i] = bestNetworks[ (i%2==0)? randNet1 : randNet2 ];
                if (i%2==0) GenerationNetworks[i].unchanging = false;
            }
        } else {
            if (GenerationNetworks.Length > 1) {
                pop++;
                fails = 0;
            }
            
        }
        _generationText.text = "Gen: " + gen.ToString();
        _populationText.text = "Pop: " + pop.ToString();
    }

    private (NeuralNetworkSave[] networks, double bestFitness, double averageFitness) GetBestNetworks() {
        // create 
        int arraySize = (int)(_maxPopulation*(_topPercentile/100.0));
        NeuralNetworkSave[] arr = new NeuralNetworkSave[arraySize];
        double average = 0;
        foreach (var fitness in networkFitness)
        {
            average+=fitness;
        }
        average /= networkFitness.Length;
        double[] sortedArray = new double[networkFitness.Length];
        Array.Copy(networkFitness,sortedArray,networkFitness.Length-1);
        Array.Sort(sortedArray);
        Array.Reverse(sortedArray);

        for (int i = 0; i < arr.Length; i++)
        {
            // get the index of highvalue of sorted array found in the networkFitness array
            var index = Array.IndexOf(networkFitness,sortedArray[i]);
            arr[i] = GenerationNetworks[index];
        }

        return (arr,sortedArray[0],average);
    }

    // 420 + 69 = 489
    void Update() {
        float deltaTime = (float) 1 / _updateFrameRate;
        float timePassed = (float) Time.time - lastUpdate;
        if ( timePassed >= deltaTime ) {
            lastUpdate = Time.time;
            if (gameStarted) {

                if (usingFileSave) {

                    test = test.LoadNewNetwork(GenerationNetworks[0]);
                    (int p, double[] o) = test.Run( BoardToArray() );
                    if (fails > _maxFails) {
                        answerIndex++;
                        if (answerIndex > 3) 
                        {
                            answerIndex = 0;
                            gameFailed = true;
                            return;
                        }
                        fails = 0;
                    }
                    Decide(p,o);
                    if (gameFailed) {
                        gameStarted = false;
                    }
                    return;
                }

                (int prediction, double[] output) = test.Run( BoardToArray() );
                if (fails > _maxFails) {
                    answerIndex++;
                    if (answerIndex > 3) 
                    {
                        answerIndex = 0;
                        gameFailed = true;
                        return;
                    }
                    fails = 0;
                }
                var currentPopulation = pop-1;
                Decide(prediction,output);
                networkFitness[currentPopulation] += Fitness();
                if (gameFailed) {
                    gameFailed = false;
                    GenerationNetworks[currentPopulation] = test.SaveNetwork();
                    networkFitness[currentPopulation] /= gameManager._round;  //Fitness();
                    if (gen == 0) {
                        setUpNewNetwork();
                    } else {
                        test = test.LoadNewNetwork(GenerationNetworks[currentPopulation]);
                        if (!GenerationNetworks[currentPopulation].unchanging) test.Mutate( (float) rng.NextDouble()*_mutateRange );
                    }
                    if (getHighestValueInBoard() > currentHighest) currentHighest = getHighestValueInBoard();

                    gameManager.StartGame();
                }
            }
        }
        
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

    private double[] BoardToArray() {
        Node[] board = gameManager.GetBoard().ToArray();
        double[] newBoard = new double[17];
        openSpaces = 0;
        int highest = getHighestValueInBoard();
        for (int i = 0; i < board.Length; i++)
        {
            var ValueAtNode = 0;
            if ( board[i].OccupiedBlock != null ) {
                ValueAtNode = board[i].OccupiedBlock.Value /* / highest*/;
            } else {
                openSpaces++;
            }
            newBoard[i] = ValueAtNode;
            
        }
        newBoard[16] = fails;
        return newBoard;
    }

    private void Decide(int prediction,double[] output) {
        if (!_networkActive) return;
        double[] propOutput = new double[output.Length];
        Array.Copy(output,propOutput,output.Length-1);
        Array.Sort(propOutput);
        Array.Reverse(propOutput);
        prediction =  Array.IndexOf(output,propOutput[answerIndex]);
        if (prediction < 0) {
            prediction = 0;
            answerIndex = 0;
            fails = 0;
            gameFailed = true;
        }
        switch (prediction) {
            case 0:
                UpInput?.Invoke(this,EventArgs.Empty);
                break;
            case 1:
                DownInput?.Invoke(this,EventArgs.Empty);
                break;
            case 2:
                LeftInput?.Invoke(this,EventArgs.Empty);
                break;
            case 3:
                RightInput?.Invoke(this,EventArgs.Empty);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(prediction), prediction, null);
        }
    }
}
